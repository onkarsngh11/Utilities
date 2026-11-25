using MultimediaUtilities;

if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
{
    PrintUsage();
    return 0;
}

bool dryRun = false;
bool deleteRoot = false;
bool ignoreExtension = false;
bool keepDuplicates = false;
bool autoConfirm = false;
bool mediaOnly = false;
string? logPath = null;
var positionalList = new List<string>();

for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    if (arg.Equals("--dry-run", StringComparison.OrdinalIgnoreCase))
        dryRun = true;
    else if (arg.Equals("--delete-root", StringComparison.OrdinalIgnoreCase))
        deleteRoot = true;
    else if (arg.Equals("--ignore-extension", StringComparison.OrdinalIgnoreCase))
        ignoreExtension = true;
    else if (arg.Equals("--keep-duplicates", StringComparison.OrdinalIgnoreCase))
        keepDuplicates = true;
    else if (arg.Equals("--yes", StringComparison.OrdinalIgnoreCase) || arg.Equals("-y", StringComparison.OrdinalIgnoreCase))
        autoConfirm = true;
    else if (arg.Equals("--media-only", StringComparison.OrdinalIgnoreCase))
        mediaOnly = true;
    else if (arg.Equals("--log", StringComparison.OrdinalIgnoreCase))
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine("Missing value for --log");
            return 1;
        }
        logPath = args[++i];
    }
    else if (arg.StartsWith("--log=", StringComparison.OrdinalIgnoreCase))
        logPath = arg["--log=".Length..];
    else if (!arg.StartsWith('-'))
        positionalList.Add(arg);
}

string[] positional = positionalList.ToArray();

if (positional.Length == 0)
{
    PrintUsage();
    return 1;
}

string command = positional[0].ToLowerInvariant();
var organizer = new OrganizerByMonth();

try
{
    switch (command)
    {
        case "organize":
            if (positional.Length < 4)
            {
                Console.Error.WriteLine("Usage: organize <sourceDir> <organizedDir> <unorganizedDir> [--keep-duplicates] [--yes] [--dry-run]");
                return 1;
            }
            organizer.OrganizeByMonth(
                positional[1],
                positional[2],
                positional[3],
                Helper.DefaultMediaExtensions,
                dryRun,
                keepDuplicates
                    ? DuplicateHandling.MoveToDuplicates
                    : DuplicateHandling.ReplaceIfSameNameAndSize,
                confirmFirstDuplicate: !autoConfirm);
            return 0;

        case "update-dates":
            if (positional.Length < 2)
            {
                Console.Error.WriteLine("Usage: update-dates <folderPath> [--dry-run]");
                return 1;
            }
            organizer.UpdateFileDatesBasedOnName(positional[1], dryRun);
            return 0;

        case "move-videos":
            if (positional.Length < 3)
            {
                Console.Error.WriteLine("Usage: move-videos <organizedDir> <organizedVideosDir> [--dry-run]");
                return 1;
            }
            int videosMoved = MoveOrganizedVideos.MoveVideosToOrganizedVideos(
                positional[1],
                positional[2],
                dryRun: dryRun);
            Console.WriteLine($"Processed {videosMoved} video file(s).");
            return 0;

        case "move-missing":
            if (positional.Length < 4)
            {
                Console.Error.WriteLine("Usage: move-missing <existingDir> <sourceDir> <destinationDir> [--ignore-extension] [--dry-run]");
                return 1;
            }
            var comparer = new WorkWithTwoDirectories();
            int missingMoved = comparer.MoveMissingFiles(
                positional[1],
                positional[2],
                positional[3],
                Helper.DefaultMediaExtensions,
                ignoreExtension,
                dryRun);
            Console.WriteLine($"Processed {missingMoved} missing file(s).");
            return 0;

        case "deduplicate":
            if (positional.Length < 2)
            {
                Console.Error.WriteLine("Usage: deduplicate <organizedDir> [unorganizedDir] [--media-only] [--yes] [--dry-run] [--log <csvPath>]");
                return 1;
            }
            var cleaner = new DuplicateCleaner();
            cleaner.FindAndDeleteDuplicates(
                positional[1],
                positional.Length >= 3 ? positional[2] : null,
                mediaOnly ? Helper.DefaultMediaExtensions : null,
                dryRun,
                confirmFirstDeletion: !autoConfirm,
                logFilePath: logPath);
            return 0;

        case "delete-empty":
            if (positional.Length < 2)
            {
                Console.Error.WriteLine("Usage: delete-empty <folderPath> [--delete-root] [--dry-run]");
                return 1;
            }
            int deleted = DirectoryManager.RecurseDeleteEmptyDirectories(positional[1], deleteRoot, dryRun);
            Console.WriteLine($"Empty directories matched: {deleted}.");
            return 0;

        case "list-extensions":
            if (positional.Length < 2)
            {
                Console.Error.WriteLine("Usage: list-extensions <folderPath>");
                return 1;
            }
            var extensions = Helper.GetAllFileExtensions(positional[1]);
            Console.WriteLine(string.Join(", ", extensions));
            return 0;

        case "google-photos":
            var photosService = new GooglePhotosClientService();
            await photosService.ListMediaItemsAsync();
            return 0;

        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintUsage();
            return 1;
    }
}
catch (OperationCanceledException ex)
{
    Console.WriteLine($"Cancelled: {ex.Message}");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static void PrintUsage()
{
    Console.WriteLine("""
        MultimediaUtilities CLI

        Commands:
          organize <sourceDir> <organizedDir> <unorganizedDir> [--keep-duplicates] [--yes] [--dry-run]
              Organize photo/video files into yyyy-MMM folders (non-media files are skipped;
              errored media files fall back to unorganizedDir). Exact duplicates (same name &
              byte size) replace existing files by default; other collisions move to a Duplicates
              subfolder. Pauses on the first duplicate for confirmation unless --yes/-y or --dry-run is specified.

          update-dates <folderPath> [--dry-run]
              Update file Creation/LastWrite/LastAccess timestamps from dates in filenames.

          move-videos <organizedDir> <organizedVideosDir> [--dry-run]
              Move video files out of organizedDir while preserving subfolder structure.

          move-missing <existingDir> <sourceDir> <destinationDir> [--ignore-extension] [--dry-run]
              Move media files present in sourceDir but absent from existingDir into destinationDir.

          deduplicate <organizedDir> [unorganizedDir] [--media-only] [--yes] [--dry-run] [--log <csvPath>]
              Find and permanently delete exact byte-identical duplicates (size + SHA-256)
              across organizedDir and unorganizedDir. Prefers keeping files in organizedDir,
              outside Duplicates folders, in custom album folders (non-yyyy-MMM), with cleaner
              filenames (no (1)/- Copy suffix), then oldest modified date. Pauses on the first
              deletion for confirmation unless --yes/-y or --dry-run is set, and writes a CSV
              audit log on live runs.

          delete-empty <folderPath> [--delete-root] [--dry-run]
              Recursively delete empty subdirectories (root preserved unless --delete-root).

          list-extensions <folderPath>
              List distinct file extensions under folderPath.

          google-photos
              List Google Photos media items using GOOGLE_PHOTOS_USER,
              GOOGLE_PHOTOS_CLIENT_ID, and GOOGLE_PHOTOS_CLIENT_SECRET env vars.
        """);
}
