using System;
using System.Collections.ObjectModel;
using System.Windows.Data;

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
    }
}
