
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

            var root = new DirectoryInfo(path);
            var directories = new[] { root }.Concat(root.GetDirectories("*", SearchOption.AllDirectories));

            int filesRenamed = 0;
            foreach (var d in directories)
            {
                IEnumerable<FileInfo> files = new DirectoryInfo(d.FullName).GetFiles("*");

                files = files.Where(f =>
                    !f.Name.Contains(Utils.VID_prefix) && // exclude renamed videos 
                    !f.Name.Contains(Utils.IMG_prefix)); // exlude renamed images

                files = files.Where(file => !Utils.ExtensionsToSkip.Any(e => e.Equals(file.Extension.ToUpper()))); // exclude some extensions
                files = files.Where(file => !string.IsNullOrEmpty(file.Extension.ToUpper())); // exclude files without extension
                files = files.Where(file => !file.FullName.Contains("@eaDir")); // exclude Synology internal files (@eaDir)

                if (files.Count() > 0)
                {
                    Console.WriteLine(files.Count() + " file (s) | " + d.FullName);

                    foreach (FileInfo file in files)
                    {
                        Process(file);
                        filesRenamed++;
                    }
                }
            }

            Console.WriteLine(filesRenamed +  " files renamed.");
            Environment.Exit(0);
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

                    if (lastWrite.Year != DateTime.Now.Year) // First fallback, last write time not in this year
                    {
                        date = lastWrite;
                    }
                    else if (folderDate.Year != 1) // Second fallback, folder with a 4 digits name = year
                    {
                        date = folderDate;
                    }
                    else
                    {
                        date = lastWrite; // Last fallback, last write time
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
