using ArmyLib.Design;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace ArmyLib.Source
{
    public class GameHandler
    {
        public static async Task GetEpicGames(string epicDir, string token)
        {
            await EpicGames.GetEpicGames(epicDir, token);
        }

        public static async Task GetSteamGames(string steamDir)
        {
            await Steam.GetSteamGames(steamDir);
        }

        private static async Task SaveGames()
        {
            await Cache.SaveCache(GameItemHandler.Instance.Games, GarbageAppHandler.Instance.GarbageApps);
            MessageBox.Show($"game count : {GameItemHandler.Instance.Games.Count}");
        }

        private static async Task LoadGamesAtStartupAsync()
        {
            string steamDir = DirectionHandler.GetSteamDir();
            string epicDir = DirectionHandler.GetEpicDir();

            EpicWindow epic = new EpicWindow();
            epic.ShowDialog();

            await GetEpicGames(epicDir, EpicWindow.AccessToken);
            await GetSteamGames(steamDir);

            await SaveGames();
        }

        public static async void LoadGamesAtStartup()
        {
            await LoadGamesAtStartupAsync();
        }
    }
}
