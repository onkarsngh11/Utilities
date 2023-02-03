using System.Drawing;
using System;
using System.IO;

namespace MusicAndVideoSeparator
{
    static class Program
    {
        static void Main(string[] args)
        {
            string WorkingDirectory = @"D:\Memories\Images\1 Onkar wed\3 Sagan\mp4";
            FileManager videosManager = new FileManager(WorkingDirectory);
            //videosManager.PopulateExtensionsList(WorkingDirectory);
            //videosManager.MoveVideos();
            //videosManager.FixJpgFileNameToMp4BasedOnSize();
            videosManager.RenameImagesToMp4();
        }
    }
}
