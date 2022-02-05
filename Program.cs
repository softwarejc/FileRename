using RedCorners.ExifLibrary;

namespace FileEditor
{
    // docker build -t filerename .
    // docker run filerename
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;

            DirectoryInfo d = new DirectoryInfo(args[0]);

            Console.WriteLine(Directory.GetCurrentDirectory());
            Console.WriteLine("Path - " + d.FullName);

            var files = d.GetFiles("*", SearchOption.AllDirectories)
                .Where(f => !f.FullName.Contains("@") && 
                !f.FullName.Contains(Utils.VID_prefix) &&
                !f.FullName.Contains(Utils.IMG_prefix));

            Console.WriteLine(files.Count() + " files found.");

            int cont = 0;
            foreach (FileInfo file in files)
            {
                cont++;

                if (cont % 100 == 0)
                {
                    Console.WriteLine("- " + (files.Count() - cont) + " files left");
                }

                Process(file);
            }
        }

        private static void Process(FileInfo file)
        {
            if (file.Name.Contains(Utils.VID_prefix) || file.Name.StartsWith(Utils.IMG_prefix))
            {
                return;
            }

            try
            {
                DateTime? date = null;
                var isVideo = IsVideoFile(file.FullName);

                try
                {
                    date = Utils.GetDateFromTags(file);
                }
                catch (Exception)
                {
                    try
                    {
                        // 2nd method
                        var imageFile = ImageFile.FromFile(file.FullName);
                        date = Utils.GetDateFromTagsOnlyImages(file, imageFile);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (!date.HasValue || date.Value.Equals(Utils.DefaultDate))
                {
                    // Fallback to lastWrite time
                    var lastWrite = file.LastWriteTime;
                    var folderDate = Utils.GetDefaultTimeFromFolder(file);

                    if (lastWrite.Year != DateTime.Now.Year)
                    {
                        date = lastWrite;
                    }
                    else if (folderDate.Year != 1)
                    {
                        date = folderDate;
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Warning. " + file.FullName);
                        Console.WriteLine();
                        date = null;
                    }
                }

                if (date.HasValue) Utils.Rename(file, date.Value, isVideo: isVideo);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        static readonly string[] _mediaExtensions = { ".MOV", ".AVI", ".MP4", ".DIVX", ".WMV", ".LVIX" };

        static bool IsVideoFile(string path)
        {
            return -1 != Array.IndexOf(_mediaExtensions, Path.GetExtension(path).ToUpperInvariant());
        }
    }
}
