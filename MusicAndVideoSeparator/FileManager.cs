using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MusicAndVideoSeparator
{
    public class FileManager : FIleManagerBase
    {
        private readonly string[] videoFormats = { "mp4", "3gp", "AVI", "VOB", "mkv", "MP4", "MOV" };
        private readonly string[] imageFormats = { "jpg", "JPG", "jpeg", "png" };
        public FileManager(string workingDirectory)
        {
            DirectoriesToBeProcessed.Add(workingDirectory);
            GetAllDirectoriesWithHeirarchy(workingDirectory);
        }

        public void MoveVideos()
        {
            foreach (string directory in DirectoriesToBeProcessed)
            {
                foreach (string file in Directory.GetFiles(directory))
                {
                    var extension = file.Split(".").Last();

                    //PopulateExtensionsList(extension);

                    //RenameVideosToMp4(file, directory, extension);
                    //RenameImagesToJpg(file, directory, extension);

                    if (file.EndsWith(".mp4") || file.EndsWith(".3gp"))
                    {
                        //MoveVideos(file, videoDestinationPrefix);
                    }
                }
            }
            Console.WriteLine(string.Join(",", ExtensionsList));
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

        public void MoveVideos(string item, string videoDestinationPrefix)
        {
            var items = item.Split("/");
            var formpath = string.Join('/', items, 2, items.Length - 3);

            Directory.CreateDirectory(videoDestinationPrefix + formpath);
            File.Move(item, videoDestinationPrefix + item.Substring(10, item.Length - 10));
        }

        public void RenameVideosToMp4(string file, string directory, string extension)
        {
            if (videoFormats.Contains(extension) && extension != "mp4")
            {
                var split = file.Split(".");
                var formFilePath = string.Join(".", split.Take(split.Count() - 1)) + ".mp4";

                File.Move(file, formFilePath);
            }
        }

        public void RenameImagesToJpg(string file, string directory, string extension)
        {
            if (imageFormats.Contains(extension) && extension != "jpg")
            {
                var split = file.Split(".");
                var formFilePath = string.Join(".", split.Take(split.Count() - 1)) + ".jpg";

                File.Move(file, formFilePath);
            }
        }

        internal void RenameImagesToMp4()
        {
            foreach (string directory in DirectoriesToBeProcessed)
            {
                foreach (string file in Directory.GetFiles(directory))
                {
                    var split = file.Split(".");
                    var formFilePath = string.Join(".", split.Take(split.Count() - 1)) + ".mp4";

                    File.Move(file, formFilePath);
                }
            }
        }
    }
}