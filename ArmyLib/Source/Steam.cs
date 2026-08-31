using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ArmyLib.Source
{
    public class Steam
    {
        public static readonly HttpClient _httpClient = new HttpClient();

        private static string steamRegeditPath = @"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess";

        private static string GetVValue(VObject parent, string key)
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

        private static string CleanVdfContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return content;

            content = Regex.Replace(content, @"\[\$.*?\]", "");

            content = Regex.Replace(content, @"^\s*//.*", "", RegexOptions.Multiline);

            return content;
        }

        private static async Task<HashSet<string>> GetAppIDsFromLocalConfig(string steamDir)
        {
            HashSet<string> appIDs = new HashSet<string>();

            object currentUserRegedit = Registry.GetValue(steamRegeditPath, "ActiveUser", null);
            if(currentUserRegedit == null || currentUserRegedit.ToString() == "0") return null;

            string currentUser = Convert.ToInt32(currentUserRegedit).ToString();
            string localConfig = Path.Combine(steamDir, "userdata", currentUser, "config", "localconfig.vdf");

            if (!File.Exists(localConfig)) { Debug.WriteLine("GetAppIDsFromLocalConfig : cant find localconfig.vdf");  return appIDs; }

            try
            {
                string configContent;

                using (var stream = new FileStream(
                    localConfig,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                    ))

                using (var reader = new StreamReader(stream))
                {
                    configContent = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(configContent))
                {
                    Debug.WriteLine("Cant find configContent");
                    return appIDs;
                }

                int appsIndex = configContent.IndexOf("\"apps\"", StringComparison.OrdinalIgnoreCase);

                if(appsIndex != -1)
                {
                    string appsSection = configContent.Substring(appsIndex);

                    var matches = Regex.Matches(appsSection, @"^\s*""(\d+)""", RegexOptions.Multiline);
                    foreach(Match match in matches)
                    {
                        if(match.Groups.Count > 0)
                        {
                            appIDs.Add(match.Groups[1].Value);
                        }
                    }
                }
            }

            catch(Exception ex)
            {
                Debug.WriteLine($"GetAppIDsFromLocalConfig : {ex.Message}");
            }

            return appIDs;
        }

        private static (string name, string type)? GetSteamAppData(string appDetail, string appID)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(appDetail))
                {
                    if (doc.RootElement.TryGetProperty(appID, out JsonElement appElement))
                    {
                        if (appElement.TryGetProperty("success", out JsonElement success))
                        {
                            if (success.GetBoolean())
                            {
                                if (appElement.TryGetProperty("data", out JsonElement data))
                                {
                                    string name = data.TryGetProperty("name", out JsonElement nameProp) ? nameProp.GetString() : null;
                                    string type = data.TryGetProperty("type", out JsonElement typeProp) ? typeProp.GetString() : null;

                                    return (name, type);
                                }
                            }
                        }
                    }

                }
            }

            catch(Exception ex)
            {
                Debug.WriteLine($"GetSteamAppData : {ex.Message}");
            }

            return null;
        }

        private static async Task<string> GetAppDetail(string appID)
        {
            try
            {
                string appDetailUrl = $"https://store.steampowered.com/api/appdetails?appids={appID}";

                var response = await _httpClient.GetAsync(appDetailUrl);
                if (!response.IsSuccessStatusCode) return null;

                string appDetail = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[SteamAPI] get data for :{appID}");

                return appDetail;
            }

            catch(Exception ex)
            {
                Debug.WriteLine($"GetAppDetail : {ex.Message}");
            }

            return null;
        }

        private static async Task GetSteamUserLibrary(HashSet<string> userAppIDs)
        {
            if (userAppIDs == null || userAppIDs.Count == 0) return;

            if (Cache.cachePath != null)
            {
                var cachedGames = Cache.LoadCache();
                                
                foreach (string appID in userAppIDs)
                {
                    var game = cachedGames.FirstOrDefault(g => string.Equals(g.AppIdOrPath, appID, StringComparison.OrdinalIgnoreCase) || (int.TryParse(g.AppIdOrPath, out int gameId) && int.TryParse(appID, out int searchId) && gameId == searchId));

                    if (game == null)
                    {
                        try
                        {
                            string appDetail = await GetAppDetail(appID);

                            var appInfo = GetSteamAppData(appDetail, appID);

                            if (appInfo.HasValue)
                            {
                                if (appInfo.Value.type == "game" || appInfo.Value.type == "dlc")
                                {
                                    GameItemHandler.Instance.AddGame(new GameItem
                                    {
                                        Name = appInfo.Value.name,
                                        Platform = "Steam",
                                        Type = appInfo.Value.type,
                                        AppIdOrPath = appID,
                                        InstallLocation = null,
                                        IsInstalled = false
                                    });
                                }
                            }

                            await Task.Delay(100);
                        }

                        catch (Exception ex)
                        {
                            Debug.WriteLine($"GetSteamUserLibrary[SteamAPI] : {ex.ToString()}");
                        }
                    }

                    else
                    {
                        continue;
                    }
                }
            }

            else
            {
                foreach (string appID in userAppIDs)
                {
                    try
                    {
                        string appDetail = await GetAppDetail(appID);

                        var appInfo = GetSteamAppData(appDetail, appID);

                        if (appInfo.HasValue)
                        {
                            if (appInfo.Value.type == "game" || appInfo.Value.type == "dlc")
                            {
                                GameItemHandler.Instance.AddGame(new GameItem
                                {
                                    Name = appInfo.Value.name,
                                    Platform = "Steam",
                                    Type = appInfo.Value.type,
                                    AppIdOrPath = appID,
                                    InstallLocation = null,
                                    IsInstalled = false
                                });
                            }
                        }

                        await Task.Delay(100);
                    }

                    catch (Exception ex)
                    {
                        Debug.WriteLine($"GetSteamUserLibraryCompeletely[SteamAPI] : {ex.ToString()}");
                    }
                }
            }
        }

        private static async Task GetInstalledSteamGames(string steamDir)
        {           
            HashSet<string> installedAppIds = new HashSet<string>();
            if (String.IsNullOrEmpty(steamDir) || !Directory.Exists(steamDir)) return;

            string steamApps = Path.Combine(steamDir, "steamapps");

            if (Directory.Exists(steamApps))
            {
                var acfFiles = Directory.GetFiles(steamApps, "appmanifest_*.acf");

                foreach (var file in acfFiles)
                {
                    try
                    {
                        string fileContent = File.ReadAllText(file);
                        if (string.IsNullOrWhiteSpace(fileContent)) continue;

                        fileContent = CleanVdfContent(fileContent);
                        VProperty rootProp = VdfConvert.Deserialize(fileContent);

                        if (rootProp?.Value is VObject appState)
                        {
                            string appID = GetVValue(appState, "appid");
                            string installDir = GetVValue(appState, "installdir");

                            GameItem app = GameItemHandler.Instance.GetGameByAppID(appID);

                            if(app != null)
                            {
                                app.InstallLocation = Path.Combine(steamDir, "common", installDir ?? "");
                                app.IsInstalled = true;
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        MessageBox.Show($"GetInstalledSteamGames : {ex.Message}");
                    }
                }
            }
        }

        public static async Task GetSteamGames(string steamDir)
        {
            HashSet<string> userAppIDs = await GetAppIDsFromLocalConfig(steamDir);

            await GetSteamUserLibrary(userAppIDs);

            await GetInstalledSteamGames(steamDir);
        }
    }
}
