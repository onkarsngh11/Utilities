using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MultimediaManager
{
    public class MultimediaManager : MultimediaManagerBase
    {
        private readonly string[] videoFormats = { "mp4", "3gp", "AVI", "VOB", "mkv", "MP4", "MOV" };
        private readonly string[] imageFormats = { "jpg", "JPG", "jpeg", "png" };

        public MultimediaManager(string workingDirectory, string videoDestinationPrefix)
        {
            VideoDestinationPrefix = videoDestinationPrefix;
            DirectoriesToBeProcessed.Add(workingDirectory);
            GetAllDirectoriesWithHeirarchy(workingDirectory);
        }

        public void MoveVideos(string sourceFilePath)
        {
            if (sourceFilePath.EndsWith(".mp4") || sourceFilePath.EndsWith(".3gp"))
            {
                var items = sourceFilePath.Split("\\");
                var destinationFilePath = string.Join('\\', items, 3, items.Length - 3);
                var destinationDirectoryPath = string.Join('\\', items, 3, items.Length - 4);

                Directory.CreateDirectory(VideoDestinationPrefix + destinationDirectoryPath);
                File.Move(sourceFilePath, VideoDestinationPrefix + destinationFilePath);
            }
        }

        public void FixJpgFileNameToMp4()
        {
            Dictionary<string, float> fileNameWithSize = new Dictionary<string, float>();
            foreach (string directory in DirectoriesToBeProcessed)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                FileInfo[] files = directoryInfo.GetFiles();
                foreach (FileInfo file in files)
                {
                    var size = file.Length / (1024 * 1024);

                    fileNameWithSize.Add(file.FullName, size);
                }
            }

            var sortedFiles = fileNameWithSize.OrderByDescending(f => f.Value);

            foreach (var item in sortedFiles)
            {
                if ((item.Key.ToLower().Contains("vid") || item.Key.ToLower().Contains("mvi")) && item.Key.Split(".").Last() != "mp4"
                    && !item.Key.ToLower().Contains("mvimg")) // && (item.Value > 16)
                {
                    var split = item.Key.Split(".");
                    var formFilePath = string.Join(".", split.Take(split.Count() - 1)) + ".mp4";

                    File.Move(item.Key, formFilePath);
                }
                Console.WriteLine("{0}: {1}", item.Key, item.Value);
            }
        }

        public void PopulateExtensionsList(string extension)
        {
            if (!ExtensionsList.Contains(extension))// && videoFormats.Contains(extension))
                ExtensionsList.Add(extension);
        }

        public void RenameVideosToMp4(string file, string extension)
        {
            if (videoFormats.Contains(extension) && extension != "mp4")
            {
                var split = file.Split(".");
                var formFilePath = string.Join(".", split.Take(split.Count() - 1)) + ".mp4";

                File.Move(file, formFilePath);
            }
        }

        public void RenameImagesToJpg(string file, string extension)
        {
            if (imageFormats.Contains(extension) && extension != "jpg")
            {
                var split = file.Split(".");
                var formFilePath = string.Join(".", split.Take(split.Count() - 1)) + ".jpg";

                File.Move(file, formFilePath);
            }
        }

        public void RenameImagesToMp4(string file)
        {
            var split = file.Split(".");
            var formFilePath = string.Join(".", split.Take(split.Count() - 1)) + ".mp4";

            File.Move(file, formFilePath);
        }

        internal void GetPhotosOrVieosNotInMemoriesButInGooglePhotosToBeMoved()
        {

            throw new NotImplementedException();
        }

        internal void GetVideosNotInGooglePhotosButInMemoriesToBeBackedUp()
        {
            throw new NotImplementedException();
        }
    }
}