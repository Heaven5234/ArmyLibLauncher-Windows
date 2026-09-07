using System;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Linq;
using System.Diagnostics;

namespace ArmyLib.Source
{
    public class GameItemHandler
    {
        private static readonly Lazy<GameItemHandler> _instance = new Lazy<GameItemHandler>(() => new GameItemHandler());

        public static GameItemHandler Instance => _instance.Value;

        public ObservableCollection<GameItem> Games { get; }

        private readonly object _lock = new object();

        private GameItemHandler()
        {
            Games = new ObservableCollection<GameItem>();

            BindingOperations.EnableCollectionSynchronization(Games, _lock);
        }

        public void AddGame(GameItem game)
        {
            lock (_lock)
            {
                Games.Add(game);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                Games.Clear();
            }
        }

        public GameItem GetGameByAppID(string appID)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(appID))
                {
                    Debug.WriteLine("GetGameByAppId : Error");
                    return null;
                }

                else if (Games == null || Games.Count == 0)
                {
                    Debug.WriteLine("GetGameByAppId : No games found");
                    return null;
                }

                var result = Games.FirstOrDefault(x => x != null && !string.IsNullOrEmpty(x.AppIdOrPath) && string.Equals(x.AppIdOrPath, appID, StringComparison.OrdinalIgnoreCase));

                if(result != null)
                {
                    Debug.WriteLine($"GetGameByyAppId : Found game with AppID {appID}");
                }

                else
                {
                    Debug.WriteLine($"GetGameByAppId : No game found with AppID {appID}");
                }

                return result;
            }
        }
    }
}
