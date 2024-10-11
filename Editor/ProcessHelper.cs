#if UNITY_EDITOR
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace xAPI4Unity.Editor
{
    public static class ProcessHelper
    {
        public static Process Cmd(string app, string arguments, bool showShell = false, string workingDirectory = default, Action<Process> configure = null)
        {
            Debug.Log($"[xAPI4Unity] Executed `> {app} {arguments}` in directory '{workingDirectory}'");
            
            var startInfo = new ProcessStartInfo(app)
            {
                Arguments = arguments,
                UseShellExecute = showShell,
                RedirectStandardOutput = !showShell,
                CreateNoWindow = !showShell,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
            };

            var process = new Process()
            {
                StartInfo = startInfo
            };
            
            configure?.Invoke(process);
            process.Start();
            process.WaitForExit();
            
            if (!showShell) 
                process.BeginOutputReadLine();
            
            return process;
        }
    }
}
#endif