using System.Text.RegularExpressions;

namespace MultimediaUtilities
{
    public class OrganizerByMonth
    {
        string organized = @"D:\Memories\OrganizedByMonth\";
        string unorganized = @"D:\Memories\Unorganized\";
        public void OrganizeByMonth(IEnumerable<string> files)
        {

            foreach (var file in files)
            {
                FileInfo fi = new(file);
                try
                {
                    string sourceFileName = fi.Name;
                    string fileType = GetFileType(sourceFileName);
                    if (fileType != null)
                    {
                        DateTime date = ExtractDate(sourceFileName);

                        if (date != DateTime.MinValue && date.Year>= 1994 && date.Year<=2024)
                        {
                            string destinationFolder = Path.Combine(organized, date.ToString("yyyy-MMM"));
                            Directory.CreateDirectory(destinationFolder);

                            string destinationPath = Path.Combine(destinationFolder, sourceFileName);
                            if (File.Exists(destinationPath))
                            {
                                Directory.CreateDirectory(Path.Combine(destinationFolder, "Duplicates"));
                                destinationPath = Path.Combine(destinationFolder, "Duplicates", sourceFileName);
                            }
                            File.Move(fi.FullName, destinationPath);
                        }
                        else
                        {
                            if (fi.CreationTime != DateTime.MinValue)
                            {
                                string destinationFolder = Path.Combine(organized, fi.CreationTime.ToString("yyyy-MMM"));
                                Directory.CreateDirectory(destinationFolder);

                                string destinationPath = Path.Combine(destinationFolder, sourceFileName);

                                File.Move(fi.FullName, destinationPath);
                            }
                        }
                    }
                    else
                    {
                        string destinationPath = Path.Combine(unorganized, sourceFileName);
                        File.Move(fi.FullName, destinationPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    File.Move(fi.FullName, unorganized + fi.Name);
                }
            }
        }

        public string GetFileType(string fileName)
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

        public DateTime ExtractDate(string fileName)
        {
            if (fileName.Length == 15)
            {
                string datePattern1 = @"(\d{2}\d{2}\d{4}\d{3})";
                Match match1 = Regex.Match(fileName, datePattern1);
                if (match1.Success)
                {
                    string dat1 = match1.Groups[1].Value;
                    var dateS1 = dat1.Length == 11 ?
                    dat1.Substring(0, 2) + '-' + dat1.Substring(2, 2) + '-' + dat1.Substring(4, 4) : dat1;
                    return DateTime.Parse(dateS1.Replace('_', '-'));

                    //if (DateTime.TryParseExact(dateStr1, "yyyy-MMM", null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime date1))
                    //{
                    //    return date1;
                    //}
                }
            }
            else if (fileName.Length == 16)
            {
                string datePattern1 = @"(\d{2}\d{2}\d{4}\d{4})";
                Match match1 = Regex.Match(fileName, datePattern1);
                if (match1.Success)
                {
                    string dat1 = match1.Groups[1].Value;
                    var dateS1 = dat1.Length == 12 ?
                    dat1.Substring(0, 2) + '-' + dat1.Substring(2, 2) + '-' + dat1.Substring(4, 4) : dat1;

                    return DateTime.Parse(dateS1.Replace('_', '-'));

                    //if (DateTime.TryParseExact(dateStr1, "yyyy-MMM", null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime date1))
                    //{
                    //    return date1;
                    //}
                }
            }
            else
            {

                string datePattern = @"(?:(\d{4}\d{2}\d{2})|(\d{4}[-_]\d{2}[-_]\d{2})|(\d{2}\d{2}\d{4}d{3}))";

                Match match = Regex.Match(fileName, datePattern);

                if (match.Success)
                {
                    string dat = match.Groups[1].Value;
                    if (dat == "" && match.Groups.Count > 1)
                    {
                        dat = match.Groups[2].Value;
                    }
                    var dateS = dat.Length == 8 ? dat.Substring(0, 4) + '-' + dat.Substring(4, 2) + '-' + dat.Substring(6, 2) : dat;
                    //.Length == 11 ?
                    //dat.Substring(0, 2) + '-' + dat.Substring(2, 2) + '-' + dat.Substring(4, 4) : dat;
                    if (DateTime.TryParse(dateS.Replace('_', '-'), out DateTime date))
                    { return date; }
                    //if (DateTime.TryParseExact(dateStr, "yyyy-MMM", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                    //{
                    //    return date;
                    //}
                }
            }
            return DateTime.MinValue;
        }

    }
}
