using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpaceShooter
{
    public partial class MainMenuWindow : UserControl
    {
        private GameConfiguration gameConfiguration;
        public event EventHandler StartGameClicked;
        public event EventHandler SettingsClicked;
        public event EventHandler StatsClicked;
        public event EventHandler LevelSelectorClicked;

        public MainMenuWindow()
        {
            InitializeComponent();
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            text_block_version.Text = string.Format("  Version {0}.{1}.{2}", version.Major, version.Minor, version.Build);

            gameConfiguration = new GameConfiguration
            {
                FireKey = Key.Space,
                MoveLeftKey = Key.Left,
                MoveRightKey = Key.Right,
                MoveUpKey = Key.Up,
                MoveDownKey = Key.Down
            };


        }

        private void StartGame(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            this.Content = mainWindow;
            mainWindow.StartGame();
        }

        private void ShowLevelSelector(object sender, RoutedEventArgs e)
        {
            LevelSelectorClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ShowSettings(object sender, RoutedEventArgs e)
        {
            var settings = new Settings(gameConfiguration);
            this.Content = settings;
        }

        private void ShowStats(object sender, RoutedEventArgs e)
        {
            StatsClicked?.Invoke(this, EventArgs.Empty);
        }

        
    }
}
