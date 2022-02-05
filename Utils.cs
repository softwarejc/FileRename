using MetadataExtractor;
using MetadataExtractor.Formats.Avi;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using System.Globalization;

namespace FileEditor
{
    internal static class Utils
    {
        internal static readonly DateTime DefaultDate = default(DateTime); // default

        internal static readonly string IMG_prefix = "PHOTO_";
        internal static readonly string VID_prefix = "VIDEO_";

        internal static string[] DatePatterns => _datePatterns;

        internal static readonly string[] ExtensionsToSkip = { ".INI", ".DB", ".EXE" };

        internal static readonly string[] MediaExtensions = { ".MOV", ".AVI", ".MP4", ".DIVX", ".WMV", ".LVIX" };

        internal static void Rename(FileInfo file, DateTime newNameDate, bool isVideo)
        {
            string prefix = IMG_prefix;
            if (isVideo)
            {
                prefix = VID_prefix;
            }

            var datePart = newNameDate.ToString("yyyyMMdd_HHmmss_");
            if (newNameDate.Year < 1986)
            {
                datePart = "00000000_000000_";
            }


            var newName = Path.Combine(file.Directory.FullName, prefix + datePart + file.Length + file.Extension.ToLower());

            if (file.FullName == newName)
            {
                return;
            }

            var cont = 0;
            while (File.Exists(newName))
            {
                cont++;
                newName = Path.Combine(file.Directory.FullName, prefix + datePart + file.Length + "_copy_" + cont + file.Extension.ToLower());
            }

            Console.WriteLine("Rename " + file.FullName + " to " + newName);
            File.Move(file.FullName, newName);
        }

        internal static DateTime GetDefaultTimeFromFolder(FileInfo file)
        {
            // try to get default from folder name
            var folder = System.IO.Directory.GetParent(file.FullName).Name;
            int folderYear;
            if (int.TryParse(folder, out folderYear))
            {
                return new DateTime(folderYear, 1, 1);
            }

            folder = System.IO.Directory.GetParent(file.FullName).Parent.Name;
            if (int.TryParse(folder, out folderYear))
            {
                return new DateTime(folderYear, 1, 1);
            }

            return DefaultDate;
        }

        internal static DateTime GetDateFromTags(FileInfo file)
        {
            DateTime result = DefaultDate;

            // Print all metadata
            //var metadataList = ImageMetadataReader.ReadMetadata(file.FullName);
            //foreach (var metadataGroup in metadataList)
            //    foreach (var tag in metadataGroup.Tags)
            //        Console.WriteLine($"{metadataGroup.Name} - {tag.Name} = {tag.Description}");

            var tags = ImageMetadataReader.ReadMetadata(file.FullName);

            // Generic Exif
            var subIfdDirectory = tags.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var found = (subIfdDirectory != null) && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out result);

            if (!found)
            {
                found = (subIfdDirectory != null) && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTime, out result);
            }

            if (!found)
            {
                var pngDirecectories = tags.OfType<MetadataExtractor.Formats.Png.PngDirectory>().ToList();
                foreach (var pngDirectory in pngDirecectories)
                {
                    // Print all metadata
                    foreach (var tag in pngDirectory.Tags)
                    {
                        // Console.WriteLine($"{pngDirectory.Name} - {tag.Name} = {tag.Description}");
                        if (tag.Description.Contains("Creation Time: "))
                        {
                            result = ParseDateTime(tag.Description.Replace("Creation Time: ", ""));

                            if (result != DefaultDate)
                            {
                                found = true;
                            }
                        }
                    }

                    if (found) continue;
                }
            }

            if (!found)
            {
                found = (subIfdDirectory != null) && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out result);
            }

            if (!found)
            {
                var extension = file.Extension.ToLower();
                if (extension == ".mp4" || extension == ".mov")
                {
                    var directories = new List<MetadataExtractor.Directory>();

                    using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        directories.AddRange(QuickTimeMetadataReader.ReadMetadata(stream));

                    // Mov
                    var movMetadata = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
                    var dateText = movMetadata?.GetDescription(QuickTimeMovieHeaderDirectory.TagCreated);
                    if (string.IsNullOrEmpty(dateText))
                    {
                        dateText = movMetadata?.GetDescription(QuickTimeMetadataHeaderDirectory.TagCreationDate);
                    }

                    if (dateText != null)
                    {
                        dateText = dateText.Substring(4, dateText.Length - 4);
                        result = ParseDateTime(dateText);
                        found = true;
                    }
                }
                else if (extension == ".avi")
                {
                    // Avi
                    var aviMetadata = tags.OfType<AviDirectory>().FirstOrDefault();
                    var dateText = aviMetadata?.GetDescription(AviDirectory.TagDateTimeOriginal);

                    if (dateText != null)
                    {
                        dateText = dateText.Substring(4, dateText.Length - 4);
                        result = ParseDateTime(dateText);
                        found = true;
                    }

                }

                if (!found)
                {
                    // try to get date from modified time
                    throw new InvalidOperationException("Date not found");
                }
            }

            return result;
        }

        internal static bool IsVideoFile(string path)
        {
            return -1 != Array.IndexOf(MediaExtensions, Path.GetExtension(path).ToUpperInvariant());
        }

        internal static readonly string[] _datePatterns =
        {
            "yyyy:MM:dd HH:mm:ss",
            "MMM dd HH:mm:ss yyyy",
            "MMM d HH:mm:ss yyyy",
            "yyyy:MM:dd HH:mm:ss.fff",
            "yyyy:MM:dd HH:mm:ss.fffzzz",
            "yyyy:MM:dd HH:mm:ss",
            "yyyy:MM:dd HH:mm:sszzz",
            "yyyy:MM:dd HH:mm",
            "yyyy:MM:dd HH:mmzzz",
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss.fffzzz",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:sszzz",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd HH:mmzzz",
            "yyyy.MM.dd HH:mm:ss",
            "yyyy.MM.dd HH:mm:sszzz",
            "yyyy.MM.dd HH:mm",
            "yyyy.MM.dd HH:mmzzz",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.fffzzz",
            "yyyy-MM-ddTHH:mm:ss.ff",
            "yyyy-MM-ddTHH:mm:ss.f",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mmzzz",
            "yyyy:MM:dd",
            "yyyy-MM-dd",
            "yyyy-MM",
            "yyyyMMdd", // as used in IPTC data
            "yyyy"
        };


        private static DateTime ParseDateTime(string dateText)
        {
            if (string.IsNullOrEmpty(dateText))
            {
                return DefaultDate;
            }

            List<CultureInfo> cultures = new List<CultureInfo> { CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("es-ES") };

            foreach (var culture in cultures)
            {
                DateTime date;
                foreach (var pattern in DatePatterns)
                {
                    if (DateTime.TryParseExact(dateText, pattern, culture, DateTimeStyles.AllowWhiteSpaces, out date))
                    {
                        return date;
                    }
                }
            }

            return DefaultDate;
        }

        public static DateTime ChangeYear(this DateTime dt, int newYear)
        {
            return dt.AddYears(newYear - dt.Year);
        }
    }
}




