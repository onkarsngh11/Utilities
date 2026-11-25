using System.Globalization;
using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;

namespace MultimediaUtilities;

public partial class OrganizerByMonth
{
    public const int MinValidYear = 1994;
    public static int MaxValidYear => DateTime.Now.Year;

    // Matches yyyy-MM-dd or yyyy_MM_dd not embedded inside a longer digit sequence.
    [GeneratedRegex(@"(?<!\d)(\d{4})[-_](\d{2})[-_](\d{2})(?!\d)")]
    private static partial Regex DelimitedYmdRegex();

    // Matches yyyyMMdd not embedded inside a longer digit sequence.
    [GeneratedRegex(@"(?<!\d)(\d{4})(\d{2})(\d{2})(?!\d)")]
    private static partial Regex CompactYmdRegex();

    // Matches ddMMyyyy followed by 3 or 4 sequence digits (e.g. 25122023001 or 251220230001).
    [GeneratedRegex(@"(?<!\d)(\d{2})(\d{2})(\d{4})\d{3,4}(?!\d)")]
    private static partial Regex CompactDmyWithSequenceRegex();

    public static bool IsValidMediaDate(DateTime date) =>
        date != DateTime.MinValue && date.Year >= MinValidYear && date.Year <= MaxValidYear;

    public void UpdateFileDatesBasedOnName(string folderPath, bool dryRun = false)
    {
        var files = Helper.GetFilesWithFullPath(folderPath);
        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            DateTime? date = ExtractDate(fileName);

            if (!date.HasValue)
            {
                Console.WriteLine($"No valid date found in file name: {fileName}");
                continue;
            }

            DateTime fileDate = date.Value;
            if (dryRun)
            {
                Console.WriteLine($"[DRY RUN] Would update timestamps for {file} -> {fileDate:yyyy-MM-dd}");
                continue;
            }

            try
            {
                File.SetCreationTime(file, fileDate);
                File.SetLastWriteTime(file, fileDate);
                File.SetLastAccessTime(file, fileDate);
                Console.WriteLine($"Updated {file} with date: {fileDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update timestamps for '{file}': {ex.Message}");
            }
        }
    }

    public void OrganizeByMonth(
        string workingDirectory,
        string organized,
        string unorganized,
        IEnumerable<string> mediaFormats,
        bool dryRun = false,
        DuplicateHandling duplicateHandling = DuplicateHandling.ReplaceIfSameNameAndSize,
        bool confirmFirstDuplicate = true,
        Func<DuplicateCollisionInfo, bool>? duplicateConfirmer = null)
    {
        var allowedExtensions = Helper.NormalizeExtensions(mediaFormats);
        var files = Helper.GetFilesWithFullPath(workingDirectory);

        bool hasConfirmedDuplicate = false;
        Func<DuplicateCollisionInfo, bool>? oneShotConfirmer = null;

        if (confirmFirstDuplicate && !dryRun)
        {
            var confirmer = duplicateConfirmer ?? PromptUserOnFirstDuplicate;
            oneShotConfirmer = info =>
            {
                if (hasConfirmedDuplicate)
                    return true;

                bool proceed = confirmer(info);
                if (proceed)
                    hasConfirmedDuplicate = true;
                return proceed;
            };
        }

        foreach (var file in files)
        {
            var fi = new FileInfo(file);

            try
            {
                string sourceFileName = fi.Name;
                string fileExtension = Path.GetExtension(sourceFileName);

                if (!allowedExtensions.Contains(fileExtension))
                {
                    // Only process supported photo/video formats; leave other files untouched in sourceDir.
                    continue;
                }

                DateTime? dateFromName = ExtractDate(sourceFileName);
                DateTime finalDate;

                if (dateFromName.HasValue)
                {
                    finalDate = dateFromName.Value;
                }
                else
                {
                    DateTime? metadataDate = GetDateTakenFromMedia(fi.FullName);
                    finalDate = metadataDate ?? GetFallbackFileSystemDate(fi);
                }

                string destinationFolder = Path.Combine(
                    organized,
                    finalDate.ToString("yyyy-MMM", CultureInfo.InvariantCulture));

                Helper.MoveFileSafely(
                    fi.FullName,
                    destinationFolder,
                    "Duplicates",
                    dryRun,
                    duplicateHandling,
                    oneShotConfirmer);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing '{fi.FullName}': {ex.Message}");
                try
                {
                    if (File.Exists(fi.FullName))
                    {
                        Helper.MoveFileSafely(
                            fi.FullName,
                            unorganized,
                            "Duplicates",
                            dryRun,
                            duplicateHandling,
                            oneShotConfirmer);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception moveEx)
                {
                    Console.WriteLine($"Failed to move '{fi.FullName}' to unorganized: {moveEx.Message}");
                }
            }
        }
    }

    private static bool PromptUserOnFirstDuplicate(DuplicateCollisionInfo info)
    {
        Console.WriteLine();
        Console.WriteLine("=== First Duplicate Detected (Manual Confirmation Required) ===");
        Console.WriteLine($"  Action:          {(info.WillReplaceExisting
            ? "REPLACE existing file (exact name & size match)"
            : "MOVE to Duplicates subfolder (same name, different size)")}");
        Console.WriteLine($"  Source:          {info.SourcePath}");
        Console.WriteLine($"    Size:          {info.SourceSize:N0} bytes");
        Console.WriteLine($"    Created:       {info.SourceCreationTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"    Modified:      {info.SourceLastWriteTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  Existing target: {info.ExistingDestinationPath}");
        Console.WriteLine($"    Size:          {info.ExistingSize:N0} bytes");
        Console.WriteLine($"    Created:       {info.ExistingCreationTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"    Modified:      {info.ExistingLastWriteTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  Destination:     {info.PlannedDestinationPath}");
        Console.WriteLine("===============================================================");
        Console.Write("Continue with this operation and all remaining files? [y/N]: ");

        string? response = Console.ReadLine()?.Trim();
        return string.Equals(response, "y", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(response, "yes", StringComparison.OrdinalIgnoreCase);
    }

    public static DateTime? GetDateTakenFromMedia(string path)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(path);

            var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exifSubIfd is not null)
            {
                if (exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime original) && IsValidMediaDate(original))
                    return original;

                if (exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out DateTime digitized) && IsValidMediaDate(digitized))
                    return digitized;
            }

            var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (exifIfd0 is not null &&
                exifIfd0.TryGetDateTime(ExifDirectoryBase.TagDateTime, out DateTime exifDate) &&
                IsValidMediaDate(exifDate))
            {
                return exifDate;
            }

            var qtMeta = directories.OfType<QuickTimeMetadataHeaderDirectory>().FirstOrDefault();
            if (qtMeta is not null &&
                qtMeta.TryGetDateTime(QuickTimeMetadataHeaderDirectory.TagCreationDate, out DateTime qtMetaDate) &&
                IsValidMediaDate(qtMetaDate))
            {
                return qtMetaDate;
            }

            var qtHeader = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
            if (qtHeader is not null &&
                qtHeader.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out DateTime qtCreated) &&
                IsValidMediaDate(qtCreated))
            {
                return qtCreated;
            }
        }
        catch (ImageProcessingException)
        {
            // Unsupported format for MetadataExtractor — fall through to TagLibSharp.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Metadata read warning for '{path}': {ex.Message}");
        }

        return GetDateTakenFromMp4(path);
    }

    public static DateTime? GetDateTakenFromMp4(string path)
    {
        try
        {
            using var file = TagLib.File.Create(path);
            if (file.Tag?.DateTagged is DateTime tagged && IsValidMediaDate(tagged))
                return tagged;
        }
        catch (TagLib.UnsupportedFormatException)
        {
            // Not a TagLib-supported media format.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TagLib read warning for '{path}': {ex.Message}");
        }

        return null;
    }

    public static string GetFileType(string fileName)
    {
        if (fileName.StartsWith("IMG_", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("IMG-", StringComparison.OrdinalIgnoreCase))
            return "IMG";

        if (fileName.StartsWith("VID_", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("VID-", StringComparison.OrdinalIgnoreCase))
            return "VID";

        if (fileName.StartsWith("Screenshot_", StringComparison.OrdinalIgnoreCase))
            return "Screenshot";

        if (fileName.StartsWith("Inshot_", StringComparison.OrdinalIgnoreCase))
            return "Inshot";

        return "date";
    }

    public static DateTime? ExtractDate(string fileName)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(stem))
            return null;

        // 1. Delimited yyyy-MM-dd or yyyy_MM_dd
        foreach (Match match in DelimitedYmdRegex().Matches(stem))
        {
            string candidate = $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}";
            if (TryParseValidDate(candidate, "yyyy-MM-dd", out DateTime parsed))
                return parsed;
        }

        // 2. Compact yyyyMMdd
        foreach (Match match in CompactYmdRegex().Matches(stem))
        {
            string candidate = $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}";
            if (TryParseValidDate(candidate, "yyyy-MM-dd", out DateTime parsed))
                return parsed;
        }

        // 3. Compact ddMMyyyy followed by 3-4 sequence digits (e.g. 25122023001.jpg)
        foreach (Match match in CompactDmyWithSequenceRegex().Matches(stem))
        {
            string candidate = $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}";
            if (TryParseValidDate(candidate, "dd-MM-yyyy", out DateTime parsed))
                return parsed;
        }

        return null;
    }

    private static bool TryParseValidDate(string input, string format, out DateTime result)
    {
        if (DateTime.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result) &&
            IsValidMediaDate(result))
        {
            return true;
        }

        result = DateTime.MinValue;
        return false;
    }

    private static DateTime GetFallbackFileSystemDate(FileInfo fi)
    {
        if (fi.CreationTime != DateTime.MinValue && fi.LastWriteTime != DateTime.MinValue)
            return fi.CreationTime > fi.LastWriteTime ? fi.LastWriteTime : fi.CreationTime;

        return fi.LastWriteTime != DateTime.MinValue ? fi.LastWriteTime : fi.CreationTime;
    }
}
