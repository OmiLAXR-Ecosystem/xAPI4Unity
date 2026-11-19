/*
* SPDX-License-Identifier: AGPL-3.0-or-later
* Copyright (C) 2025 Sergej Görzen <sergej.goerzen@gmail.com>
* This file is part of xAPI4Unity.
*/
using System;
using UnityEngine.Networking;

namespace xAPI4Unity.Editor.Releases
{
    /// <summary>
    /// Asynchronous HTTP requests for the editor. The callbacks run on the main thread, so they may use the AssetDatabase.
    /// </summary>
    internal static class WebRequests
    {
        /// <summary>
        /// User agent of the requests. GitHub's API rejects requests without one.
        /// </summary>
        private const string UserAgent = "xAPI4Unity";

        /// <summary>
        /// Downloads a text, such as a JSON response.
        /// </summary>
        /// <param name="url">URL of the text.</param>
        /// <param name="done">Gets the text, or null and the error.</param>
        public static void GetText(string url, Action<string, string> done)
        {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", UserAgent);
            request.SetRequestHeader("Accept", "application/vnd.github+json");
            request.SendWebRequest().completed += _ =>
            {
                var error = ErrorOf(request);
                var text = error == null ? request.downloadHandler.text : null;
                request.Dispose();
                done(text, error);
            };
        }

        /// <summary>
        /// Downloads a file, such as a zip archive.
        /// </summary>
        /// <param name="url">URL of the file.</param>
        /// <param name="path">Where to save the file. A failed download leaves no file.</param>
        /// <param name="done">Gets null on success, otherwise the error.</param>
        public static void DownloadFile(string url, string path, Action<string> done)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerFile(path) { removeFileOnAbort = true }
            };
            request.SetRequestHeader("User-Agent", UserAgent);
            request.SendWebRequest().completed += _ =>
            {
                var error = ErrorOf(request);
                request.Dispose();
                done(error);
            };
        }

        /// <summary>
        /// The error of a finished request, or null if it succeeded. Names GitHub's rate limit, which is the usual cause of a 403.
        /// </summary>
        private static string ErrorOf(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.Success)
                return null;
            if (request.responseCode == 403 && request.GetResponseHeader("X-RateLimit-Remaining") == "0")
                return "GitHub's rate limit for requests without sign-in is reached. Try again in an hour.";
            return request.responseCode > 0
                ? $"HTTP {request.responseCode} from {request.url}: {request.error}"
                : $"{request.error} ({request.url})";
        }
    }
}
