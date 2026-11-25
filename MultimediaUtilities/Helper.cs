namespace MultimediaUtilities;

public enum DuplicateHandling
{
    MoveToDuplicates,
    ReplaceIfSameNameAndSize
}

public sealed record DuplicateCollisionInfo(
    string SourcePath,
    long SourceSize,
    DateTime SourceCreationTime,
    DateTime SourceLastWriteTime,
    string ExistingDestinationPath,
    long ExistingSize,
    DateTime ExistingCreationTime,
    DateTime ExistingLastWriteTime,
    string PlannedDestinationPath,
    bool WillReplaceExisting);

public static class Helper
{
    public static readonly IReadOnlyList<string> DefaultMediaExtensions =
    [
        ".mp4", ".3gp", ".avi", ".vob", ".mkv", ".mov", ".mp",
        ".jpg", ".jpeg", ".png", ".heic"
    ];

    public static readonly IReadOnlyList<string> DefaultVideoExtensions =
    [
        ".mp4", ".3gp", ".avi", ".vob", ".mkv", ".mov", ".mp"
    ];

    public static HashSet<string> NormalizeExtensions(IEnumerable<string> extensions)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ext in extensions)
        {
            if (string.IsNullOrWhiteSpace(ext))
                continue;

            var trimmed = ext.Trim();
            normalized.Add(trimmed.StartsWith('.') ? trimmed : "." + trimmed);
        }

        return normalized;
    }

    public static List<string> GetFilesWithFullPath(string path)
    {
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true
        };

        return Directory.EnumerateFiles(path, "*", enumerationOptions).ToList();
    }

    public static List<string> GetDirectories(string path, bool recursive = true)
    {
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = recursive
        };

        return Directory.EnumerateDirectories(path, "*", enumerationOptions).ToList();
    }

    public static List<string> GetFileNames(string path)
    {
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true
        };

        return Directory.EnumerateFiles(path, "*", enumerationOptions)
                        .Select(Path.GetFileName)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Cast<string>()
                        .ToList();
    }

    public static List<string> GetAllFileExtensions(string path)
    {
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true
        };

        return Directory.EnumerateFiles(path, "*", enumerationOptions)
                        .Select(file => Path.GetExtension(file))
                        .Where(ext => !string.IsNullOrEmpty(ext))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
    }

    public static List<string> GetVideoFiles(string path, IEnumerable<string>? videoExtensions = null)
    {
        var extensions = NormalizeExtensions(videoExtensions ?? DefaultVideoExtensions);

        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true
        };

        return Directory.EnumerateFiles(path, "*", enumerationOptions)
                        .Where(file => extensions.Contains(Path.GetExtension(file)))
                        .ToList();
    }

    public static string MoveFileSafely(
        string sourcePath,
        string destinationFolder,
        string? duplicatesSubfolder = "Duplicates",
        bool dryRun = false,
        DuplicateHandling duplicateHandling = DuplicateHandling.MoveToDuplicates,
        Func<DuplicateCollisionInfo, bool>? confirmDuplicate = null)
    {
        string fileName = Path.GetFileName(sourcePath);
        string destinationPath = Path.Combine(destinationFolder, fileName);

        if (string.Equals(
                Path.GetFullPath(sourcePath),
                Path.GetFullPath(destinationPath),
                StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"Skipped (already at destination): {sourcePath}");
            return destinationPath;
        }

        bool overwriteExisting = false;
        string existingDestinationPath = destinationPath;

        if (File.Exists(existingDestinationPath))
        {
            var sourceInfo = new FileInfo(sourcePath);
            var existingInfo = new FileInfo(existingDestinationPath);

            if (duplicateHandling == DuplicateHandling.ReplaceIfSameNameAndSize &&
                IsExactNameAndSizeMatch(sourceInfo, existingInfo))
            {
                overwriteExisting = true;
            }
            else
            {
                string collisionFolder = string.IsNullOrWhiteSpace(duplicatesSubfolder)
                    ? destinationFolder
                    : Path.Combine(destinationFolder, duplicatesSubfolder);

                destinationPath = GetUniqueDestinationPath(collisionFolder, fileName);
            }

            if (!dryRun && confirmDuplicate is not null)
            {
                var collisionInfo = new DuplicateCollisionInfo(
                    SourcePath: sourcePath,
                    SourceSize: sourceInfo.Length,
                    SourceCreationTime: sourceInfo.CreationTime,
                    SourceLastWriteTime: sourceInfo.LastWriteTime,
                    ExistingDestinationPath: existingDestinationPath,
                    ExistingSize: existingInfo.Length,
                    ExistingCreationTime: existingInfo.CreationTime,
                    ExistingLastWriteTime: existingInfo.LastWriteTime,
                    PlannedDestinationPath: destinationPath,
                    WillReplaceExisting: overwriteExisting);

                if (!confirmDuplicate(collisionInfo))
                {
                    throw new OperationCanceledException(
                        $"Aborted by user before processing duplicate '{sourcePath}'.");
                }
            }
        }

        string? finalDirectory = Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrEmpty(finalDirectory))
            throw new InvalidOperationException($"Could not determine destination directory for '{destinationPath}'.");

        if (dryRun)
        {
            string action = overwriteExisting ? "Would replace exact duplicate" : "Would move";
            Console.WriteLine($"[DRY RUN] {action}: {sourcePath} -> {destinationPath}");
            return destinationPath;
        }

        Directory.CreateDirectory(finalDirectory);
        File.Move(sourcePath, destinationPath, overwrite: overwriteExisting);
        Console.WriteLine(overwriteExisting
            ? $"Replaced exact duplicate: {sourcePath} -> {destinationPath}"
            : $"Moved: {sourcePath} -> {destinationPath}");
        return destinationPath;
    }

    private static bool IsExactNameAndSizeMatch(FileInfo sourceInfo, FileInfo destinationInfo) =>
        string.Equals(sourceInfo.Name, destinationInfo.Name, StringComparison.OrdinalIgnoreCase) &&
        sourceInfo.Length == destinationInfo.Length;

    private static string GetUniqueDestinationPath(string folder, string fileName)
    {
        string candidate = Path.Combine(folder, fileName);
        if (!File.Exists(candidate))
            return candidate;

        string stem = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        int suffix = 1;

        while (true)
        {
            candidate = Path.Combine(folder, $"{stem} ({suffix}){extension}");
            if (!File.Exists(candidate))
                return candidate;
            suffix++;
        }
    }
}
