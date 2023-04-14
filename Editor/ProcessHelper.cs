using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using File = UnityEngine.Windows.File;

namespace EduXR.xAPI.Editor
{
    public static class ProcessHelper
    {
        [DllImport ("libc", EntryPoint = "chmod", SetLastError = true)]
        private static extern int sys_chmod (string path, uint mode);

        [DllImport("libc", EntryPoint = "exec", SetLastError = true)]
        private static extern void exec(string path);

        public static void AllowExecute(string path) 
            => sys_chmod (path, 0x1ED); // 0x1ED is 0755 
        
        public static Process Cmd(string app, string arguments, bool showShell = false, string workingDirectory = default, Action<Process> configure = null)
        {

            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                var f = Path.Combine(DefaultValues.CliRoot, "fetch.sh");
                
                if (File.Exists(f))
                    File.Delete(f);

                System.IO.File.WriteAllText(f, $"exec {app} {arguments}");
                AllowExecute(f);
                
                Debug.Log("[xAPI4Unity] > Please run manually if unity does not open " + f);
                Process.Start(f);
                return null;
            }
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

            if (!showShell)
            {
                process.OutputDataReceived += Process_OutputDataReceived;
                process.ErrorDataReceived += Process_ErrorDataReceived;
            }

            configure?.Invoke(process);
            
            process.Start();

            process.WaitForExit();
            
            if (!showShell) 
                process.BeginOutputReadLine();
            
            return process;
        }
        
        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.LogError(e.Data);

        private static void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            => Debug.Log(e.Data);
    }
}