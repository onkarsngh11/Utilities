namespace MultimediaUtilities;

public static class DirectoryManager
{
    public static int RecurseDeleteEmptyDirectories(string path, bool deleteRoot = false, bool dryRun = false)
    {
        if (!Directory.Exists(path))
            return 0;

        int deletedCount = 0;

        // Enumerate only immediate children so each directory is visited once bottom-up.
        foreach (var subdirectory in Directory.EnumerateDirectories(path))
        {
            deletedCount += RecurseDeleteEmptyDirectories(subdirectory, deleteRoot: true, dryRun: dryRun);
        }

        if (!deleteRoot)
            return deletedCount;

        bool isEmpty = !Directory.EnumerateFileSystemEntries(path).Any();
        if (!isEmpty)
            return deletedCount;

        if (dryRun)
        {
            Console.WriteLine($"[DRY RUN] Would delete empty directory: {path}");
        }
        else
        {
            Directory.Delete(path);
            Console.WriteLine($"Deleted empty directory: {path}");
        }

        return deletedCount + 1;
    }
}
