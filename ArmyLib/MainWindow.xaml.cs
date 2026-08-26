using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace ArmyLib
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<GameItem> Games { get; set; } = new ObservableCollection<GameItem>();

        public string GetSteamDir()
        {
            return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\steam", "SteamPath", null) as string;
        }

        public string GetEpicDir()
        {
            string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(programData, @"Epic\EpicGamesLauncher\Data\Manifests");
        }

        public string GetActiveSteamUser(string steamDir)
        {
            object activeUserObject = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess", "ActiveUser", 0);

            if(activeUserObject != null && Convert.ToInt32(activeUserObject) != 0)
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

        private List<string> GetAppIdsFromLocalConfig(string filePath)
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

        public string GetVValue(VObject parent, string key)
        {
            if (parent == null) return null;

            foreach(var prop in parent)
            {
                if(string.Equals(prop.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return prop.Value?.ToString();
                }
            }
            return null;
        }

        public void GetSteamGames(string steamDir)
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

                                Games.Add(new GameItem
                                {
                                    Name = gameName,
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
                        List<string> libraryAppIds = GetAppIdsFromLocalConfig(activeUserConfigPath);

                        foreach (string appId in libraryAppIds)
                        {
                            if (!installedAppIds.Contains(appId))
                            {
                                Games.Add(new GameItem
                                {
                                    Name = $"Steam Game {appId}",
                                    Platform = "Steam",
                                    AppIdOrPath = appId,
                                    InstallLocation = null,
                                    IsInstalled = false
                                });

                                installedAppIds.Add(appId);
                            }
                        }
                    }

                    catch(Exception ex) 
                    {
                        MessageBox.Show($"GetSteamGames : Error on not installed games {ex.Message}");
                    }
                }
            }
        }

        public void GetEpicGames(string epicManifestDir)
        {
            if (String.IsNullOrEmpty(epicManifestDir) || !Directory.Exists(epicManifestDir)) return;

            var manifestes = Directory.GetFiles(epicManifestDir, "*.item");
            foreach(var manifest in manifestes)
            {
                try
                {
                    string jsonContent = File.ReadAllText(manifest);
                    
                    using(JsonDocument doc = JsonDocument.Parse(jsonContent))
                    {
                        var root = doc.RootElement;

                        string name = root.TryGetProperty("DisplayName", out var nameProp) ? nameProp.GetString() : null;
                        string appName = root.TryGetProperty("AppName", out var appNameProp) ? appNameProp.GetString() : null;
                        string installDir = root.TryGetProperty("InstallLocation", out var dirProp) ? dirProp.GetString() : null;

                        if (!string.IsNullOrEmpty(name))
                        {
                            Games.Add(new GameItem
                            {
                                Name = name,
                                Platform = "Epic Games",
                                AppIdOrPath = appName,
                                InstallLocation = installDir
                            });
                        }
                    }
                }

                catch
                {
                    MessageBox.Show("GetEpicGames : Error");
                }
            }
        }

        public void GetAllGames()
        {
            string steamDir = GetSteamDir();
            string epicManifestDir = GetEpicDir();

            GetSteamGames(steamDir);
            GetEpicGames(epicManifestDir);
        }

        public MainWindow()
        {
            InitializeComponent();

            GetAllGames();

            MessageBox.Show($"Bulunan oyunların sayısı {Games.Count}");

            foreach(var game in Games)
            {
                MessageBox.Show(game.Name);
            }
        }
    }    
}
