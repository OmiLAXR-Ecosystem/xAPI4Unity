/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Provides utility methods for file operations such as creating zip archives, extracting them, downloading files, and path manipulations.
    /// </summary>
    internal static class FileHelper
    {
        /// <summary>
        /// Gets the relative path between a base directory and a target file or directory.
        /// </summary>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="fullPath">The full path to the target file or directory.</param>
        /// <returns>A string representing the relative path.</returns>
        public static string GetRelativePath(string basePath, string fullPath)
        {
            var baseUri = new Uri(AppendDirectorySeparatorChar(basePath));
            var fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Appends a directory separator character to the end of the path if it does not already exist.
        /// </summary>
        /// <param name="path">The path to modify.</param>
        /// <returns>The modified path with a directory separator character at the end.</returns>
        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        /// <summary>
        /// Ensures that a directory path exists. If it does not exist, it will be created.
        /// </summary>
        /// <param name="path">The path to check and create if necessary.</param>
        public static void EnsurePath(string path)
        {
            if (path.Contains("."))
            {
                var file = Path.GetFileName(path);
                path = path.Replace(file, "");
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Creates a zip archive from the contents of a source directory.
        /// </summary>
        /// <param name="sourceDirectory">The directory containing files to include in the zip archive.</param>
        /// <param name="zipPath">The path where the zip archive will be created.</param>
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

        /// <summary>
        /// Extracts a zip archive to a destination directory.
        /// </summary>
        /// <param name="zipPath">The path to the zip archive to extract.</param>
        /// <param name="extractPath">The directory where the archive contents will be extracted.</param>
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
                        using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                        {
                            entryStream.CopyTo(destinationStream);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Downloads a file from a given URI to the specified local path.
        /// </summary>
        /// <param name="fileUri">The URI of the file to download.</param>
        /// <param name="fileName">The local path where the downloaded file will be saved.</param>
        /// <param name="onComplete">An optional callback to invoke after the download completes successfully.</param>
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