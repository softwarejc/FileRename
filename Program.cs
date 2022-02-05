
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
            Console.WriteLine("Root path: " + d.FullName);

            var files = d.GetFiles("*", SearchOption.AllDirectories).Where(f =>
                !f.FullName.Contains("@") &&
                !f.FullName.Contains(Utils.VID_prefix) &&
                !f.FullName.Contains(Utils.IMG_prefix));

            Console.WriteLine(files.Count() + " files found.");

            int cont = 0;
            foreach (FileInfo file in files)
            {
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
            if (file.Name.Contains(Utils.VID_prefix) || file.Name.StartsWith(Utils.IMG_prefix))
            {
                return;
            }

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
