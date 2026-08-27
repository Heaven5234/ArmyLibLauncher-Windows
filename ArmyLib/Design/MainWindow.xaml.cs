using ArmyLib.Source;
using System.Windows;

namespace ArmyLib
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            GameHandler.LoadGamesAtStartup();
        }
    }
}
