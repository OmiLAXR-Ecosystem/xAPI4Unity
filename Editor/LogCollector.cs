/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace xAPI4Unity.Editor
{
    /// <summary>
    /// Collects the errors and warnings Unity logs while an action runs, so that the UI can show them.
    /// The messages still reach the Console.
    /// </summary>
    internal sealed class LogCollector
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        private LogCollector() { }

        /// <summary>
        /// Runs an action and collects what it logs. An exception thrown by the action counts as an error.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <returns>The collected errors and warnings.</returns>
        public static LogCollector Run(Action action)
        {
            var collector = new LogCollector();
            Exception exception = null;
            Application.logMessageReceived += collector.Collect;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
                collector.Errors.Add(ex.Message);
            }
            finally
            {
                Application.logMessageReceived -= collector.Collect;
            }

            // Logged after unsubscribing, so the exception is not collected twice
            if (exception != null)
                Debug.LogException(exception);
            return collector;
        }

        private void Collect(string message, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    Errors.Add(message);
                    break;
                case LogType.Warning:
                    Warnings.Add(message);
                    break;
            }
        }
    }
}
#endif
