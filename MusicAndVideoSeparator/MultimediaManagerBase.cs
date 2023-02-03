using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MusicAndVideoSeparator
{
    public class MultimediaManagerBase
    {
        protected List<string> DirectoriesToBeProcessed = new List<string>();
        protected List<string> ExtensionsList = new List<string>();
        protected string WorkingDirectory;
        protected string VideoDestinationPrefix;

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
    }
}