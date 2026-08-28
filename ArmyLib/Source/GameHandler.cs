using System.Threading.Tasks;
using System.Windows;

namespace ArmyLib.Source
{
    public class GameHandler
    {
        private static async Task GetAllGamesAsync()
        {
            if (Cache.HasCache())
            {
                var cachedGames = Cache.LoadCache();

                foreach(var game in cachedGames)
                {
                    GameItemHandler.Instance.AddGame(game);
                }

                return;
            }

            string steamDir = DirectionHandler.GetSteamDir();
            string epicManifestDir = DirectionHandler.GetEpicDir();

            await Steam.GetSteamGames(steamDir);
            EpicGames.GetEpicGames(epicManifestDir);

            Cache.SaveCache(GameItemHandler.Instance.Games);
        }

        public static async void LoadGamesAtStartup()
        {
            await GetAllGamesAsync();
        }
    }
}
