namespace MultimediaUtilities;

public static class MoveOrganizedVideos
{
    public static int MoveVideosToOrganizedVideos(
        string organizedDir,
        string organizedVideosDir,
        IEnumerable<string>? videoExtensions = null,
        bool dryRun = false)
    {
        List<string> videoFiles = Helper.GetVideoFiles(organizedDir, videoExtensions);
        int movedCount = 0;

        foreach (var videoFile in videoFiles)
        {
            string relativePath = Path.GetRelativePath(organizedDir, videoFile);
            string? relativeDir = Path.GetDirectoryName(relativePath);
            string destinationFolder = string.IsNullOrEmpty(relativeDir)
                ? organizedVideosDir
                : Path.Combine(organizedVideosDir, relativeDir);

            Helper.MoveFileSafely(videoFile, destinationFolder, "Duplicates", dryRun);
            movedCount++;
        }

        return movedCount;
    }
}
