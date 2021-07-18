using System.IO;
using System.Linq;

namespace ConsolePlayGround
{
    class Program
    {
        static void Main(string[] args)
        {
            string[] crap = { "(dj-jatt.com)", "(mr-song.com)", "(mrsong.com)", "(filmysongs.co)", "(mirchifuns.ga)", "(jspweb.tk)", "(mrjatt.club", "mrjatt.app", "ROckMaza.com.mp3", "(Mr-Jatt.com)", "(DjjOhal.com)", "(RockMaza.com)", "(DjYoungster.Com)", "()", "(mr-Jatt.com)", "(DjPunjab.CoM)", "(Dj-Jatt.com)", "(PenduJatt.com)", "[Mr-Song.Com]" };
            string folderpath = "D:/Music/Punjabi Songs";
            int count = 0;
            DirectoryInfo di = new DirectoryInfo(folderpath);
            var listOfFiles = di.GetFileSystemInfos();
            count = di.GetFiles().Count();
            for (int i = 0; i < count; i++)
            {
                var taglibFile = TagLib.File.Create(listOfFiles[i].FullName);
                var extension = listOfFiles[i].FullName.Split('.').Last();
                for (int j = 0; j < crap.Length; j++)
                {
                    if (listOfFiles[i].FullName.ToLower().Contains(crap[j].ToLower()))
                    {
                        System.IO.File.Move(listOfFiles[i].FullName, listOfFiles[i].FullName.ToLower().Replace("-", ""));
                        System.IO.File.Move(listOfFiles[i].FullName, listOfFiles[i].FullName.ToLower().Replace(crap[j].ToLower(), ""));
                        break;
                    }
                    else if (listOfFiles[i].FullName.ToLower().Contains("_"))
                    {

                        System.IO.File.Move(listOfFiles[i].FullName, listOfFiles[i].FullName.ToLower().Replace("_", ""));
                    }

                }
                if (extension.Contains("mp3"))
                {
                    if (taglibFile.Tag.Album != null)
                    {
                        for (int j = 0; j < crap.Length; j++)
                        {
                            if (taglibFile.Tag.Album.ToLower().Contains(crap[j].ToLower()))
                            {
                                taglibFile.Tag.Album = taglibFile.Tag.Album.ToLower().Replace(crap[j].ToLower(), "");
                                taglibFile.Save();
                            }
                        }
                    }
                    if (taglibFile.Tag.Title != null)
                    {
                        for (int j = 0; j < crap.Length; j++)
                        {
                            if (taglibFile.Tag.Title.ToLower().Contains(crap[j].ToLower()))
                            {
                                taglibFile.Tag.Title = taglibFile.Tag.Title.ToLower().Replace(crap[j].ToLower(), "");
                                taglibFile.Save();
                            }
                        }
                    }
                    if (taglibFile.Tag.Comment != null)
                    {
                        for (int j = 0; j < crap.Length; j++)
                        {
                            if (taglibFile.Tag.Comment.ToLower().Contains(crap[j].ToLower()))
                            {
                                taglibFile.Tag.Comment = taglibFile.Tag.Comment.ToLower().Replace(crap[j].ToLower(), "");
                                taglibFile.Save();
                            }
                        }
                    }
                    if (taglibFile.Tag.FirstAlbumArtist != null)
                    {
                        for (int j = 0; j < crap.Length; j++)
                        {
                            for (int k = 0; k < taglibFile.Tag.AlbumArtists.Length; k++)
                            {
                                if (taglibFile.Tag.AlbumArtists[k].ToLower().Contains(crap[j].ToLower()))
                                {
                                    taglibFile.Tag.AlbumArtists[k] = taglibFile.Tag.AlbumArtists[k].ToLower().Replace(crap[j].ToLower(), "");
                                    taglibFile.Save();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
