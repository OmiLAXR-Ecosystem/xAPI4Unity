using System;
using System.IO;
using System.Linq;

namespace xAPI4Unity.Editor.Parser
{
    public delegate string StringTransform(string content);
    public static class ResourceManager
    {
        /// <summary>
        /// Copy resource by name to an output folder assign namespace to it.
        /// </summary>
        /// <param name="resourcePath">Path of resource</param>
        /// <param name="outputFolder">Path to output folder.</param>
        public static void CopyResource(string resourcePath, string outputFolder, params StringTransform[] transforms)
        {
            var name = Path.GetFileName(resourcePath);
            var dir = Path.GetDirectoryName(resourcePath);

            var resourceDir = dir == "" ? dir : dir + ".";

            // Fetch resource content
            var content = GetContentFromResource(resourceDir + name);

            // apply string transforms
            content = transforms.Aggregate(content, (current, transform) => transform(current));

            // Make line endings consistent
            var cleanedContent = content.Replace("\r\n", Environment.NewLine);
            
            // Create absolute path for file
            var destFile = Path.Combine(outputFolder, resourcePath);

            var fullDir = Path.Combine(outputFolder, dir);
            // Ensure the path exists
            if (!Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);

            // Delete old version of file
            if (File.Exists(destFile))
                File.Delete(destFile);
            
            File.WriteAllText(destFile, cleanedContent);
        }
        
        /// <summary>
        /// Get text content of a resource.
        /// </summary>
        /// <param name="resourceName">The resource name.</param>
        private static string GetContentFromResource(string resourceName)
        {
            // Get resource file assembly reference to reading content of it.
            var assembly = typeof(ResourceManager).Assembly;
            using (var stream = assembly.GetManifestResourceStream("Parser.Resources." + resourceName))
            {
                // Read content
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }
}
