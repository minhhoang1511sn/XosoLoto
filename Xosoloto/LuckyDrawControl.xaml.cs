using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Xosoloto
{
    public partial class LuckyDrawWindow : Window
    {
        // Kích thước chuẩn
        private const double BASE_WIDTH = 1600;
        private const double BASE_HEIGHT = 900;

        public LuckyDrawWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }

        public LuckyDrawWindow(string imagePath, string title, string logoPath, string[] prizePaths)
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;

            this.ImagePath = imagePath;
            this.Title = title;
            this.LogoPath = logoPath;
            this.PrizePaths = prizePaths;

            LoadData();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Đợi render xong rồi mới update positions
            this.Dispatcher.InvokeAsync(() =>
            {
                UpdatePositions();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePositions();
        }

        private void UpdatePositions()
        {
            double actualWidth = MainCanvas.ActualWidth;
            double actualHeight = MainCanvas.ActualHeight;

            // Nếu chưa render xong thì dùng Window size
            if (actualWidth == 0) actualWidth = this.ActualWidth;
            if (actualHeight == 0) actualHeight = this.ActualHeight;

            double scaleX = actualWidth / BASE_WIDTH;
            double scaleY = actualHeight / BASE_HEIGHT;

            // Title - Giữa trên (căn giữa theo width)
            txtTitle.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(txtTitle, (actualWidth - txtTitle.DesiredSize.Width) / 2);
            Canvas.SetTop(txtTitle, 170 * scaleY);

            // Lộc Xuân 1 - Cột 1 hàng 1 (bên phải)
            txtLoc1.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double loc1X = actualWidth - 1000 * scaleX + 100 * scaleX; // Vị trí cột 1
            double loc1Y = 340 * scaleY + 30; // Hàng 1
            Canvas.SetLeft(txtLoc1, loc1X - txtLoc1.DesiredSize.Width / 2);
            Canvas.SetTop(txtLoc1, loc1Y);

            // Lộc Xuân 2 - Cột 2 hàng 1
            txtLoc2.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double loc2X = actualWidth - 1000 * scaleX + 600 * scaleX; // Vị trí cột 2
            double loc2Y = 340 * scaleY + 30;
            Canvas.SetLeft(txtLoc2, loc2X - txtLoc2.DesiredSize.Width / 2);
            Canvas.SetTop(txtLoc2, loc2Y);

            // Lộc Xuân 3 - Cột 1 hàng 2
            txtLoc3.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double loc3X = actualWidth - 1000 * scaleX + 100 * scaleX;
            double loc3Y = 340 * scaleY + 200; // Hàng 2
            Canvas.SetLeft(txtLoc3, loc3X - txtLoc3.DesiredSize.Width / 2);
            Canvas.SetTop(txtLoc3, loc3Y);

            // Lộc Xuân 4 - Cột 2 hàng 2
            txtLoc4.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double loc4X = actualWidth - 1000 * scaleX + 600 * scaleX;
            double loc4Y = 340 * scaleY + 200;
            Canvas.SetLeft(txtLoc4, loc4X - txtLoc4.DesiredSize.Width / 2);
            Canvas.SetTop(txtLoc4, loc4Y);

            // Buttons - Dưới cùng bên trái
            spButtons.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(spButtons, 100 * scaleX);
            Canvas.SetTop(spButtons, actualHeight - 120 * scaleY);

            // Text KK1 - Dưới cùng giữa
            txtKK1.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(txtKK1, ((actualWidth - txtKK1.DesiredSize.Width) / 2) + 100);
            Canvas.SetTop(txtKK1, actualHeight - 160 * scaleY);

            // Text KK2 - Dưới txtKK1
            txtKK2.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(txtKK2, ((actualWidth - txtKK2.DesiredSize.Width) / 2) + 100);
            Canvas.SetTop(txtKK2, actualHeight - 120 * scaleY);
        }

        public string ImagePath { get; set; }
        public string LogoPath { get; set; }
        public string[] PrizePaths { get; set; }

        private void LoadData()
        {
            try
            {
                // Load background image
                if (!string.IsNullOrEmpty(ImagePath))
                {
                    imgPrize.Source = new BitmapImage(new Uri(ImagePath));
                }

                // Load title
                if (!string.IsNullOrEmpty(Title))
                {
                    txtTitle.Text = Title;
                }

                // Load logo
                if (!string.IsNullOrEmpty(LogoPath))
                {
                    imgLogo.Source = new BitmapImage(new Uri(LogoPath));
                }

                // Load prize images (nếu có Image controls cho prizes trong XAML)
                if (PrizePaths != null && PrizePaths.Length >= 5)
                {
                    // Giả sử bạn có các Image controls tên là imgPrize1, imgPrize2, etc.
                    // Bạn cần thêm các controls này vào XAML của LuckyDrawWindow

                    // Ví dụ:
                    // if (!string.IsNullOrEmpty(PrizePaths[0]))
                    //     imgPrize1.Source = new BitmapImage(new Uri(PrizePaths[0]));
                    // if (!string.IsNullOrEmpty(PrizePaths[1]))
                    //     imgPrize2.Source = new BitmapImage(new Uri(PrizePaths[1]));
                    // ... và tiếp tục cho các prizes còn lại
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}