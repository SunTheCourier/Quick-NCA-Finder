using System.IO;

namespace Quick_NCA_Finder
{
    //HACGUI extensions reeeeeeeeeeeeeeeeeeeeeeeeeeeeeee
    public static class Extensions
    {
        public static FileInfo GetFile(this DirectoryInfo obj, string filename) => new FileInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{filename}");
    }
}

