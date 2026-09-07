using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace ArmyLib.Source
{
    public class DirectionHandler
    {
        public static string GetSteamDir()
        {
            try
            {
                var steamPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;

                if (string.IsNullOrEmpty(steamPath) || !Directory.Exists(steamPath)) throw new InvalidOperationException("SteamDir cannot be found");

                return steamPath;
            }

            catch(Exception ex) 
            {
                Debug.WriteLine($"SteamDir read error : {ex.Message}");
                return null;
            }
        }

        public static string GetEpicDir()
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                if (string.IsNullOrEmpty(programData)) throw new InvalidOperationException("EpicDir cannot be found");

                return Path.Combine(programData, @"Epic\EpicGamesLauncher\Data\Manifests");
            }
            
            catch(Exception ex)
            {
                Debug.WriteLine($"EpicDir read error : {ex.Message}");
                throw new InvalidOperationException("Failed to find EpicDir");
            }
        }
    }
}
