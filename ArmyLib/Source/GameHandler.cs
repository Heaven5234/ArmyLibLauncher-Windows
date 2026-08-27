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
            EpicGames.GetEpicGames(epicManifestDir);
        }

        public static async void LoadGamesAtStartup()
        {
            await GetAllGamesAsync();

            MessageBox.Show($"Game count : {GameItemHandler.Instance.Games.Count}");
        }
    }
}
