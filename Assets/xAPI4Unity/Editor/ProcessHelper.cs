using System;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace EduXR.xAPI.Editor
{
    public static class ProcessHelper
    {
        public static Process Cmd(string app, string arguments, bool showShell = false, string workingDirectory = default, Action<Process> configure = null)
        {
            Debug.Log("[xAPI4Unity] > " + app + " " + arguments);
            
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
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_ErrorDataReceived;

            configure?.Invoke(process);
            
            process.Start();
            process.BeginOutputReadLine();

            return process;
        }
        
        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.LogError(e.Data);

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.Log(e.Data);
    }
}