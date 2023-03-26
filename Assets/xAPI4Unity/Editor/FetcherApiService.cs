using System;
using System.Collections;
using System.IO;
using System.Net;
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

        public IEnumerator DownloadExe(string outPath, Action<byte[]> onFinish = null)
        {
            var fileUri = Path.Join(_url.Replace("https", "http"), "static/fetcher-cli-mac.zip");//"download/fetcher-cli.zip?os=Auto");
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