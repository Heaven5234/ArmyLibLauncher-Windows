using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Text.Json;
using System.Web;
using Microsoft.Win32;

namespace ArmyLib.Source
{
    internal class EpicLoginHandler
    {
        static HttpClient _httpClient = new HttpClient();
        static string fallBackVersion = "14.0.8";
        static string clientID = "34a02cf8f4414e29b15921876da36f9a";
        static string clientSecret = "daafbccc737745039dffe53d94fc76cf";
        static string epicLauncherDirRegedit = @"HKEY_CURRENT_USER\SOFTWARE\Epic Games\EOS";

        private static string GetEpicLauncherVersion()
        {
            string epicLauncherExeDir = Registry.GetValue(epicLauncherDirRegedit, "ModSdkCommand", null).ToString();

            try
            {
                if (File.Exists(epicLauncherExeDir))
                {
                    FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(epicLauncherExeDir);

                    return fileVersion.ProductVersion ?? fallBackVersion;
                }

                else
                {
                    Debug.WriteLine("GetEpicLauncherVersion : cant find launcher");
                }
            }

            catch(Exception ex)
            {
                Debug.WriteLine($"GetEpicLauncherVersion :  error {ex.Message}");
            }

            return fallBackVersion;
        }

        private static void InitializeHttpClient()
        {
            string version = GetEpicLauncherVersion();
            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            string useragent = $"EpicGamesLauncher/{version} Windows/10.0.19041.1.256.64bit";
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(useragent);

            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientID} : {clientSecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
        
        private static async Task<string> ExchangeCodeForTokenAsync(string authCode)
        {
            InitializeHttpClient();
            string requestUrl = "account-public-service-prod03.ol.epicgames.com/launcher/api/public/assets/v2/";

            var requestBody = new Dictionary<string, string>
            {
                {"grant_type", "authorization_code" },
                {"code", authCode.Trim() }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            string jsonResponse = await response.Content.ReadAsStringAsync();
            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                return doc.RootElement.TryGetProperty("access_token", out var keyProp) ? keyProp.GetString() : null;
            }            
        }

        public static async Task ExchangeCodeForToken(string authCode)
        {
            await ExchangeCodeForTokenAsync(authCode);
        }
    }
}
