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

        private string[] prizeNames = new string[]
        {
            "LỘC XUÂN 1",
            "LỘC XUÂN 2",
            "LỘC XUÂN 3",
            "LỘC XUÂN 4",
            "LỘC XUÂN 5"
        };

        public LuckyDrawWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            InitializeButtonEvents();
        }

        public LuckyDrawWindow(string imagePath, string title, string logoPath, string[] prizePaths)
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;

            this.ImagePath = imagePath;
            this.Title = title;
            this.LogoPath = logoPath;
            this.PrizePaths = prizePaths;

            InitializeButtonEvents();
            LoadData();
        }

        private void InitializeButtonEvents()
        {
            // Gắn sự kiện Click cho các buttons
            btn1.Click += Btn1_Click;
            btn2.Click += Btn2_Click;
            btn3.Click += Btn3_Click;
            btn4.Click += Btn4_Click;
            btn5.Click += Btn5_Click;
        }

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            OpenPrizeWindow(0); // Prize index 0 = Lộc Xuân 1
        }

        private void Btn2_Click(object sender, RoutedEventArgs e)
        {
            OpenPrizeWindow(1); // Prize index 1 = Lộc Xuân 2
        }

        private void Btn3_Click(object sender, RoutedEventArgs e)
        {
            OpenPrizeWindow(2); // Prize index 2 = Lộc Xuân 3
        }

        private void Btn4_Click(object sender, RoutedEventArgs e)
        {
            OpenPrizeWindow(3); // Prize index 3 = Lộc Xuân 4
        }

        private void Btn5_Click(object sender, RoutedEventArgs e)
        {
            OpenPrizeWindow(4); // Prize index 4 = Lộc Xuân 5
        }

        private void OpenPrizeWindow(int prizeIndex)
        {
            try
            {
                // Kiểm tra xem có prize images không
                if (PrizePaths == null || PrizePaths.Length == 0)
                {
                    MessageBox.Show("Chưa có hình ảnh giải thưởng!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Kiểm tra index hợp lệ
                if (prizeIndex < 0 || prizeIndex >= PrizePaths.Length)
                {
                    MessageBox.Show("Giải thưởng không hợp lệ!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Tạo và mở PrizeDisplayWindow
                PrizeDisplayWindow prizeWindow = new PrizeDisplayWindow(
                    title: this.Title,                    // Title từ LuckyDrawWindow
                    logoPath: this.LogoPath,              // Logo path
                    prizeName: prizeNames[prizeIndex],    // Tên giải (Lộc Xuân 1-5)
                    prizeIndex: prizeIndex,               // Index của giải
                    prizePaths: this.PrizePaths,          // Mảng tất cả prize images
                    backgroundPath: this.ImagePath 
                );

                // Ẩn LuckyDrawWindow
                this.Hide();

                // Hiển thị PrizeDisplayWindow
                prizeWindow.ShowDialog();

                // Hiện lại LuckyDrawWindow sau khi đóng PrizeDisplayWindow
                this.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ giải thưởng: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    imgPrize.Source = new BitmapImage(new Uri(ImagePath, UriKind.Absolute));
                }

                // Load title
                if (!string.IsNullOrEmpty(Title))
                {
                    txtTitle.Text = Title;
                }

                // Load logo
                if (!string.IsNullOrEmpty(LogoPath))
                {
                    imgLogo.Source = new BitmapImage(new Uri(LogoPath, UriKind.Absolute));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Public method để cập nhật số hiển thị
        public void UpdatePrizeNumbers(string loc1, string loc2, string loc3, string loc4)
        {
            if (!string.IsNullOrEmpty(loc1))
                txtLoc1.Text = FormatNumber(loc1);
            if (!string.IsNullOrEmpty(loc2))
                txtLoc2.Text = FormatNumber(loc2);
            if (!string.IsNullOrEmpty(loc3))
                txtLoc3.Text = FormatNumber(loc3);
            if (!string.IsNullOrEmpty(loc4))
                txtLoc4.Text = FormatNumber(loc4);
        }

        // Public method để cập nhật text khuyến khích
        public void UpdateIncentiveTexts(string kk1, string kk2)
        {
            if (!string.IsNullOrEmpty(kk1))
                txtKK1.Text = kk1;
            if (!string.IsNullOrEmpty(kk2))
                txtKK2.Text = kk2;
        }

        // Helper method để format số với khoảng cách
        private string FormatNumber(string number)
        {
            number = number.Replace(" ", "");
            if (number.Length >= 4)
            {
                return $"{number[0]} {number[1]} {number[2]} {number[3]}";
            }
            return number;
        }
    }
}