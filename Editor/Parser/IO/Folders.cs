using System.IO;

namespace xAPI4Unity.Editor.Parser.IO
{
    public static class Folders
    {
        public static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        
        public static void EnsureRootFolder(string @namespace, string destination)
        {
            EnsureFolder(Path.Combine(destination, @namespace));
        }
    }
}