using System;

namespace MultimediaManager
{
    public delegate void FunctionUsingExtension(string extension);
    public delegate void FunctionUsingFileAndExtension(string file, string extension);
    public delegate void FunctionUsingFile(string file);
    static class Program
    {
        static void Main()
        {
            string[] videoFormats = { "mp4", "3gp", "AVI", "VOB", "mkv", "MP4", "MOV", "MP" };
            string[] imageFormats = { "jpg", "JPG", "jpeg", "png" };
            string workingDirectory = @"D:\Memories\";       //22421  4610
            string videoDestinationPrefix = @"D:\Memories\Videos\";

            MultimediaManager multimediaManager = new MultimediaManager(workingDirectory, videoDestinationPrefix, imageFormats, videoFormats);

            var moveVideos = new FunctionUsingFile(multimediaManager.MoveVideos);
            multimediaManager.IterateDirectoriesAnd(moveVideos);

            var populateExtensionsList = new FunctionUsingExtension(multimediaManager.PopulateExtensionsList);
            multimediaManager.IterateDrectoriesAnd(populateExtensionsList);
            Console.WriteLine(string.Join(",", multimediaManager.ExtensionsList));

            var renameVideosToMp4 = new FunctionUsingFileAndExtension(multimediaManager.RenameVideosToMp4);
            multimediaManager.IterateDirectoriesAnd(renameVideosToMp4);

            var renameImagesToJpg = new FunctionUsingFileAndExtension(multimediaManager.RenameImagesToJpg);
            multimediaManager.IterateDirectoriesAnd(renameImagesToJpg);

            //works only with one directory having all jpg file names which are to be converted to mp4
            var renameImagesToMp4 = new FunctionUsingFile(multimediaManager.RenameImagesToMp4);
            multimediaManager.IterateDirectoriesAnd(renameImagesToMp4);

            multimediaManager.FixJpgFileNameToMp4();
    
            multimediaManager.GetPhotosOrVieosNotInMemoriesButInGooglePhotosToBeMoved(videoFormats, imageFormats,
            videoDestinationPrefix);
        }
    }
}
