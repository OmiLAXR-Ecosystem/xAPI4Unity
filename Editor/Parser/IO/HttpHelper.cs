using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace xAPI4Unity.Editor.Parser.IO {
    /// <summary>
    /// Class with HTTP access methods
    /// </summary>
    public static class HttpHelper {
        private static readonly string GitLabEndpoint = GetConnectionString("GitLabEndpoint");

        private static readonly HttpClient client = new HttpClient();

        private static JArray actions;
        /// <summary>
        /// True, if actions is not null, else false
        /// </summary>
        public static bool IsActionsInitialized => actions != null;

        /// <summary>
        /// Builds a URI with arguments
        /// </summary>
        /// <param name="uriBase">Base of the URI</param>
        /// <param name="args">Arguments</param>
        /// <returns>Created URI</returns>
        public static string BuildUriArgs(string uriBase, params KeyValuePair<string, string>[] args) {
            var uri = uriBase;
            for (var i = 0; i < args.Length; ++i) {
                if (i == 0) {
                    uri += "?";
                } else {
                    uri += "&";
                }
                uri += args[i].Key + "=" + args[i].Value;
            }
            return uri;
        }

        /// <summary>
        /// Builds a URI out of paths
        /// </summary>
        /// <param name="uriBase">Base of the URI</param>
        /// <param name="paths">Paths</param>
        /// <returns>Created URI</returns>
        public static string BuildUriPaths(string uriBase, params string[] paths)
            => paths.Aggregate(uriBase, (current, path) => current + (current == string.Empty ? path : "/" + path));

        private static string GetConnectionString(string name)
        {

            return "todo";
        }

        /// <summary>
        /// Gets a tree from GitLab
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="branch">Branch</param>
        /// <param name="path">Path to get the tree of</param>
        /// <param name="privateKey">Private key</param>
        /// <param name="rec">Recursively get files or only those directly in the specified path</param>
        /// <returns>Retrieved GitLab tree</returns>
        public static List<string> GetGitLabTree(string projectId,
            string branch, string path, string privateKey, bool rec = false) {
            var uriBase = BuildUriPaths(GitLabEndpoint, "projects", projectId, "repository", "tree");
            var uri = BuildUriArgs(uriBase,
                new KeyValuePair<string, string>("ref", branch),
                new KeyValuePair<string, string>("path", Uri.EscapeDataString(path)),
                new KeyValuePair<string, string>("pagination", "keyset"),
                new KeyValuePair<string, string>("per_page", "100"),
                new KeyValuePair<string, string>("recursive", rec.ToString().ToLower()));

            var gets = new List<string>();
            var morePages = true;
            while (morePages)
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    request.Headers.Add("PRIVATE-TOKEN", privateKey);
                    using (var response = client.SendAsync(request))
                    {
                        gets.Add(response.Result.Content.ReadAsStringAsync().Result);
                        var linkHeaders = ParseLinkHeader(response.Result.Headers.GetValues("Link"));
                        morePages = linkHeaders.TryGetValue("next", out uri);
                    }
                }
            }

            return gets;
        }

        /// <summary>
        /// Asynchronously gets a tree from GitLab
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="branch">Branch</param>
        /// <param name="path">Path to get the tree of</param>
        /// <param name="privateKey">Private key</param>
        /// <param name="rec">Recursively get files or only those directly in the specified path</param>
        /// <returns>Retrieved GitLab tree</returns>
        public static async Task<List<string>> GetGitLabTreeAsync(string projectId,
            string branch, string path, string privateKey, bool rec = false) {
            var uriBase = BuildUriPaths(GitLabEndpoint, "projects", projectId, "repository", "tree");
            var uri = BuildUriArgs(uriBase,
                new KeyValuePair<string, string>("ref", branch),
                new KeyValuePair<string, string>("path", Uri.EscapeDataString(path)),
                new KeyValuePair<string, string>("pagination", "keyset"),
                new KeyValuePair<string, string>("per_page", "100"),
                new KeyValuePair<string, string>("recursive", rec.ToString().ToLower()));

            var gets = new List<string>();
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri)) {
                request.Headers.Add("PRIVATE-TOKEN", privateKey);
                
                using (var response = client.SendAsync(request))
                {
                    var msg = await response;
                    var content = await msg.Content.ReadAsStringAsync();
                    gets.Add(content);
                    var linkHeaders = ParseLinkHeader(response.Result.Headers.GetValues("Link"));
                    linkHeaders.TryGetValue("next", out uri);
                }
            }
            
            return gets;
        }

        private static IDictionary<string, string> ParseLinkHeader(IEnumerable<string> header) {
            var dict = new Dictionary<string, string>();
            if (header == null) return dict;
            foreach (var head in header) {
                var links = head.Split(',');
                foreach (var link in links) {
                    var linkRel = link.Trim().Split(';');
                    var key = Regex.Match(linkRel[1], "rel=\"(.*)\"").Groups[1].Value;
                    var value = Regex.Match(linkRel[0], "<(.*)>").Groups[1].Value;
                    dict.Add(key, value);
                }
            }
            return dict;
        }

        /// <summary>
        /// Gets a file from GitLab
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="branch">Branch</param>
        /// <param name="path">Path of the file to get</param>
        /// <param name="privateKey">Private key</param>
        /// <returns>Retrieved GitLab file</returns>
        public static string GetGitLabFile(string projectId, string branch, string path, string privateKey) {
            var uriBase = BuildUriPaths(GitLabEndpoint, "projects", projectId, "repository", "files", Uri.EscapeDataString(path), "raw");
            var uri = BuildUriArgs(uriBase,
                new KeyValuePair<string, string>("ref", branch));

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Add("PRIVATE-TOKEN", privateKey);
                
                using (var response = client.SendAsync(request))
                {
                    return response.Result.Content.ReadAsStringAsync().Result;
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a file from GitLab
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="branch">Branch</param>
        /// <param name="path">Path of the file to get</param>
        /// <param name="privateKey">Private key</param>
        /// <returns>Retrieved GitLab file</returns>
        public static async Task<string> GetGitLabFileAsync(string projectId, string branch, string path, string privateKey) {
            var uriBase = BuildUriPaths(GitLabEndpoint, "projects", projectId, "repository", "files", Uri.EscapeDataString(path), "raw");
            var uri = BuildUriArgs(uriBase,
                new KeyValuePair<string, string>("ref", branch));

            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Add("PRIVATE-TOKEN", privateKey);
                
                using (var response = client.SendAsync(request))
                {
                    var msg = await response;
                    return await msg.Content.ReadAsStringAsync();
                }
            }
        }

        /// <summary>
        /// Initiates the class for commit actions
        /// </summary>
        public static void InitGitLabCommitActions() {
            actions = new JArray();
        }

        /// <summary>
        /// Resets the class for commit actions
        /// </summary>
        public static void ResetGitLabCommitActions() {
            actions = null;
        }

        /// <summary>
        /// Add a GitLab create commit action
        /// </summary>
        /// <param name="filePath">Path of the file to create</param>
        /// <param name="content">Content of the file</param>
        public static void AddGitLabCommitCreate(string filePath, object content) {
            var action = new JObject
            {
                new JProperty("action", "create"),
                new JProperty("file_path", filePath),
                new JProperty("content", content)
            };
            actions.Add(action);
        }
        
        /// <summary>
        /// Add a GitLab update commit action
        /// </summary>
        /// <param name="filePath">Path of the file to update</param>
        /// <param name="content">Content of the file</param>
        public static void AddGitLabCommitUpdate(string filePath, object content) {
            var action = new JObject
            {
                new JProperty("action", "update"),
                new JProperty("file_path", filePath),
                new JProperty("content", content)
            };
            actions.Add(action);
        }

        /// <summary>
        /// Post the added GitLab commit actions
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="branch">Branch</param>
        /// <param name="privateKey">Private key</param>
        /// <param name="commitMsg">Commit message</param>
        public static void PostGitLabCommit(string projectId, string branch, string privateKey, string commitMsg) {
            var payload = new JObject
            {
                new JProperty("branch", branch),
                new JProperty("commit_message", commitMsg),
                new JProperty("actions", actions)
            };

            var uri = BuildUriPaths(GitLabEndpoint, "projects", projectId, "repository", "commits");

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Add("PRIVATE-TOKEN", privateKey);

                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                client.SendAsync(request).Wait();
            }
        }

        /// <summary>
        /// Asynchronously post the added GitLab commit actions
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="branch">Branch</param>
        /// <param name="privateKey">Private key</param>
        /// <param name="commitMsg">Commit message</param>
        public static async Task PostGitLabCommitAsync(string projectId, string branch, string privateKey, string commitMsg) {
            var payload = new JObject
            {
                new JProperty("branch", branch),
                new JProperty("commit_message", commitMsg),
                new JProperty("actions", actions)
            };

            var uri = BuildUriPaths(GitLabEndpoint, "projects", projectId, "repository", "commits");

            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                request.Headers.Add("PRIVATE-TOKEN", privateKey);

                request.Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                await client.SendAsync(request);
            }
        }
    }
}