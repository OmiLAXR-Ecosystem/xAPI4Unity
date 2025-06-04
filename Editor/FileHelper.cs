#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace xAPI4Unity.Editor
{
    internal static class FileHelper
    {
        public static string GetRelativePath(string basePath, string fullPath)
        {
            var baseUri = new Uri(AppendDirectorySeparatorChar(basePath));
            var fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }
        public static void CreateZip(string sourceDirectory, string zipPath)
        {
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            using (var zipToCreate = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create))
            {
                var files = Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    // Create a relative path for the archive entry
                    var relativePath = GetRelativePath(sourceDirectory, file).Replace("\\", "/");

                    var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);

                    using (var entryStream = entry.Open())
                    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.CopyTo(entryStream);
                    }
                }
            }

            Debug.Log($"Created zip archive at: {zipPath}");
        }
        public static void ExtractZip(string zipPath, string extractPath)
        {
            // Open the zip file and extract it
            using (var fs = new FileStream(zipPath, FileMode.Open))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    // Define the complete path to extract each file
                    var destinationPath = Path.Combine(extractPath, entry.FullName);

                    // Create the directory for the entry if it's not already present
                    var directoryPath = Path.GetDirectoryName(destinationPath);
                    if (directoryPath != null && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Skip extraction for directory entries
                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        // Manually copy the file stream to the destination file
                        using (var entryStream = entry.Open())
                        {
                            using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                            {
                                entryStream.CopyTo(destinationStream);
                            }
                        }
                    }
                }
            }
        }
        public static void Download(string fileUri, string fileName, Action onComplete = null)
        {
            var wc = new WebClient();
            wc.DownloadFileCompleted += (sender, args) =>
            {
                if (!args.Cancelled && args.Error == null)
                {
                    onComplete?.Invoke();
                }
            };
            wc.DownloadFileAsync(new Uri(fileUri), fileName);
        }
    }
}
#endif