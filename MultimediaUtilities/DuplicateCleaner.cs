using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MultimediaUtilities;

public sealed record DuplicateDeletionInfo(
    string Sha256,
    long SizeBytes,
    string KeptPath,
    DateTime KeptLastWriteTime,
    string DeletedPath,
    DateTime DeletedLastWriteTime,
    int TotalGroups,
    int TotalFilesToDelete,
    long TotalBytesToReclaim);

public sealed record DeduplicationSummary(
    int TotalFilesScanned,
    int ZeroByteFilesSkipped,
    int DuplicateGroupsFound,
    int DuplicateFilesMatched,
    int FilesDeleted,
    long BytesReclaimed,
    string? LogFilePath);

public partial class DuplicateCleaner
{
    private const int PartialHashSampleBytes = 64 * 1024;
    private const int StreamBufferSize = 128 * 1024;

    [GeneratedRegex(@"(?:\s*\(\d+\)|(?:\s*-\s*Copy(?:\s*\(\d+\))?))$", RegexOptions.IgnoreCase)]
    private static partial Regex DuplicateSuffixRegex();

    public DeduplicationSummary FindAndDeleteDuplicates(
        string organizedDir,
        string? unorganizedDir = null,
        IEnumerable<string>? mediaFormats = null,
        bool dryRun = false,
        bool confirmFirstDeletion = true,
        string? logFilePath = null,
        Func<DuplicateDeletionInfo, bool>? deletionConfirmer = null)
    {
        if (string.IsNullOrWhiteSpace(organizedDir) || !Directory.Exists(organizedDir))
            throw new DirectoryNotFoundException($"Organized directory not found: '{organizedDir}'.");

        string organizedFull = NormalizeDirPath(organizedDir);
        string? unorganizedFull = null;

        if (!string.IsNullOrWhiteSpace(unorganizedDir))
        {
            if (Directory.Exists(unorganizedDir))
                unorganizedFull = NormalizeDirPath(unorganizedDir);
            else
                Console.WriteLine($"Warning: Unorganized directory not found (skipping): {unorganizedDir}");
        }

        HashSet<string>? allowedExtensions = mediaFormats is not null
            ? Helper.NormalizeExtensions(mediaFormats)
            : null;

        // Collect unique file paths across organized (priority 0) and unorganized (priority 1).
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allFiles = new List<CandidateFile>();

        CollectFiles(organizedFull, isUnorganized: false, allowedExtensions, seenPaths, allFiles);
        if (unorganizedFull is not null)
            CollectFiles(unorganizedFull, isUnorganized: true, allowedExtensions, seenPaths, allFiles);

        int zeroByteSkipped = 0;
        var bySize = new Dictionary<long, List<CandidateFile>>();
        foreach (var file in allFiles)
        {
            if (file.SizeBytes == 0)
            {
                zeroByteSkipped++;
                continue;
            }

            if (!bySize.TryGetValue(file.SizeBytes, out var bucket))
            {
                bucket = [];
                bySize[file.SizeBytes] = bucket;
            }
            bucket.Add(file);
        }

        var sizeCollisionBuckets = bySize.Values.Where(b => b.Count > 1).ToList();
        int sizeCandidateCount = sizeCollisionBuckets.Sum(b => b.Count);

        Console.WriteLine($"Scanned {allFiles.Count:N0} file(s)" +
            (zeroByteSkipped > 0 ? $" (skipped {zeroByteSkipped:N0} empty 0-byte file(s))" : "") +
            $". Found {sizeCandidateCount:N0} file(s) across {sizeCollisionBuckets.Count:N0} matching-size group(s).");

        // Stage 2 + 3: Partial head/tail hash (for files > 128 KB), then full SHA-256.
        var duplicateGroups = new List<(string Sha256, long SizeBytes, List<CandidateFile> Files)>();
        int hashedCount = 0;

        foreach (var sizeBucket in sizeCollisionBuckets)
        {
            IEnumerable<List<CandidateFile>> subGroups;

            if (sizeBucket[0].SizeBytes > PartialHashSampleBytes * 2)
            {
                var byPartial = new Dictionary<string, List<CandidateFile>>(StringComparer.Ordinal);
                foreach (var candidate in sizeBucket)
                {
                    if (!TryComputePartialHash(candidate.FullPath, candidate.SizeBytes, out string partialHash))
                        continue;

                    if (!byPartial.TryGetValue(partialHash, out var list))
                    {
                        list = [];
                        byPartial[partialHash] = list;
                    }
                    list.Add(candidate);
                }

                subGroups = byPartial.Values.Where(g => g.Count > 1);
            }
            else
            {
                subGroups = [sizeBucket];
            }

            foreach (var subGroup in subGroups)
            {
                var byFullHash = new Dictionary<string, List<CandidateFile>>(StringComparer.Ordinal);
                foreach (var candidate in subGroup)
                {
                    if (!TryComputeSha256(candidate.FullPath, out string sha256))
                        continue;

                    hashedCount++;
                    if (hashedCount % 250 == 0)
                        Console.WriteLine($"  Hashed {hashedCount:N0} candidate file(s)...");

                    if (!byFullHash.TryGetValue(sha256, out var list))
                    {
                        list = [];
                        byFullHash[sha256] = list;
                    }
                    list.Add(candidate);
                }

                foreach (var (sha256, group) in byFullHash)
                {
                    if (group.Count > 1)
                    {
                        group.Sort(CompareKeepPriority);
                        duplicateGroups.Add((sha256, group[0].SizeBytes, group));
                    }
                }
            }
        }

        int totalToDelete = duplicateGroups.Sum(g => g.Files.Count - 1);
        long totalBytesToReclaim = duplicateGroups.Sum(g => g.SizeBytes * (g.Files.Count - 1L));

        Console.WriteLine();
        Console.WriteLine($"Duplicate scan complete: {duplicateGroups.Count:N0} group(s), " +
            $"{totalToDelete:N0} duplicate file(s) ({FormatBytes(totalBytesToReclaim)} reclaimable).");

        if (totalToDelete == 0)
        {
            return new DeduplicationSummary(
                allFiles.Count,
                zeroByteSkipped,
                0,
                0,
                0,
                0,
                null);
        }

        string? resolvedLogPath = logFilePath;
        if (string.IsNullOrWhiteSpace(resolvedLogPath) && !dryRun)
        {
            string logDir = Directory.GetParent(Path.TrimEndingDirectorySeparator(organizedFull))?.FullName
                ?? organizedFull;
            resolvedLogPath = Path.Combine(logDir, $"deduplicate_log_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        var logRows = new List<string>();
        bool hasConfirmed = false;
        int filesDeleted = 0;
        long bytesReclaimed = 0;
        var confirmer = deletionConfirmer ?? PromptUserOnFirstDeletion;

        try
        {
            foreach (var (sha256, sizeBytes, group) in duplicateGroups)
            {
                CandidateFile kept = group[0];

                for (int i = 1; i < group.Count; i++)
                {
                    CandidateFile toDelete = group[i];

                    if (dryRun)
                    {
                        Console.WriteLine($"[DRY RUN] KEEP:   {kept.FullPath}");
                        Console.WriteLine($"[DRY RUN] DELETE: {toDelete.FullPath} ({FormatBytes(sizeBytes)}, SHA256={sha256[..12]}...)");
                        if (!string.IsNullOrWhiteSpace(resolvedLogPath))
                        {
                            logRows.Add(FormatCsvRow(
                                DateTime.Now,
                                "DryRunDelete",
                                sha256,
                                sizeBytes,
                                kept.FullPath,
                                toDelete.FullPath,
                                ""));
                        }
                        continue;
                    }

                    if (confirmFirstDeletion && !hasConfirmed)
                    {
                        var info = new DuplicateDeletionInfo(
                            Sha256: sha256,
                            SizeBytes: sizeBytes,
                            KeptPath: kept.FullPath,
                            KeptLastWriteTime: kept.LastWriteTime,
                            DeletedPath: toDelete.FullPath,
                            DeletedLastWriteTime: toDelete.LastWriteTime,
                            TotalGroups: duplicateGroups.Count,
                            TotalFilesToDelete: totalToDelete,
                            TotalBytesToReclaim: totalBytesToReclaim);

                        if (!confirmer(info))
                        {
                            throw new OperationCanceledException(
                                $"Aborted by user before deleting duplicate '{toDelete.FullPath}'. No files were deleted.");
                        }

                        hasConfirmed = true;
                    }

                    try
                    {
                        File.Delete(toDelete.FullPath);
                        filesDeleted++;
                        bytesReclaimed += sizeBytes;
                        Console.WriteLine($"Deleted: {toDelete.FullPath} (kept: {kept.FullPath})");

                        logRows.Add(FormatCsvRow(
                            DateTime.Now,
                            "Deleted",
                            sha256,
                            sizeBytes,
                            kept.FullPath,
                            toDelete.FullPath,
                            ""));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete '{toDelete.FullPath}': {ex.Message}");
                        logRows.Add(FormatCsvRow(
                            DateTime.Now,
                            "Error",
                            sha256,
                            sizeBytes,
                            kept.FullPath,
                            toDelete.FullPath,
                            ex.Message));
                    }
                }
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(resolvedLogPath) && logRows.Count > 0)
            {
                WriteCsvLog(resolvedLogPath, logRows);
                Console.WriteLine($"Audit log saved: {resolvedLogPath}");
            }
        }

        Console.WriteLine(dryRun
            ? $"[DRY RUN] Would delete {totalToDelete:N0} duplicate file(s) ({FormatBytes(totalBytesToReclaim)})."
            : $"Deleted {filesDeleted:N0} of {totalToDelete:N0} duplicate file(s) ({FormatBytes(bytesReclaimed)} reclaimed).");

        return new DeduplicationSummary(
            allFiles.Count,
            zeroByteSkipped,
            duplicateGroups.Count,
            totalToDelete,
            filesDeleted,
            bytesReclaimed,
            logRows.Count > 0 ? resolvedLogPath : null);
    }

    private static void CollectFiles(
        string rootDir,
        bool isUnorganized,
        HashSet<string>? allowedExtensions,
        HashSet<string> seenPaths,
        List<CandidateFile> target)
    {
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        foreach (string path in Directory.EnumerateFiles(rootDir, "*", options))
        {
            string fullPath = Path.GetFullPath(path);
            if (!seenPaths.Add(fullPath))
                continue;

            string fileName = Path.GetFileName(fullPath);
            if (fileName.StartsWith("deduplicate_log_", StringComparison.OrdinalIgnoreCase) &&
                fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (allowedExtensions is { Count: > 0 } && !allowedExtensions.Contains(Path.GetExtension(fileName)))
                continue;

            try
            {
                var fi = new FileInfo(fullPath);
                string relativeDir = Path.GetDirectoryName(Path.GetRelativePath(rootDir, fullPath)) ?? "";
                bool inDuplicatesFolder = HasDuplicatesSegment(relativeDir);
                bool isCustomAlbum = !isUnorganized && !inDuplicatesFolder && IsCustomAlbumDirectory(relativeDir);
                string stem = Path.GetFileNameWithoutExtension(fileName);
                bool hasDuplicateSuffix = DuplicateSuffixRegex().IsMatch(stem);

                target.Add(new CandidateFile(
                    FullPath: fullPath,
                    SizeBytes: fi.Length,
                    LastWriteTime: fi.LastWriteTime,
                    LastWriteTimeUtc: fi.LastWriteTimeUtc,
                    IsUnorganized: isUnorganized,
                    InDuplicatesFolder: inDuplicatesFolder,
                    IsCustomAlbum: isCustomAlbum,
                    HasDuplicateSuffix: hasDuplicateSuffix));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not inspect '{fullPath}': {ex.Message}");
            }
        }
    }

    private static bool HasDuplicatesSegment(string relativeDir)
    {
        if (string.IsNullOrEmpty(relativeDir))
            return false;

        foreach (string segment in relativeDir.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.Equals("Duplicates", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsCustomAlbumDirectory(string relativeDir)
    {
        if (string.IsNullOrEmpty(relativeDir))
            return false;

        string? topSegment = relativeDir
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(topSegment) ||
            topSegment.Equals("Duplicates", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !DateTime.TryParseExact(
            topSegment,
            "yyyy-MMM",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    // Lower value = higher priority to KEEP.
    private static int CompareKeepPriority(CandidateFile a, CandidateFile b)
    {
        int cmp = a.IsUnorganized.CompareTo(b.IsUnorganized);
        if (cmp != 0) return cmp;

        cmp = a.InDuplicatesFolder.CompareTo(b.InDuplicatesFolder);
        if (cmp != 0) return cmp;

        // Prefer custom named album folders (IsCustomAlbum == true) over yyyy-MMM/root folders.
        cmp = b.IsCustomAlbum.CompareTo(a.IsCustomAlbum);
        if (cmp != 0) return cmp;

        cmp = a.HasDuplicateSuffix.CompareTo(b.HasDuplicateSuffix);
        if (cmp != 0) return cmp;

        cmp = a.LastWriteTimeUtc.CompareTo(b.LastWriteTimeUtc);
        if (cmp != 0) return cmp;

        cmp = a.FullPath.Length.CompareTo(b.FullPath.Length);
        if (cmp != 0) return cmp;

        return StringComparer.OrdinalIgnoreCase.Compare(a.FullPath, b.FullPath);
    }

    private static bool TryComputePartialHash(string path, long fileLength, out string hashHex)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, PartialHashSampleBytes);
            byte[] buffer = new byte[PartialHashSampleBytes * 2];

            int firstRead = ReadFully(stream, buffer.AsSpan(0, PartialHashSampleBytes));
            stream.Seek(fileLength - PartialHashSampleBytes, SeekOrigin.Begin);
            int lastRead = ReadFully(stream, buffer.AsSpan(firstRead, PartialHashSampleBytes));

            byte[] digest = SHA256.HashData(buffer.AsSpan(0, firstRead + lastRead));
            hashHex = Convert.ToHexString(digest);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not read '{path}' for partial hash: {ex.Message}");
            hashHex = "";
            return false;
        }
    }

    private static bool TryComputeSha256(string path, out string hashHex)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, StreamBufferSize);
            byte[] digest = SHA256.HashData(stream);
            hashHex = Convert.ToHexString(digest);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not hash '{path}': {ex.Message}");
            hashHex = "";
            return false;
        }
    }

    private static int ReadFully(Stream stream, Span<byte> buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = stream.Read(buffer[totalRead..]);
            if (read == 0)
                break;
            totalRead += read;
        }
        return totalRead;
    }

    private static bool PromptUserOnFirstDeletion(DuplicateDeletionInfo info)
    {
        Console.WriteLine();
        Console.WriteLine("=== First Duplicate Deletion (Manual Confirmation Required) ===");
        Console.WriteLine($"  SHA-256:        {info.Sha256}");
        Console.WriteLine($"  Size:           {info.SizeBytes:N0} bytes ({FormatBytes(info.SizeBytes)})");
        Console.WriteLine($"  KEEP:           {info.KeptPath}");
        Console.WriteLine($"    Modified:     {info.KeptLastWriteTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  DELETE:         {info.DeletedPath}");
        Console.WriteLine($"    Modified:     {info.DeletedLastWriteTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  Total planned:  {info.TotalFilesToDelete:N0} file(s) across {info.TotalGroups:N0} group(s) ({FormatBytes(info.TotalBytesToReclaim)} to reclaim)");
        Console.WriteLine("===============================================================");
        Console.Write("Permanently delete this duplicate and all remaining duplicates? [y/N]: ");

        string? response = Console.ReadLine()?.Trim();
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteCsvLog(string logFilePath, List<string> rows)
    {
        string? dir = Path.GetDirectoryName(logFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Action,Sha256,SizeBytes,KeptPath,DeletedPath,Error");
        foreach (string row in rows)
            sb.AppendLine(row);

        File.WriteAllText(logFilePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string FormatCsvRow(
        DateTime timestamp,
        string action,
        string sha256,
        long sizeBytes,
        string keptPath,
        string deletedPath,
        string error) =>
        string.Join(",",
            CsvEscape(timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
            CsvEscape(action),
            CsvEscape(sha256),
            sizeBytes.ToString(),
            CsvEscape(keptPath),
            CsvEscape(deletedPath),
            CsvEscape(error));

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string NormalizeDirPath(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0 ? $"{bytes:N0} B" : $"{value:F2} {units[unit]}";
    }

    private sealed record CandidateFile(
        string FullPath,
        long SizeBytes,
        DateTime LastWriteTime,
        DateTime LastWriteTimeUtc,
        bool IsUnorganized,
        bool InDuplicatesFolder,
        bool IsCustomAlbum,
        bool HasDuplicateSuffix);
}
