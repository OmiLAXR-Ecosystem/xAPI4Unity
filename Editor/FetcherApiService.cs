using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace EduXR.xAPI.Editor {
    public class FetcherApiService
    {
        public const string ZipName = "fetcher-cli.zip";
        private readonly string _url;
        public FetcherApiService(string url)
        {
            _url = url;
        }

        private readonly Dictionary<PlatformID, string> _zipNames = new Dictionary<PlatformID, string>()
        {
            [PlatformID.Unix] = "fetcher-cli-linux.zip",
            [PlatformID.MacOSX] = "fetcher-cli-mac.zip",
            [PlatformID.Win32NT] = "fetcher-cli-win.zip",
            [PlatformID.Win32S] = "fetcher-cli-win.zip",
            [PlatformID.Win32Windows] = "fetcher-cli-win.zip",
            [PlatformID.WinCE] = "fetcher-cli-win.zip",
        }; 
        
        public IEnumerator DownloadExe(string outPath, Action<byte[]> onFinish = null)
        {
            var zipName = _zipNames[Environment.OSVersion.Platform];
            var fileUri = Path.Join(_url, "static/" + zipName);
            Debug.Log("Request cli from " + fileUri);
            var savePath = Path.Join(outPath, ZipName);
            using (var webRequest = UnityWebRequest.Get(fileUri))
            {
                webRequest.SetRequestHeader("accept", "application/x-zip-compression");
                // Send the request to the server
                yield return webRequest.SendWebRequest();

                // Check for errors
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to download file. Error: {webRequest.error}");
                }
                else
                {
                    // Save the downloaded file to disk
                    File.WriteAllBytes(savePath, webRequest.downloadHandler.data);
                    Debug.Log("File download successful.");
                    onFinish?.Invoke(webRequest.downloadHandler.data);
                }
            }
        }
    }
}