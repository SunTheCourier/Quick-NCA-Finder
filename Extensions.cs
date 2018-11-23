using System.IO;

namespace Quick_NCA_Finder
{
    //HACGUI extensions reeeeeeeeeeeeeeeeeeeeeeeeeeeeeee
    public static class Extensions
    {
        public static FileInfo GetFile(this DirectoryInfo obj, string filename)
        {
            return new FileInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{filename}");
        }

        public static bool ContainsFile(this DirectoryInfo obj, string filename)
        {
            return obj.GetFile(filename).Exists;
        }

        public static DirectoryInfo GetDirectory(this DirectoryInfo obj, string foldername)
        {
            return new DirectoryInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{foldername}");
        }
    }
}

