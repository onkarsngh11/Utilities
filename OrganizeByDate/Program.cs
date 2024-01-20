using System.Text.RegularExpressions;

string workingDirectory = @"D:\Memories\Organized";
string organized = @"D:\Memories\Organized";
string unorganized = @"D:\Memories\Unorganized\";
List<string> Files = new();
IterateDirectories();
OrganizeByMonth(organized);

void IterateDirectories()
{
    var directories = Directory.EnumerateDirectories(workingDirectory);
    var newList = directories.ToList();
    newList.Add(workingDirectory);
    foreach (string directory in newList)
    {
        var files = Directory.GetFiles(directory);
        foreach (string file in files)
        {
            Files.Add(file);
        }
    }
}

//IterateFiles();
void IterateFiles() {
    foreach (var sourceFileName in Files)
    {
        FileInfo fi = new(sourceFileName);
        try
        {
            OrganizeByDateInNameIntoMonth(fi);
        }
        catch( Exception ex)
        {
            Console.WriteLine(ex.ToString());
            File.Move(sourceFileName, unorganized + fi.Name);
        }
    }
}
void OrganizeByDateInNameIntoMonth(FileInfo fi)
{
    string sourceFileName = fi.Name;
    string fileType = GetFileType(sourceFileName);

    if (fileType != null)
    {
        DateTime date = ExtractDate(sourceFileName);

        if (date != DateTime.MinValue)
        {
            string destinationFolder = Path.Combine(organized, date.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(destinationFolder);

            string destinationPath = Path.Combine(destinationFolder, sourceFileName);
            File.Move(fi.FullName, destinationPath);
        }
    }
    else
    {
        string destinationPath = Path.Combine(unorganized, sourceFileName);
        File.Move(fi.FullName, destinationPath);
    }
}
static string GetFileType(string fileName)
{
    if (fileName.StartsWith("IMG_") || fileName.StartsWith("IMG-"))
        return "IMG";
    else if (fileName.StartsWith("VID_") || fileName.StartsWith("VID-"))
        return "VID";
    else if (fileName.StartsWith("Screenshot_"))
        return "Screenshot";
    else if (fileName.StartsWith("Inshot_"))
        return "Inshot";
    else
        return "date";
}
static DateTime ExtractDate(string fileName)
{
    string datePattern = @"(?:(\d{4}\d{2}\d{2})|(\d{4}[-_]\d{2}[-_]\d{2}))";
    Match match = Regex.Match(fileName, datePattern);

    if (match.Success)
    {
        string dat = match.Groups[1].Value;
        var dateS = dat.Length == 8 ? dat.Substring(0, 4) + '-' + dat.Substring(4, 2) + '-' + dat.Substring(6, 2) : dat;
        string dateStr = dateS.Replace('_', '-');
        if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
        {
            return date;
        }
    }

    return DateTime.MinValue;
}

static void OrganizeByMonth(string organizedDirectory)
{
    foreach (var directory in Directory.EnumerateDirectories(organizedDirectory))
    {
        DirectoryInfo di = new(directory);
        var month = DateTime.Parse(di.Name).ToString("yyyy-MMM");
        var destPath = Path.Combine(organizedDirectory, month);

        Directory.CreateDirectory(destPath);

        var files = Directory.GetFiles(directory);
        var count = files.Count();
        foreach (var file in files)
        {
            FileInfo fi = new FileInfo(file);
            var destFile = Path.Combine(destPath, fi.Name);
            File.Move(file, destFile);
        }
        if (!di.GetFiles().Any())
        {
            di.Delete();
        }
    }
}