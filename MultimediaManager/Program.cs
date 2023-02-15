using System;

namespace MultimediaManager
{
    public delegate void PopulateExtensionsList();
    public delegate void FunctionUsingExtension(string extension);
    public delegate void FunctionUsingFileAndExtension(string file, string extension);
    public delegate void FunctionUsingFile(string file);
    static class Program
    {
        static void Main()
        {
            string workingDirectory = @"D:\Memories\Images\";       //22421  4610
            string videoDestinationPrefix = @"D:\Memories\Videos\";
            MultimediaManager multimediaManager = new MultimediaManager(workingDirectory, videoDestinationPrefix);

            var populateExtensionsList = new FunctionUsingExtension(multimediaManager.PopulateExtensionsList);
            multimediaManager.DirectoriesIterationAndPerform(populateExtensionsList);
            Console.WriteLine(string.Join(",", multimediaManager.ExtensionsList));

            //var renameVideosToMp4 = new FunctionUsingFileAndExtension(multimediaManager.RenameVideosToMp4);
            //multimediaManager.DirectoriesIterationAndPerform(renameVideosToMp4);

            //var renameImagesToJpg = new FunctionUsingFileAndExtension(multimediaManager.RenameImagesToJpg);
            //multimediaManager.DirectoriesIterationAndPerform(renameImagesToJpg);

            ////works only with one directory having all jpg file names which are to be converted to mp4
            //var renameImagesToMp4 = new FunctionUsingFile(multimediaManager.RenameImagesToMp4);
            //multimediaManager.DirectoriesIterationAndPerform(renameImagesToMp4);

        }
    }
}
