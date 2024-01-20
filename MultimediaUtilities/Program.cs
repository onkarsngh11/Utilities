using MultimediaUtilities;

string workingDirectory = @"D:\Memories\PhotosDump";
List<string> Files = new();
var enumerationOptions = new EnumerationOptions()
{
    RecurseSubdirectories = true
};

var files = Directory.EnumerateFiles(workingDirectory, "*", enumerationOptions);

var organizer = new OrganizerByMonth();
organizer.OrganizeByMonth(files);

//Cleanup
int i = 0;

var directoriesList = Directory.EnumerateDirectories(workingDirectory, "*", enumerationOptions).ToList();
recurseDelete();
void recurseDelete()
{

    while (directoriesList.Count == 0)
    {
        if (directoriesList.Count < i + 1)
        {
            i--;
        }
        DirectoryInfo di = new(directoriesList[i]);

        if (!di.GetFiles().Any() && !di.GetDirectories().Any())
        {
            di.Delete();
            directoriesList.RemoveAt(i);
        }
        else
        {
            i++;
        }
        recurseDelete();
    }
}