using System.IO;

namespace MusicAndVideoSeparator
{
    class Program
    {
        static void Main(string[] args)
        {
            string videoDestinationPrefix = "D:/Videos/";
            string[] folders = Directory.GetDirectories("D:/images/");
            foreach (string lvlonefolder in folders)
            {
                string[] lvltwofolders = Directory.GetDirectories(lvlonefolder + "/");
                string[] lvlonefiles = Directory.GetFiles(lvlonefolder + "/");
                if (lvltwofolders.Length != 0)
                {
                    foreach (string lvltwofolder in lvltwofolders)
                    {
                        string[] lvltwofiles = Directory.GetFiles(lvltwofolder + "/");
                        foreach (var lvltwofile in lvltwofiles)
                        {
                            MoveVideos(lvltwofile, videoDestinationPrefix);
                        }
                    }
                }
                foreach (string lvlonefile in lvlonefiles)
                {
                    MoveVideos(lvlonefile, videoDestinationPrefix);
                }
            }
        }
        public static void MoveVideos(string item, string videoDestinationPrefix)
        {
            if (item.EndsWith(".mp4") || item.EndsWith(".3gp"))
            {
                var items = item.Split("/");
                var formpath = string.Join('/', items, 2, items.Length - 3);

                Directory.CreateDirectory(videoDestinationPrefix + formpath);    //videoDestinationPrefix + item.Substring(10, item.Length - 10))
                File.Move(item, videoDestinationPrefix + item.Substring(10, item.Length - 10));
            }
        }
    }
}
