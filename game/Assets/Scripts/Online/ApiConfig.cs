using System;
using System.IO;
using UnityEngine;

namespace BaGio12.Online
{
    [Serializable] public sealed class ApiConfig
    {
        public const string DefaultBaseUrl = "http://localhost:8080";
        public string baseUrl = DefaultBaseUrl;

        public static string BaseUrl
        {
            get
            {
                string fromEnvironment = System.Environment.GetEnvironmentVariable("THREEGIO12_API_BASE");
                if (!string.IsNullOrWhiteSpace(fromEnvironment)) return Trim(fromEnvironment);
                try
                {
                    string file = Path.Combine(Application.streamingAssetsPath, "api.json");
                    if (File.Exists(file))
                    {
                        var config = JsonUtility.FromJson<ApiConfig>(File.ReadAllText(file));
                        if (config != null && !string.IsNullOrWhiteSpace(config.baseUrl)) return Trim(config.baseUrl);
                    }
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { }
                return DefaultBaseUrl;
            }
        }

        static string Trim(string url) => url.Trim().TrimEnd('/');
    }
}
