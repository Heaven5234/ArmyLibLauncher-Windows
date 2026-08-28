using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ArmyLib.Source
{
    public class Steam
    {
        public static readonly HttpClient _httpClient = new HttpClient();

        public static string GetActiveSteamUser(string steamDir)
        {
            object activeUserObject = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess", "ActiveUser", 0);

            if (activeUserObject != null && Convert.ToInt32(activeUserObject) != 0)
            {
                string activeUserId = activeUserObject.ToString();
                string localConfigPath = Path.Combine(steamDir, "userdata", activeUserId, @"config\localconfig.vdf");

                if (File.Exists(localConfigPath))
                {
                    return localConfigPath;
                }
            }

            return null;
        }

        private static List<string> GetSteamAppIdsFromLocalConfig(string filePath)
        {
            List<string> appIds = new List<string>();
            if (!File.Exists(filePath)) return appIds;

            try
            {
                string content = File.ReadAllText(filePath);

                int appsIdx = content.IndexOf("\"apps\"", StringComparison.OrdinalIgnoreCase);
                if (appsIdx != -1)
                {
                    int openBrace = content.IndexOf('{', appsIdx);
                    if (openBrace != -1)
                    {
                        string appsBlock = content.Substring(openBrace);
                        MatchCollection matches = Regex.Matches(appsBlock, @"^\s*""(\d+)""\s*\{", RegexOptions.Multiline);

                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                appIds.Add(match.Groups[1].Value);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return appIds;
        }

        public static string GetVValue(VObject parent, string key)
        {
            if (parent == null) return null;

            foreach (var prop in parent)
            {
                if (string.Equals(prop.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value?.ToString();
                }
            }
            return null;
        }

        private static async Task<string> GetGameNameFromSteamStoreAPI(string appId)
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            }

            if (string.IsNullOrWhiteSpace(appId) || !long.TryParse(appId, out var _))
            {
                return null;
            }

            try
            {
                string url = $"https://store.steampowered.com/api/appdetails?appids={appId}";

                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty(appId, out var appData))
                    {
                        if (appData.TryGetProperty("success", out var success) && success.GetBoolean())
                        {
                            if (appData.TryGetProperty("data", out var dataNode))
                            {
                                if (dataNode.TryGetProperty("name", out var nameNode))
                                {
                                    return nameNode.GetString();
                                }
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show($"GetGameNameFromSteamStoreAPI : error {ex.Message}");
            }

            return null;
        }

        public static async Task GetSteamGames(string steamDir)
        {
            if (String.IsNullOrEmpty(steamDir) || !Directory.Exists(steamDir)) return;

            HashSet<string> installedAppIds = new HashSet<string>();
            string steamApps = Path.Combine(steamDir, "steamapps");

            if (Directory.Exists(steamApps))
            {
                var acfFiles = Directory.GetFiles(steamApps, "appmanifest_*.acf");

                foreach (var file in acfFiles)
                {
                    try
                    {
                        string fileContent = File.ReadAllText(file);

                        VProperty rootProp = VdfConvert.Deserialize(fileContent);

                        if (rootProp?.Value is VObject appState)
                        {

                            string gameName = GetVValue(appState, "name");
                            string appId = GetVValue(appState, "appid");
                            string installDir = GetVValue(appState, "installdir");

                            if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(gameName))
                            {
                                installedAppIds.Add(appId);

                                string fullInstallPath = Path.Combine(steamApps, "common", installDir ?? "");

                                GameItemHandler.Instance.AddGame(new GameItem
                                {
                                    Name = $"{gameName} : not installed",
                                    Platform = "Steam",
                                    AppIdOrPath = appId,
                                    InstallLocation = fullInstallPath,
                                    IsInstalled = true
                                });
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        MessageBox.Show($"GetSteamGames : Error on installed games {ex.Message}");
                    }
                }

                string activeUserConfigPath = GetActiveSteamUser(steamDir);

                if (!string.IsNullOrEmpty(activeUserConfigPath) && File.Exists(activeUserConfigPath))
                {
                    try
                    {
                        List<string> libraryAppIds = GetSteamAppIdsFromLocalConfig(activeUserConfigPath);

                        foreach (string appId in libraryAppIds)
                        {
                            if (!installedAppIds.Contains(appId))
                            {
                                string realGameNames = await GetGameNameFromSteamStoreAPI(appId);

                                if (realGameNames != null)
                                {
                                    Debug.WriteLine($"[Steam API] {appId} için isim çekiliyor");

                                    GameItemHandler.Instance.AddGame(new GameItem
                                    {
                                        Name = $"{realGameNames} : not installed",
                                        Platform = "Steam",
                                        AppIdOrPath = appId,
                                        InstallLocation = null,
                                        IsInstalled = false
                                    });
                                }

                                installedAppIds.Add(appId);

                                await Task.Delay(100);
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        MessageBox.Show($"GetSteamGames : Error on not installed games {ex.Message}");
                    }
                }
            }
        }
    }
}
