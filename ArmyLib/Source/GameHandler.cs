using System.Threading.Tasks;
using System.Windows;

namespace ArmyLib.Source
{
    public class GameHandler
    {
        private static async Task GetAllGamesAsync()
        {
            string steamDir = DirectionHandler.GetSteamDir();
            string epicManifestDir = DirectionHandler.GetEpicDir();

            await Steam.GetSteamGames(steamDir);
            await EpicGames.GetEpicGames(epicManifestDir);

            await Cache.SaveCache(GameItemHandler.Instance.Games);
            MessageBox.Show($"game count : {GameItemHandler.Instance.Games.Count}");
        }

        public static async void LoadGamesAtStartup()
        {
            await GetAllGamesAsync();
        }
    }
}
