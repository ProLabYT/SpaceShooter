using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SpaceShooter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //public static GameConfiguration GameConfiguration { get; set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainMenu = new MainMenuWindow();

            mainMenu.StartGameClicked += (sender, args) =>
            {
                var mainWindow = new MainWindow();
                mainWindow.StartGame();
            };
            
        }
       /* public App()
        {
            App.GameConfiguration = LoadGameConfiguration();
        }

        private GameConfiguration LoadGameConfiguration()
        {

        }*/

    }
}
