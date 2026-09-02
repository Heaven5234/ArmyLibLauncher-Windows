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

        public static readonly string garbageAppPath = Path.Combine(armyLibPath, "garbage_app_cache.json");

        public static bool HasCache()
        {
            return File.Exists(cachePath);
        }

        public static bool HasGarbageCache()
        {
            return File.Exists(garbageAppPath);
        }

        public static async Task SaveCache(ObservableCollection<GameItem> games, ObservableCollection<GarbageApp> garbageApps)
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
                string garbage_app_cache_json = JsonSerializer.Serialize(garbageApps, options);

                File.WriteAllText(cachePath, cache_json);
                File.WriteAllText(garbageAppPath, garbage_app_cache_json);
            }

            catch(Exception ex)
            {
                Debug.WriteLine($"CacheHandler : failed to saving cache. {ex.Message}");
            }
        }

        public static (List<GameItem> games, List<GarbageApp> garbageApps)? LoadCache()
        {
            if (!HasCache()) return (new List<GameItem>(), new List<GarbageApp>());

            try
            {
                string json = File.ReadAllText(cachePath);
                string garbage_json = File.ReadAllText(garbageAppPath);
                return ((JsonSerializer.Deserialize<List<GameItem>>(json) ?? new List<GameItem>()), (JsonSerializer.Deserialize<List<GarbageApp>>(garbage_json) ?? new List<GarbageApp>()));
            }

            catch
            {
                return (new List<GameItem>(), new List<GarbageApp>());
            }
        }
    }
}
