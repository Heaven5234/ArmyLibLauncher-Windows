using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArmyLib.Source
{
    public class EpicGames
    {
        private static HttpClient _httpClient = new HttpClient();

        static string libraryUrl = "https://library-service.live.use1a.on.epicgames.com/library/api/public/items?includeMetadata=true";
        static string gamesUrl = "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/public/assets/v2/platform/{platform}/namespace/{namespace}/catalogItem/{catalog_item_id}/app/{app_name}/label/{label}";

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
                var request = new HttpRequestMessage(HttpMethod.Get, libraryUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                string jsonContent = await response.Content.ReadAsStringAsync();

                using(JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    var root = doc.RootElement;

                    if(root.TryGetProperty("records", out var records))
                    {
                        foreach(var app in records.EnumerateArray())
                        {
                            string productId = app.TryGetProperty("appName", out var productProp) ? productProp.GetString() : null;

                            if (GarbageAppHandler.Instance.GetGarbageAppByProductType(productId) == null)
                            {
                                string appName = app.TryGetProperty("productId", out var appProp) ? appProp.GetString() : null;
                                string name = app.TryGetProperty("sandboxName", out var nameProp) ? nameProp.GetString() : null;

                                Debug.WriteLine($"[EpicAPI] get data for :{appName}");

                                GameItemHandler.Instance.AddGame(new GameItem
                                {
                                    Name = name,
                                    AppIdOrPath = appName,
                                    Platform = "Epic Games",
                                    Type = "game",
                                    InstallLocation = null,
                                    IsInstalled = false
                                });
                            }

                            else
                            {
                                continue;
                            }

                            await Task.Delay(100);
                        }
                    }
                }
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
                            GameItem app = GameItemHandler.Instance.GetGameByAppID(appName);

                            if(app != null)
                            {
                                app.InstallLocation = installDir;
                                app.IsInstalled = true;
                            }
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
