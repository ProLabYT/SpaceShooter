using System;
using System.Windows;
using System.Windows.Controls;

namespace SpaceShooter
{
    /// <summary>
    /// Interaction logic for PauseMenu.xaml
    /// </summary>
    public partial class PauseMenu : UserControl
    {
        public EventHandler ResumeClicked;
        public EventHandler RestartClicked;
        public EventHandler ExitClicked;
        public PauseMenu()
        {
            InitializeComponent();
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            ResumeClicked?.Invoke(this, EventArgs.Empty);
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            ExitClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
