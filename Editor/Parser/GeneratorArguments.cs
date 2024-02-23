using System;

namespace xAPI4Unity.Editor.Parser
{

    public struct GeneratorArguments
    {
        public string Path { get; }
        public string ProjectId { get; }
        public string Branch { get; }
        public string PrivateKey { get; }
        public string Namespace { get; }
        public string Output { get; set; }
        public bool CreateAsmdef { get; set; }
        public Action<string> OnLog { get; }
        public string Version { get; }
        public string[] ExplicitDefinitions { get; }

        public GeneratorArguments(string path, string projectId, string branch, string privateKey,
            string @namespace, string output, bool createAsmdef = false, string version = "1.0.0", Action<string> onLog = null,
            string[] explicitDefinitions = null)
        {
            CreateAsmdef = createAsmdef;
            Path = path;
            ProjectId = projectId;
            Branch = branch;
            PrivateKey = privateKey;
            Namespace = @namespace;
            Output = output;
            Version = version;
            OnLog = onLog;
            ExplicitDefinitions = explicitDefinitions;
        }
    }
}