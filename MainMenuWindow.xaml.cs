using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace SpaceShooter
{
    /// <summary>
    /// Obsahuje Event Handling metódy pre tlačítka definované v .xaml súbore, reprezentuje hlavné menu.
    /// Poskytuje prepínanie medzi jednotlivými "obrazovkami" tým, že content nimi vyplní.
    /// </summary>
    public partial class MainMenuWindow : UserControl
    {
        private const string configFilePath = "../../../config.txt";
        private GameConfiguration gameConfiguration;
        private Settings settingsControl;
        public MainMenuWindow()
        {
            InitializeComponent();
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            text_block_version.Text = string.Format("  Version {0}.{1}.{2}", version.Major, version.Minor, version.Build);

            gameConfiguration = MainWindow.LoadKeybindsFromFile(configFilePath);
            //vytvorenie settings UserControlu, medzi ktorým budem len prepínať 
            settingsControl = new Settings(gameConfiguration); 
        }

        //nasledujúce funkcie sú nabindované na tlačítka definované v MainMenuWindow.xaml súbore
        private void StartGame(object sender, RoutedEventArgs e) 
        {
            //tlačidlo na začatie hry
            var mainWindow = new MainWindow();
            this.Content = mainWindow;
            mainWindow.StartGame();
        }

        private void ShowSettings(object sender, RoutedEventArgs e) 
        {
            //tlačidlo na otvorenie nastavení
            this.Content = settingsControl;
        }

        private void QuitGame(object sender, RoutedEventArgs e)
        {
            //ukončenie aplikácie
            Application.Current.Shutdown();
        }
    }
}
