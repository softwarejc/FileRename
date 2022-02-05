
namespace FileEditor
{
    // docker build -t filerename .
    // docker run filerename
    class Program
    {
        static void Main()
        {
            var path = Environment.GetEnvironmentVariable("PHOTOS_PATH");
            Console.WriteLine("Root path: " + path);

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                throw new InvalidOperationException("Invalid path.");
            }

            // Get directories and filter the internal Synology dirs (@eaDir)
            var root = new DirectoryInfo(path);
            var directories = new[] { root }.Concat(root.GetDirectories("*", SearchOption.AllDirectories).Where(directory => !directory.Name.Contains("@")));

            foreach (var d in directories)
            {
                IEnumerable<FileInfo> files = new DirectoryInfo(d.FullName).GetFiles("*");

                files = files.Where(f => 
                    !f.Name.Contains(Utils.VID_prefix) && // videos
                    !f.Name.Contains(Utils.IMG_prefix)); // images

                files = files.Where(file => !Utils.ExtensionsToSkip.Any(e => e.Equals(file.Extension.ToUpper())));

                if (files.Count() > 0)
                {
                    Console.WriteLine(files.Count() + " | " + d.FullName);

                    foreach (FileInfo file in files)
                    {
                        Process(file);
                    }
                }
            }
        }

        private static void Process(FileInfo file)
        {
            try
            {
                DateTime? date = null;
                var isVideo = Utils.IsVideoFile(file.FullName);

                try
                {
                    date = Utils.GetDateFromTags(file);
                }
                catch (Exception) { }

                if (!date.HasValue || date.Value.Equals(Utils.DefaultDate))
                {
                    // Fallback to lastWrite time
                    var lastWrite = file.LastWriteTime;
                    var folderDate = Utils.GetDefaultTimeFromFolder(file);

                    if (lastWrite.Year != DateTime.Now.Year) // First fallback, old last write time.
                    {
                        date = lastWrite;
                    }
                    else if (folderDate.Year != 1) // Second fallback, folder with a 4 digits name = year
                    {
                        date = folderDate;
                    }
                    else
                    {
                        Console.WriteLine("Unknown date: " + file.FullName);
                        date = null;
                    }
                }

                if (date.HasValue) Utils.Rename(file, date.Value, isVideo: isVideo);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
