using System.Windows;

namespace SpaceShooter
{
    /// <summary>
    /// Len window, ktorý sa vytvorí pri spustení programu a jeho obsah je vyplnený hlavným menu (MainMenuWindow)
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            Content = new MainMenuWindow();
        }
    }
}
