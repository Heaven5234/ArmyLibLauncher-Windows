using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArmyLib.Source
{
    public class Cache
    {
        private static readonly string armyLibPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArmyLib");

        public static readonly string cachePath = Path.Combine(armyLibPath, "cached_games.json");

        public static bool HasCache()
        {
            return File.Exists(cachePath);
        }

        public static async Task SaveCache(ObservableCollection<GameItem> games)
        {
            try
            {
                if (!Directory.Exists(armyLibPath))
                {
                    Directory.CreateDirectory(armyLibPath);
                    Debug.WriteLine("folder created");
                }

                var options = new JsonSerializerOptions { WriteIndented = true };

                string cache_json = JsonSerializer.Serialize(games, options);

                File.WriteAllText(cachePath, cache_json);
            }

            catch(Exception ex)
            {
                Debug.WriteLine($"CacheHandler : failed to saving cache. {ex.Message}");
            }
        }

        public static List<GameItem> LoadCache()
        {
            if (!HasCache()) return new List<GameItem>();

            try
            {
                string json = File.ReadAllText(cachePath);
                return JsonSerializer.Deserialize<List<GameItem>>(json) ?? new List<GameItem>();
            }

            catch
            {
                return new List<GameItem>();
            }
        }
    }
}
