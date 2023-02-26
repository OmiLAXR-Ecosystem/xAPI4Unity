using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EduXR.xAPI.Editor
{
    public static class EditorHttpHelper
    {
        public static string Get(string uri, string contentType = "application/json")
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "GET";
            request.ContentType = contentType;
            
            try
            {
                using var response = (HttpWebResponse)request.GetResponse();
                Debug.Log("Publish Response: " + (int)response.StatusCode + ", " + response.StatusDescription);
                if (response.StatusCode != HttpStatusCode.OK) 
                    return null;
                using var r = new System.IO.StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException(), Encoding.UTF8);
                var content = r.ReadToEnd();
                response.Close();
                r.Close();

                return content;
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }

            return null;
        }
    }
}