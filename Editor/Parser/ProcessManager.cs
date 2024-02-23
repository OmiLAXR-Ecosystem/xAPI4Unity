using System;
using System.Diagnostics;

namespace xAPI4Unity.Editor.Parser
{
    public static class ProcessManager
    {
        public static void Execute(string fileName, string arguments, Action<string> onOutput = null, string workingDirectory = "", bool useShellExecute = false, bool redirectStandardOutput = true)
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = useShellExecute,
                RedirectStandardOutput = redirectStandardOutput,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                
            };
            process.StartInfo = startInfo;
#if DEBUG
            Console.WriteLine($"[Execute] {startInfo.FileName} {startInfo.Arguments}");
#endif
            process.Start();
            while (!process.StandardOutput.EndOfStream)
            {
                var line = process.StandardOutput.ReadLine();
                onOutput?.Invoke(line);
            }
            process.WaitForExit();
        }
    }
}