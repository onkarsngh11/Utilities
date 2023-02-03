using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MusicAndVideoSeparator
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

        public void WorkWithMultimediaFiles()
        {
            int filesCount = 0;
            foreach (string directory in DirectoriesToBeProcessed)
            {
                var files = Directory.GetFiles(directory);
                if (files.Length == 0)
                {
                    Console.WriteLine(directory);
                }
                foreach (string file in files)
                {
                    //var extension = file.Split(".").Last();
                    //PopulateExtensionsList(extension);
                    //RenameVideosToMp4(file, extension);
                    //RenameImagesToJpg(file, extension);
                    //RenameImagesToMp4(file, extension);   works only with one directory having all jpg file names which are to be converted to mp4
                    //if (file.EndsWith(".mp4") || file.EndsWith(".3gp"))
                    //{
                    //    MoveVideos(file);
                    //}
                    //filesCount++;
                }
            }
            Console.WriteLine("Files Count: {0}", filesCount);
            //Console.WriteLine(string.Join(",", ExtensionsList));
        }

        public void MoveVideos(string item)
        {
            var items = item.Split("\\");
            var filePath = string.Join('\\', items, 3, items.Length - 3);
            var directoryPath = string.Join('\\', items, 3, items.Length - 4);
            Directory.CreateDirectory(VideoDestinationPrefix + directoryPath);
            File.Move(item, VideoDestinationPrefix + filePath);
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
    }
}