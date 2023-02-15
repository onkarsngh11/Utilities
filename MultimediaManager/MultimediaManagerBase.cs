using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MultimediaManager
{
    public class MultimediaManagerBase
    {
        public List<string> DirectoriesToBeProcessed { get; set; } = new List<string>();
        public List<string> ListOfFiles { get; set; } = new List<string>();
        public List<string> ExtensionsList { get; set; } = new List<string>();
        public string VideoDestinationPrefix { get; set; }
        public string[] ImageFormats { get; set; }
        public string[] VideoFormats { get; set; }

        public List<string> GetAllDirectoriesWithHeirarchy(string parentDirectory)
        {
            var directories = Directory.EnumerateDirectories(parentDirectory).ToList();
            DirectoriesToBeProcessed.AddRange(directories);

            foreach (var directory in directories)
            {
                var subDirectories = GetAllDirectoriesWithHeirarchy(directory);
                if (subDirectories != null)
                    DirectoriesToBeProcessed.AddRange(subDirectories);
            }

            if (directories.Count == 0)
                return directories;
            else
            {
                return new List<string>();
            }
        }

        public void IterateDrectoriesAnd(FunctionUsingExtension perform)
        {
            foreach (string directory in DirectoriesToBeProcessed)
            {
                var files = Directory.GetFiles(directory);
                foreach (string file in files)
                {
                    var extension = file.Split(".").Last();
                    perform(extension);
                }
            }
        }

        public void IterateDirectoriesAnd(FunctionUsingFile perform)
        {
            foreach (string directory in DirectoriesToBeProcessed)
            {
                var files = Directory.GetFiles(directory);
                foreach (string file in files)
                {
                    perform(file);
                }
            }
        }

        public void IterateDirectoriesAnd(FunctionUsingFileAndExtension perform)
        {
            foreach (string directory in DirectoriesToBeProcessed)
            {
                var files = Directory.GetFiles(directory);
                foreach (string file in files)
                {
                    var extension = file.Split(".").Last();
                    perform(file, extension);
                }
            }
        }
    }
}