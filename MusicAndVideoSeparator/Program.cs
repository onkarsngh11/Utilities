using System.Drawing;
using System;
using System.IO;

namespace MusicAndVideoSeparator
{
    static class Program
    {
        static void Main(string[] args)
        {
            string WorkingDirectory = @"D:\Memories\Images\";
            MultimediaManager multimediaManager = new MultimediaManager(WorkingDirectory);
            //videosManager.PopulateExtensionsList(WorkingDirectory);
            multimediaManager.WorkWithMultimediaFiles();
            //videosManager.FixJpgFileNameToMp4BasedOnSize();
        }
    }
}
