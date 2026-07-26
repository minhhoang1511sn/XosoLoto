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

            // Đảm bảo cửa sổ luôn vừa với màn hình, kể cả màn hình nhỏ
            this.Loaded += (s, e) =>
            {
                var workArea = SystemParameters.WorkArea;
                this.MaxWidth = workArea.Width;
                this.MaxHeight = workArea.Height;
                if (this.Width > workArea.Width) this.Width = workArea.Width;
                if (this.Height > workArea.Height) this.Height = workArea.Height;
            };
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