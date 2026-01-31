using System.Windows;

namespace Xosoloto
{
    public enum GameType
    {
        LotoVuiXuan,
        LocXuanDauNam 
    }

    public partial class GameTypeSelectionWindow : Window
    {
        public GameType SelectedGameType { get; private set; }

        public GameTypeSelectionWindow()
        {
            InitializeComponent();
        }

        private void LotoVuiXuanButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedGameType = GameType.LotoVuiXuan;
            DialogResult = true;
            Close();
        }
         private void LocXuanDauNamButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedGameType = GameType.LocXuanDauNam;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}