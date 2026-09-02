using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace ArmyLib.Source
{
    public class EpicGames
    {
        private static HttpClient _httpClient = new HttpClient();

        private static (string name, string appName, string installDir)? GetEpicAppData(string manifest)
        {
            string jsonContent = File.ReadAllText(manifest);

            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
            {
                var root = doc.RootElement;

                string name = root.TryGetProperty("DisplayName", out var nameProp) ? nameProp.GetString() : null;
                string appName = root.TryGetProperty("AppName", out var appNameProp) ? appNameProp.GetString() : null;
                string installDir = root.TryGetProperty("InstallLocation", out var installDirProp) ? installDirProp.GetString() : null;

                return (name, appName, installDir);
            }
        }

        private static async Task GetEpicGamesLibrary(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://library-service.live.ol.epicgames.com/library/api/v1/assets/NEW?includeMetadata=true");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                var games = response.Content.ReadAsStringAsync();

                await Task.Delay(100);
            }

            catch(Exception ex)
            {
                Debug.WriteLine($"GetEpicGamesLibray : Error {ex.Message}");
            }
        }

        private static async Task GetInstalledEpicGames(string epicManifestDir)
        {
            if (String.IsNullOrEmpty(epicManifestDir) || !Directory.Exists(epicManifestDir)) return;

            var manifests = Directory.GetFiles(epicManifestDir, "*.item");
            foreach (var manifest in manifests)
            {
                try
                {
                    var appData = GetEpicAppData(manifest);

                    string name = appData.Value.name;
                    string appName = appData.Value.appName;
                    string installDir = appData.Value.installDir;

                    if (!string.IsNullOrEmpty(installDir))
                    {
                        try
                        {
                            var fullPath = Path.GetFullPath(installDir);

                            string epicBaseDir = Path.GetDirectoryName(epicManifestDir);

                            if (!fullPath.StartsWith(epicBaseDir, StringComparison.OrdinalIgnoreCase))
                            {
                                installDir = null;
                            }
                        }

                        catch(Exception ex)
                        {
                            Debug.WriteLine($"GetInstalledEpicGames : error {ex.Message}");
                            installDir = null;
                        }
                    }

                    if (!string.IsNullOrEmpty(appName))
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            GameItemHandler.Instance.AddGame(new GameItem
                            {
                                Name = name,
                                Platform = "Epic Games",
                                AppIdOrPath = appName,
                                InstallLocation = installDir,
                                IsInstalled = true
                            });
                        }
                    }
                }

                catch (Exception ex)
                {
                    Debug.WriteLine($"GetEpicGames : Error {ex.Message}");

                    continue;
                }
            }
        }

        public static async Task GetEpicGames(string epicDir, string accessToken)
        {
            await GetEpicGamesLibrary(accessToken);
            await GetInstalledEpicGames(epicDir);
        }
    }
}
