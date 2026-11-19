/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Provides helper methods for executing external processes (e.g., shell commands).
    /// </summary>
    public static class ProcessHelper
    {
        /// <summary>
        /// Executes a command-line process with specified arguments and options.
        /// </summary>
        /// <param name="app">The name or path of the executable to run.</param>
        /// <param name="arguments">The command-line arguments to pass to the executable.</param>
        /// <param name="showShell">Indicates whether to display the shell window during execution.</param>
        /// <param name="workingDirectory">The working directory to execute the process in. Defaults to the current directory.</param>
        /// <param name="configure">An optional callback to configure the <see cref="Process"/> before starting it.</param>
        /// <returns>The <see cref="Process"/> instance after execution.</returns>
        public static Process Cmd(string app, string arguments, bool showShell = false, string workingDirectory = default, Action<Process> configure = null)
        {
            // Log the command execution for debugging purposes
            Debug.Log($"[xAPI4Unity] Executed `> {app} {arguments}` in directory '{workingDirectory}'");
            
            // Set up the process start information
            var startInfo = new ProcessStartInfo(app)
            {
                Arguments = arguments,
                UseShellExecute = showShell,
                RedirectStandardOutput = !showShell, // Redirect output if not showing the shell
                CreateNoWindow = !showShell, // Avoid creating a window if shell is not shown
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory // Default to current directory if not specified
            };

            // Create the process with the specified start information
            var process = new Process()
            {
                StartInfo = startInfo
            };
            
            // Apply any additional configuration provided
            configure?.Invoke(process);

            // Start the process
            process.Start();

            // Wait for the process to exit if the shell is hidden
            process.WaitForExit();

            // Begin reading standard output if shell is not displayed
            if (!showShell) 
                process.BeginOutputReadLine();
            
            return process;
        }

        /// <summary>
        /// Runs a command-line process without a window, waits for it and returns its exit code and output.
        /// </summary>
        /// <param name="app">The name or path of the executable to run.</param>
        /// <param name="arguments">The command-line arguments to pass to the executable.</param>
        /// <param name="workingDirectory">The working directory to execute the process in.</param>
        /// <param name="output">Standard output and standard error, trimmed.</param>
        /// <returns>The exit code, 0 on success.</returns>
        internal static int Run(string app, string arguments, string workingDirectory, out string output)
        {
            Debug.Log($"[xAPI4Unity] Executed `> {app} {arguments}` in directory '{workingDirectory}'");
            var startInfo = new ProcessStartInfo(app)
            {
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                // Read stderr asynchronously, so a full pipe cannot block the process
                var error = process.StandardError.ReadToEndAsync();
                var stdout = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                output = (stdout + error.Result).Trim();
                return process.ExitCode;
            }
        }
    }
}
#endif