namespace MultimediaUtilities;

public class WorkWithTwoDirectories
{
    public int MoveMissingFiles(
        string existingDirectory,
        string sourceDirectory,
        string destinationFolder,
        IEnumerable<string>? mediaFormats = null,
        bool ignoreExtension = false,
        bool dryRun = false)
    {
        List<string> existingFiles = Helper.GetFilesWithFullPath(existingDirectory);
        List<string> sourceFiles = Helper.GetFilesWithFullPath(sourceDirectory);

        HashSet<string>? allowedExtensions = mediaFormats is not null
            ? Helper.NormalizeExtensions(mediaFormats)
            : null;

        string KeySelector(string path) => ignoreExtension
            ? Path.GetFileNameWithoutExtension(path)
            : Path.GetFileName(path);

        var existingKeys = new HashSet<string>(
            existingFiles.Select(KeySelector),
            StringComparer.OrdinalIgnoreCase);

        int movedCount = 0;
        foreach (var file in sourceFiles)
        {
            if (allowedExtensions is { Count: > 0 } && !allowedExtensions.Contains(Path.GetExtension(file)))
                continue;

            if (existingKeys.Contains(KeySelector(file)))
                continue;

            string relativePath = Path.GetRelativePath(sourceDirectory, file);
            string? relativeDir = Path.GetDirectoryName(relativePath);
            string targetFolder = string.IsNullOrEmpty(relativeDir)
                ? destinationFolder
                : Path.Combine(destinationFolder, relativeDir);

            Helper.MoveFileSafely(file, targetFolder, "Duplicates", dryRun);
            movedCount++;
        }

        return movedCount;
    }
}
