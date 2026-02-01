using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Xosoloto
{
    public partial class PrizeDisplayWindow : Window
    {
        private string backgroundPath;
        private string logoPath;
        private string[] prizePaths;
        private string eventTitle;
        private string imagePath;
        private int prizeIndex;
        private Window parentWindow;


        public PrizeDisplayWindow(string title, string logoPath, string prizeName, int prizeIndex, string[] prizePaths, string backgroundPath = null, string imagePath = null)
        {
            InitializeComponent();
            this.eventTitle = title;
            this.logoPath = logoPath;
            this.prizeIndex = prizeIndex;
            this.prizePaths = prizePaths ?? new string[5];
            this.backgroundPath = backgroundPath;
            this.Loaded += (s, e) => LoadInitialData();
            this.KeyDown += (s, e) => {
                if (e.Key == Key.Left) MovePrize(-1);
                else if (e.Key == Key.Right) MovePrize(1);
                else if (e.Key == Key.Escape) this.Close();
            };
        }

        private void LoadInitialData()
        {
            try
            {
                imgBackground.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/DetailsPrize.png"));

                // Nạp Title
                txtTitle.Text = !string.IsNullOrEmpty(eventTitle) ? eventTitle : "ĐẠI HỘI TẾT";

                // Nạp Logo
                if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                    imgLogo.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));

                DisplayPrize(prizeIndex);
            }
            catch (Exception ex) { MessageBox.Show("Lỗi khởi tạo: " + ex.Message); }
        }

        private void DisplayPrize(int index)
        {
            if (index < 0 || index >= prizePaths.Length) return;
            prizeIndex = index;

            txtPrizeName.Text = $"LỘC XUÂN {index + 1}";
            txtPrizeNumber.Text = "? ? ? ?";

            try
            {
                if (!string.IsNullOrEmpty(prizePaths[index]) && File.Exists(prizePaths[index]))
                    imgPrize.Source = new BitmapImage(new Uri(prizePaths[index], UriKind.Absolute));
                else
                    imgPrize.Source = null;
            }
            catch { imgPrize.Source = null; }
        }

        private void MovePrize(int direction)
        {
            int nextIndex = prizeIndex + direction;
            if (nextIndex >= 0 && nextIndex < prizePaths.Length)
                DisplayPrize(nextIndex);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            // Truyền dữ liệu sang LuckyDrawWindow
            LuckyDrawWindow luc = new LuckyDrawWindow(imagePath, txtTitle.Text, logoPath, prizePaths);

            // Ẩn cửa sổ hiện tại
            this.Hide();

            // Hiển thị LuckyDrawWindow
            luc.ShowDialog();

            // Đóng cửa sổ InitLocXuan sau khi LuckyDrawWindow đóng
            this.Close();
        }
    }
}