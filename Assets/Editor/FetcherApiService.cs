using System;
using System.IO;
using System.Net;

namespace EduXR.xAPI.Editor {
    public class FetcherApiService
    {
        public const string ZipName = "fetcher-cli.zip";
        private readonly string _url;
        public FetcherApiService(string url)
        {
            _url = url;
        }

        public FetcherApiService DownloadExe(string outPath, DownloadDataCompletedEventHandler onFinish = null)
        {
            var fileUri = Path.Join(_url, "download/fetcher-cli.zip?os=Auto");
            var fileName = Path.Join(outPath, ZipName);
            var client = new WebClient();
            
            client.Headers.Add("accept", "*/*");

            var bytes = client.DownloadString(new Uri(fileUri));
            
            
            using var fs = new FileStream(fileName, FileMode.CreateNew);
           // fs.Write(bytes);
            fs.Flush(true);
            fs.Close();
            onFinish?.Invoke(null, null);
            return this;
        }
    }
}