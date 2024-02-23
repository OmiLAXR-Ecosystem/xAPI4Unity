using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using xAPI4Unity.Editor.Parser.Code.SyntaxTree;
using xAPI4Unity.Editor.Parser.Definitions;
using xAPI4Unity.Editor.Parser.IO;

namespace xAPI4Unity.Editor.Parser.Code.CSharp
{
    /// <summary>
    /// Generates C# code files from the xAPI definitions
    /// </summary>
    public sealed class CSharpGenerator : CodeGenerator
    {
        protected override string Namespace { get; }
        
        /// <summary>
        /// Creates a CSharpGenerator
        /// </summary>
        /// <param name="namespace">Namespace</param>
        public CSharpGenerator(string @namespace)
            : base(new CSharpSerializer())
        {
            Namespace = @namespace;
        }

        protected override IEnumerable<Import> CreateActivitiesImports(List<string> classes)
        {
            return new List<Import>
            {
                new Import("System.Collections.Generic"),
            };
        }

        protected override IEnumerable<Import> CreateVerbsImports(List<string> classes)
        {
            return new List<Import>
            {
                new Import("System.Collections.Generic"),
            };
        }

        protected override IEnumerable<Import> CreateExtensionsImports(string type, List<string> classes)
        {
            return new List<Import>
            {
                new Import("System.Collections.Generic"),
            };
        }

        protected override IEnumerable<Import> CreateContextImports(xAPIContext context)
        {
            return new List<Import>();
        }

        protected override IEnumerable<Import> CreateContextExtensionsImports(xAPIContext context)
        {
            return new List<Import>();
        }

        protected override IEnumerable<Import> CreateDefinitionsImports(List<xAPIContext> contexts)
        {
            return new List<Import>();
        }

        protected override IEnumerable<Import> CreateExtensionTypeImports()
        {
            return new List<Import>();
        }

        protected override MethodBody CreateExtensionsMethodBody(xAPIExtension extension)
        {
            var args = $"{Indent(2)}context: Context,{Environment.NewLine}"
                       + $"{Indent(2)}extensionType: ExtensionType,{Environment.NewLine}"
                       + $"{Indent(2)}key: \"{extension.DefName}\",{Environment.NewLine}"
                       + $"{Indent(2)}names: {Serializer.DictionaryToString(extension.Names, 2)},{Environment.NewLine}"
                       + $"{Indent(2)}descriptions: {Serializer.DictionaryToString(extension.Descriptions, 2)}";
            return new MethodBody()
                .AddStatement(new Statement(
                    $"Add(new {ExtensionClassName}({Environment.NewLine}{args}),{Environment.NewLine}{Indent(1)} value);"))
                .AddStatement(new Statement("return this;"));
        }

        protected override MethodBody CreateContextExtensionsGetterBody(xAPIContext context, string type)
        {
            return new MethodBody()
                .AddStatement(new Statement(
                    $"return new {BuildExtensionsClassName(type.Capitalize(), context.Name.Capitalize())}();"));
        }

        public override CoreFiles GenerateCoreFiles()
        {
            var files = base.GenerateCoreFiles();
            foreach (var file in files)
            {
                file.FileContent = file.FileContent.Replace("namespace xAPI.Registry", $"namespace {Namespace}");
            }

            return files;
        }

        private static bool DirContainsDotnetFiles(Dictionary<string, string> coreVersions, string dotnet,
            out string path, params string[] files)
        {
            string foundPath = null;
            foreach (var version in coreVersions.Where(version => version.Key.StartsWith(dotnet)))
            {
                foundPath = version.Value;
            }

            path = foundPath;

            return path != null && DirContainsFiles(path, files);
        }

        private static bool DirContainsFiles(string path, params string[] files)
            => files.All(file => File.Exists(Path.Combine(path, file)));

        private static Dictionary<string, string> GetNetCoreVersionsFix()
        {
            return new Dictionary<string, string>()
            {
                ["6.0.7"] = "/app/ref/net6.0"
            };
        }

#if DEBUG
        private static string RefFolder
        {
            get
            {
                var parentPath = Directory.GetParent(Environment.CurrentDirectory)?.FullName;
                return Path.Combine(parentPath ?? "/", "ref");
            }
        }
#else
        private static readonly string RefFolder = Environment.GetEnvironmentVariable("REF_PATH");
#endif

        private static void ExecuteMsBuild(string arguments, string workingDir = "")
            => ProcessManager.Execute("msbuild", arguments, s =>
            {
#if DEBUG
                Console.WriteLine(s);
#endif
            }, workingDir);

        /// <summary>
        /// Searches the system for installed .Net Core versions
        /// </summary>
        /// <returns>.Net Core versions found on the system</returns>
        public static Dictionary<string, string> GetNetCoreVersions()
        {
            var versions = new Dictionary<string, string>();

            ProcessManager.Execute("dotnet", "--list-runtimes", line =>
            {
                if (!line.StartsWith("Microsoft.NETCore.App"))
                    return;

                var parts = line.Substring(21).Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    return;

                var packsDir = parts[1].Replace("shared", "packs") + ".Ref";

                if (!Directory.Exists(packsDir))
                    return;

                var dirs = Directory.GetDirectories(packsDir);
                foreach (var dir in dirs)
                {
                    var subDir = Path.Combine(dir, "ref");
                    var refDirs = Directory.GetDirectories(subDir);
                    var key = Path.GetFileName(dir);
                    if (refDirs.Length > 0 && !versions.ContainsKey(key))
                    {
                        versions.Add(key, refDirs[0]);
                    }
                }
            });

            return versions.Count < 1 ? GetNetCoreVersionsFix() : versions;
        }

        private static readonly string[] MsBuildVersions = { "4.8" };

        private static readonly string[] DotNetRefFiles =
            { "System.dll", "System.Collections.dll", "System.Runtime.dll", "System.Linq.dll" };

        private static readonly string[] ThirdRefFiles = Array.Empty<string>();
    }
}