using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            string workingDirectory = @"D:\Memories\google";       //22421  4610
            string videoDestinationPrefix = @"D:\Memories\Videos\";

            MultimediaManager multimediaManager = new MultimediaManager(workingDirectory, videoDestinationPrefix, imageFormats, videoFormats);

            //var moveVideos = new FunctionUsingFile(multimediaManager.MoveVideos);
            //multimediaManager.IterateDirectoriesAnd(moveVideos);

            var populateExtensionsList = new FunctionUsingExtension(multimediaManager.PopulateExtensionsList);
            multimediaManager.IterateDrectoriesAnd(populateExtensionsList);
            Console.WriteLine(string.Join(",", multimediaManager.ExtensionsList));

            //var renameVideosToMp4 = new FunctionUsingFileAndExtension(multimediaManager.RenameVideosToMp4);
            //multimediaManager.IterateDirectoriesAnd(renameVideosToMp4);

            //var renameImagesToJpg = new FunctionUsingFileAndExtension(multimediaManager.RenameImagesToJpg);
            //multimediaManager.IterateDirectoriesAnd(renameImagesToJpg);

            ////works only with one directory having all jpg file names which are to be converted to mp4
            //var renameImagesToMp4 = new FunctionUsingFile(multimediaManager.RenameImagesToMp4);
            //multimediaManager.IterateDirectoriesAnd(renameImagesToMp4);

            //multimediaManager.FixJpgFileNameToMp4();

            //GetPhotosOrVieosNotInMemoriesButInGooglePhotosToBeMoved(videoFormats, imageFormats,
            //videoDestinationPrefix, firstWorkingDirectory, secondWorkingDirectory);
        }

        private static void GetPhotosOrVieosNotInMemoriesButInGooglePhotosToBeMoved(string[] videoFormats, string[] imageFormats, string videoDestinationPrefix)
        {
            string firstWorkingDirectory = @"D:\Memories\google";
            string secondWorkingDirectory = @"D:\Google Photos\";
            MultimediaManager multimediaManager = new MultimediaManager(firstWorkingDirectory, videoDestinationPrefix, imageFormats, videoFormats);

            MultimediaManager multimediaManager2 = new MultimediaManager(secondWorkingDirectory, videoDestinationPrefix, imageFormats, videoFormats);

            var listOfFilesInMemories = new FunctionUsingFile(multimediaManager.GetListOfFiles);
            multimediaManager.IterateDirectoriesAnd(listOfFilesInMemories);

            var listOfFilesInGooglePhotos = new FunctionUsingFile(multimediaManager2.GetListOfFiles);
            multimediaManager2.IterateDirectoriesAnd(listOfFilesInGooglePhotos);

            var orderedlistOfFilesInGooglePhotos = multimediaManager2.ListOfFiles.OrderBy(f => f).ToList();
            var orderedlistOfFilesInMemories = multimediaManager.ListOfFiles.OrderBy(f => f).ToList();
            var filesNotInMemories = new List<string>();

            foreach (var file in orderedlistOfFilesInGooglePhotos)
            {
                var filenameafterlastslash = file.Split('\\').Last();
                var filenameSplit = filenameafterlastslash.Split('.');
                var filename = string.Join('.',filenameSplit.Take(filenameSplit.Count() - 1));

                if (!orderedlistOfFilesInMemories.Any(f => f.Contains(filename)))
                {
                    filesNotInMemories.Add(file);
                }
            }

            Console.WriteLine(string.Join("\n", filesNotInMemories));
            foreach(var item in filesNotInMemories)
            {
                var destinationFileName = item.Split("\\").Last();

                File.Copy(item, @"D:\Memories\google\" + destinationFileName);
            }
        }
    }
}
