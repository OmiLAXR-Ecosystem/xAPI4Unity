using System;
using System.Diagnostics;

namespace EduXR.xAPI.Editor
{
    public static class ProcessHelper
    {
        public static Process Cmd(string appPath, params string[] args)
            => Cmd(appPath, args, null);
        public static Process Cmd(string appPath, string[] args, Action<Process> configure)
        {
            var arguments = string.Join(" ", args);
            var startInfo = new ProcessStartInfo(appPath)
            {
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
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
            => UnityEngine.Debug.LogError(e.Data);

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            => UnityEngine.Debug.Log(e.Data);
    }
}