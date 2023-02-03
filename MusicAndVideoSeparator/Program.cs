using System.Drawing;
using System;
using System.IO;

namespace MusicAndVideoSeparator
{
    static class Program
    {
        static void Main(string[] args)
        {
            string workingDirectory = @"D:\Memories\Images\";       //22421  4610
            string videoDestinationPrefix = @"D:\Memories\Videos\";
            MultimediaManager multimediaManager = new MultimediaManager(workingDirectory, videoDestinationPrefix);
            //videosManager.PopulateExtensionsList(WorkingDirectory);
            multimediaManager.WorkWithMultimediaFiles();
            //videosManager.FixJpgFileNameToMp4BasedOnSize();
        }
    }
}
