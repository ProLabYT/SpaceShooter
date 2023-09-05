using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SpaceShooter
{
    public partial class Settings : UserControl
    {
        public GameConfiguration gameConfiguration;
        private ControlToRebind currentControlToRebind;

        private const Key DefaultMoveUpKey = Key.Up;
        private const Key DefaultMoveDownKey = Key.Down;
        private const Key DefaultMoveLeftKey = Key.Left;
        private const Key DefaultMoveRightKey = Key.Right;
        private const Key DefaultFireKey = Key.Space;
        private const bool DefaultMouseControl = false;

        private enum ControlToRebind
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
            KeyDown += UserControl_KeyDown;
        }

        private void Rebind(ControlToRebind control)
        {
            currentControlToRebind = control;
                prompt.Content = $"Press a key to rebind the {control} command";
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (currentControlToRebind != ControlToRebind.None)
            {
                switch (currentControlToRebind)
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
                currentControlToRebind = ControlToRebind.None;
                prompt.Content = string.Empty;
                updateConfigFile();
            }
        }

        private void updateConfigFile() 
        {
            string configText =
            $"MoveUpKey={gameConfiguration.MoveUpKey}\n" +
            $"MoveDownKey={gameConfiguration.MoveDownKey}\n" +
            $"MoveLeftKey={gameConfiguration.MoveLeftKey}\n" +
            $"MoveRightKey={gameConfiguration.MoveRightKey}\n" +
            $"FireKey={gameConfiguration.FireKey}\n" +
            $"MouseControl={gameConfiguration.MouseControl}\n";

            File.WriteAllText("../../../config.txt", configText);
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (gameConfiguration == null)
                return;

            Button button = sender as Button;
            if (button != null)
            {
                string keyName = button.Tag.ToString();
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
                        prompt.Content= $"Reset Defaults";
                        break;
                }
            }
        }

        private void UpRebind(object sender, RoutedEventArgs e)
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

        private void MouseControlToggle(object sender, RoutedEventArgs e)
        {
            gameConfiguration.MouseControl = !gameConfiguration.MouseControl;
            updateConfigFile();
            prompt.Content = $"Mouse Control: {gameConfiguration.MouseControl}";
        }

        private void ResetDefaults(object sender, RoutedEventArgs e)
        {
            gameConfiguration.MoveUpKey = DefaultMoveUpKey;
            gameConfiguration.MoveDownKey = DefaultMoveDownKey;
            gameConfiguration.MoveLeftKey = DefaultMoveLeftKey;
            gameConfiguration.MoveRightKey = DefaultMoveRightKey;
            gameConfiguration.FireKey = DefaultFireKey;
            gameConfiguration.MouseControl = DefaultMouseControl;

            updateConfigFile();

            prompt.Content = "Defaults Reset.";
        }

        private void SwitchToMenu(object sender, RoutedEventArgs e)
        {
            var mainMenuWindow = new MainMenuWindow();
            this.Content = mainMenuWindow;
        }
    }
}
