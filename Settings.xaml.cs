using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpaceShooter
{
    public partial class Settings : UserControl
    {
        public GameConfiguration gameConfiguration; //konfigurácia keybindov, ktoré sa dajú meniť
        private ControlToRebind currentControlToRebind; //keybind, ktorý sa aktuálne mení

        private enum ControlToRebind    //enum všetkých možných keybindov, ktoré sa dajú meniť
        {
            None,
            MoveUpKey,
            MoveDownKey,
            MoveLeftKey,
            MoveRightKey,
            FireKey,
            MouseControl
        }

        public Settings(GameConfiguration gameConfig)
        {
            InitializeComponent();
            gameConfiguration = gameConfig;
            KeyDown += UserControl_KeyDown; //priradenie key event handleru na rebindovanie keybindov
        }

        private void Rebind(ControlToRebind control)        //nastaví currentControlToRebind, podľa toho, ktoré tlačítko bolo stlačené a zobrazí v prompte
        {
            currentControlToRebind = control;
            prompt.Content = $"Press a key to rebind the {control} command";
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e) //samotný event handler na rebindovanie
        {
            if (currentControlToRebind != ControlToRebind.None)
            {
                switch (currentControlToRebind)         //aktualizácia keybindu v gameConfiguration
                {
                    case ControlToRebind.MoveUpKey:
                        gameConfiguration.MoveUpKey = e.Key;
                        break;
                    case ControlToRebind.MoveDownKey:
                        gameConfiguration.MoveDownKey = e.Key;
                        break;
                    case ControlToRebind.MoveLeftKey:
                        gameConfiguration.MoveLeftKey = e.Key;
                        break;
                    case ControlToRebind.MoveRightKey:
                        gameConfiguration.MoveRightKey = e.Key;
                        break;
                    case ControlToRebind.FireKey:
                        gameConfiguration.FireKey = e.Key;
                        break;
                }
                currentControlToRebind = ControlToRebind.None; //resetovanie na ďalšie použitie
                prompt.Content = string.Empty;                 //vyčistenie promptu
                updateConfigFile();                            //prepísanie konfiguračného súboru so zmenami
            }
        }

        private void updateConfigFile() //funkcia na prepísanie konfiguračného súboru (aj so správnym formátovaním)
        {
            string configText =
            $"MoveUpKey={gameConfiguration.MoveUpKey}\n" +
            $"MoveDownKey={gameConfiguration.MoveDownKey}\n" +
            $"MoveLeftKey={gameConfiguration.MoveLeftKey}\n" +
            $"MoveRightKey={gameConfiguration.MoveRightKey}\n" +
            $"FireKey={gameConfiguration.FireKey}\n" +
            $"MouseControl={gameConfiguration.MouseControl}\n";

            File.WriteAllText("../../../config.txt", configText); //zapísanie do súboru
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e) //event handler pre to, keď hráč kurzorom vojde do tlačítka (použité v Settings.xaml)
        {
            if (gameConfiguration == null)
                return;

            Button button = sender as Button; //castuje sender object do Button-u
            if (button != null)
            {
                string keyName = button.Tag.ToString(); //každý button má nejaký Tag, podľa neho sa vyberie case, čo treba zobraziť v prompte on hover.
                switch (keyName)
                {
                    case "MoveUpKey":
                        prompt.Content = $"Current key binding for Up: {gameConfiguration.MoveUpKey}";
                        break;
                    case "MoveDownKey":
                        prompt.Content = $"Current key binding for Down: {gameConfiguration.MoveDownKey}";
                        break;
                    case "MoveLeftKey":
                        prompt.Content = $"Current key binding for Left: {gameConfiguration.MoveLeftKey}";
                        break;
                    case "MoveRightKey":
                        prompt.Content = $"Current key binding for Right: {gameConfiguration.MoveRightKey}";
                        break;
                    case "FireKey":
                        prompt.Content = $"Current key binding for Fire: {gameConfiguration.FireKey}";
                        break;
                    case "MouseToggleButton":
                        prompt.Content = $"Mouse Control: {gameConfiguration.MouseControl}";
                        break;
                    case "ResetButton":
                        prompt.Content = $"Reset Defaults";
                        break;
                }
            }
        }

        private void UpRebind(object sender, RoutedEventArgs e) //event handlery pre rebindovanie konkrétnych ovládacích prvkov
        {
            Rebind(ControlToRebind.MoveUpKey);
        }

        private void DownRebind(object sender, RoutedEventArgs e)
        {
            Rebind(ControlToRebind.MoveDownKey);
        }

        private void LeftRebind(object sender, RoutedEventArgs e)
        {
            Rebind(ControlToRebind.MoveLeftKey);
        }

        private void RightRebind(object sender, RoutedEventArgs e)
        {
            Rebind(ControlToRebind.MoveRightKey);
        }

        private void FireRebind(object sender, RoutedEventArgs e)
        {
            Rebind(ControlToRebind.FireKey);
        }

        private void MouseControlToggle(object sender, RoutedEventArgs e) //toggle pre mouse control 
        {
            gameConfiguration.MouseControl = !gameConfiguration.MouseControl;   //bool negácia 
            updateConfigFile();
            prompt.Content = $"Mouse Control: {gameConfiguration.MouseControl}";
        }

        private void ResetDefaults(object sender, RoutedEventArgs e)    //nahradenie všetkých bindov ich defaultnými hodnotami
        {
            gameConfiguration.MoveUpKey = MainWindow.DefaultMoveUpKey;
            gameConfiguration.MoveDownKey = MainWindow.DefaultMoveDownKey;
            gameConfiguration.MoveLeftKey = MainWindow.DefaultMoveLeftKey;
            gameConfiguration.MoveRightKey = MainWindow.DefaultMoveRightKey;
            gameConfiguration.FireKey = MainWindow.DefaultFireKey;
            gameConfiguration.MouseControl = MainWindow.DefaultMouseControl;

            updateConfigFile();

            prompt.Content = "Defaults Reset.";
        }

        private void SwitchToMenu(object sender, RoutedEventArgs e) //funkcia na vrátenie sa naspäť do menu
        {
            var mainMenuWindow = new MainMenuWindow();
            this.Content = mainMenuWindow;                      //swich contentu na mainMenuWindow
        }
    }
}
