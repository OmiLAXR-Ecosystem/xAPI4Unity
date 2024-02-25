#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;

namespace xAPI4Unity.Editor.Parser.IO {
    /// <summary>
    /// Struct that represents a file or directory
    /// </summary>
    internal class FileMeta
    {
        /// <summary>
        /// Name of the file
        /// </summary>
        public string Name;
        /// <summary>
        /// Path of the file
        /// </summary>
        public string Path;

        /// <summary>
        /// Content of the file as a string
        /// </summary>
        public string FileContent { get; set; }
        /// <summary>
        /// Content of the directory as a list of FileMetas
        /// </summary>
        public List<FileMeta> DirContent { get; set; }

        /// <summary>
        /// True, if the FileMeta is a directory, false if it's a file
        /// </summary>
        public bool IsDirectory => DirContent != null;

        public FileMeta() { }

        /// <summary>
        /// Creates a FileMeta
        /// </summary>
        /// <param name="name">Name of the file</param>
        /// <param name="path">Path of the file</param>
        /// <param name="content">Content of the file</param>
        public FileMeta(string name, string path, string content = "") {
            Name = name;
            Path = path;
            FileContent = content;
        }

        /// <summary>
        /// Creates a FileMeta
        /// </summary>
        /// <param name="name">Name of the file</param>
        /// <param name="path">Path of the file</param>
        /// <param name="files">List of files in the directory</param>
        public FileMeta(string name, string path, List<FileMeta> files) {
            Name = name;
            Path = path;
            DirContent = files;
        }

        public void AppendToDirContent(FileMeta fileMeta)
        {
            if (DirContent == null)
                DirContent = new List<FileMeta>();
            DirContent.Add(fileMeta);
        }
        
        public void Save(string destination)
        {
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);
            
            Path = destination;
            
            if (IsDirectory)
            {
                DirContent.ForEach(d => d.Save(destination));
                return;
            }
            
            var filePath = System.IO.Path.Combine(destination, Name);
            using var fs = new StreamWriter(filePath);
            fs.Write(FileContent);
        }

        /// <summary>
        /// [File: Name={Name}, Path={Path}, Content={Content}]
        /// </summary>
        public override string ToString() => $"[File: Name=\"{Name}\", Path=\"{Path}\", Content=\"{(IsDirectory ? DirContent.ToString() : FileContent)}\"]";
    }
}
#endif