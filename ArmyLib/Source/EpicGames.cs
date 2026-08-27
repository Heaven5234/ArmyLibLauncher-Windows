using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ArmyLib.Source
{
    public class EpicGames
    {
        public static void GetEpicGames(string epicManifestDir)
        {
            if (String.IsNullOrEmpty(epicManifestDir) || !Directory.Exists(epicManifestDir)) return;

            var manifestes = Directory.GetFiles(epicManifestDir, "*.item");
            foreach (var manifest in manifestes)
            {
                try
                {
                    string jsonContent = File.ReadAllText(manifest);

                    using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                    {
                        var root = doc.RootElement;

                        string name = root.TryGetProperty("DisplayName", out var nameProp) ? nameProp.GetString() : null;
                        string appName = root.TryGetProperty("AppName", out var appNameProp) ? appNameProp.GetString() : null;
                        string installDir = root.TryGetProperty("InstallLocation", out var dirProp) ? dirProp.GetString() : null;

                        if (!string.IsNullOrEmpty(installDir))
                        {
                            try
                            {
                                var fullPath = Path.GetFullPath(installDir);

                                string epicBaseDir = Path.GetDirectoryName(epicManifestDir);

                                if(!fullPath.StartsWith(epicBaseDir, StringComparison.OrdinalIgnoreCase))
                                {
                                    installDir = null;
                                }
                            }

                            catch
                            {
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
                                    InstallLocation = installDir
                                });
                            }
                        }
                    }
                }

                catch(Exception ex) 
                {
                    Debug.WriteLine($"GetEpicGames : Error {ex.Message}");

                    continue;
                }
            }
        }
    }
}
