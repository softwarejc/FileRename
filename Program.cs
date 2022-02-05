
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

            // Get files that were not renamed yet
            var files = new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories).Where(f =>
                !f.Name.Contains(Utils.VID_prefix) &&
                !f.Name.Contains(Utils.IMG_prefix));

            // Filter files that are not images
            files = files.Where(f => f.FullName.Contains("@") || Utils.ExtensionsToSkip.Contains(f.Extension.ToUpper()));

            Console.WriteLine(files.Count() + " files found.");

            int cont = 0;
            foreach (FileInfo file in files)
            {
                // Give some feedback
                if (cont % 100 == 0)
                {
                    Console.WriteLine("- " + (files.Count() - cont) + " files left");
                }
                cont++;

                Process(file);
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
