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

        // Lưu trữ các số giải thưởng
        private string[] prizeNumbers = new string[5];

        // THÊM CÁC PROPERTIES
        public string ImagePath { get; set; }
        public string LogoPath { get; set; }
        public string[] PrizePaths { get; set; }

        public LuckyDrawWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            InitializeButtonEvents();
            InitializePrizeNumbers();
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
            InitializePrizeNumbers();
            LoadData();
        }

        private void InitializePrizeNumbers()
        {
            // Khởi tạo mảng số giải với giá trị mặc định
            for (int i = 0; i < prizeNumbers.Length; i++)
            {
                prizeNumbers[i] = GetDefaultPrizeNumber(i);
            }
        }

        private string GetDefaultPrizeNumber(int index)
        {
            // Trả về số mặc định hiển thị ban đầu
            switch (index)
            {
                case 0: return "1 1 1 1";
                case 1: return "2 2 2 2";
                case 2: return "3 3 3 3";
                case 3: return "4 4 4 4";
                default: return "? ? ? ?";
            }
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
            OpenPrizeWindow(0);
        }

        private void Btn2_Click(object sender, RoutedEventArgs e)
        {
            OpenPrizeWindow(1);
        }

        private void Btn3_Click(object sender, RoutedEventArgs e)
        {
            OpenPrizeWindow(2);
        }

        private void Btn4_Click(object sender, RoutedEventArgs e)
        {
            OpenPrizeWindow(3);
        }

        private void Btn5_Click(object sender, RoutedEventArgs e)
        {
            OpenPrizeWindow(4);
        }

        private void OpenPrizeWindow(int prizeIndex)
        {
            try
            {
                if (PrizePaths == null || PrizePaths.Length == 0)
                {
                    MessageBox.Show("Chưa có hình ảnh giải thưởng!", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (prizeIndex < 0 || prizeIndex >= PrizePaths.Length)
                {
                    MessageBox.Show("Giải thưởng không hợp lệ!", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Tạo và mở PrizeDisplayWindow - TRUYỀN THÊM PARENT
                PrizeDisplayWindow prizeWindow = new PrizeDisplayWindow(
                    title: this.Title,
                    logoPath: this.LogoPath,
                    prizeName: prizeNames[prizeIndex],
                    prizeIndex: prizeIndex,
                    prizePaths: this.PrizePaths,
                    backgroundPath: this.ImagePath,
                    imagePath: this.ImagePath,
                    parent: this
                );

                // Hiển thị PrizeDisplayWindow
                prizeWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở cửa sổ giải thưởng: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // PUBLIC METHOD - Cập nhật số giải thưởng từ PrizeDisplayWindow
        public void UpdatePrizeNumber(int prizeIndex, string prizeNumber)
        {
            if (prizeIndex < 0 || prizeIndex > 4) return;

            prizeNumbers[prizeIndex] = prizeNumber;

            // Cập nhật UI
            switch (prizeIndex)
            {
                case 0:
                    txtLoc1.Text = prizeNumber;
                    break;
                case 1:
                    txtLoc2.Text = prizeNumber;
                    break;
                case 2:
                    txtLoc3.Text = prizeNumber;
                    break;
                case 3:
                    txtLoc4.Text = prizeNumber;
                    break;
                case 4:
                    txtKK.Text = prizeNumber;
                    break;
            }
        }

        // PUBLIC METHOD - Lấy số giải thưởng hiện tại
        public string GetPrizeNumber(int prizeIndex)
        {
            if (prizeIndex < 0 || prizeIndex >= prizeNumbers.Length)
                return "? ? ? ?";

            return prizeNumbers[prizeIndex];
        }

        // PUBLIC METHOD - Cập nhật tất cả số trúng thưởng
        public void UpdateAllPrizeNumbers(string[] winningNumbers)
        {
            if (winningNumbers == null || winningNumbers.Length < 4) return;

            for (int i = 0; i < Math.Min(4, winningNumbers.Length); i++)
            {
                UpdatePrizeNumber(i, winningNumbers[i]);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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

            if (actualWidth == 0) actualWidth = this.ActualWidth;
            if (actualHeight == 0) actualHeight = this.ActualHeight;

            double scaleX = actualWidth / BASE_WIDTH;
            double scaleY = actualHeight / BASE_HEIGHT;

            // Title
            txtTitle.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(txtTitle, (actualWidth - txtTitle.DesiredSize.Width) / 2);
            Canvas.SetTop(txtTitle, 170 * scaleY);

            // Lộc Xuân 1
            txtLoc1.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double loc1X = actualWidth - 1000 * scaleX + 100 * scaleX;
            double loc1Y = 340 * scaleY + 30;
            Canvas.SetLeft(txtLoc1, loc1X - txtLoc1.DesiredSize.Width / 2);
            Canvas.SetTop(txtLoc1, loc1Y);

            // Lộc Xuân 2
            txtLoc2.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double loc2X = actualWidth - 1000 * scaleX + 600 * scaleX;
            double loc2Y = 340 * scaleY + 30;
            Canvas.SetLeft(txtLoc2, loc2X - txtLoc2.DesiredSize.Width / 2);
            Canvas.SetTop(txtLoc2, loc2Y);

            // Lộc Xuân 3
            txtLoc3.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double loc3X = actualWidth - 1000 * scaleX + 100 * scaleX;
            double loc3Y = 340 * scaleY + 200;
            Canvas.SetLeft(txtLoc3, loc3X - txtLoc3.DesiredSize.Width / 2);
            Canvas.SetTop(txtLoc3, loc3Y);

            // Lộc Xuân 4
            txtLoc4.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double loc4X = actualWidth - 1000 * scaleX + 600 * scaleX;
            double loc4Y = 340 * scaleY + 200;
            Canvas.SetLeft(txtLoc4, loc4X - txtLoc4.DesiredSize.Width / 2);
            Canvas.SetTop(txtLoc4, loc4Y);

            // Buttons
            spButtons.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(spButtons, 100 * scaleX);
            Canvas.SetTop(spButtons, actualHeight - 120 * scaleY);

            // Text KK1
            txtKK.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(txtKK, ((actualWidth - txtKK.DesiredSize.Width) / 2) + 100);
            Canvas.SetTop(txtKK, actualHeight - 160 * scaleY);

          
        }

        private void LoadData()
        {
            try
            {
                if (!string.IsNullOrEmpty(ImagePath))
                {
                    imgPrize.Source = new BitmapImage(new Uri(ImagePath, UriKind.Absolute));
                }

                if (!string.IsNullOrEmpty(Title))
                {
                    txtTitle.Text = Title;
                }

                if (!string.IsNullOrEmpty(LogoPath))
                {
                    imgLogo.Source = new BitmapImage(new Uri(LogoPath, UriKind.Absolute));
                }

                // Cập nhật số giải ban đầu
                for (int i = 0; i < 4; i++)
                {
                    UpdatePrizeNumber(i, prizeNumbers[i]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Public method để cập nhật text khuyến khích
        public void UpdateIncentiveTexts(string kk1, string kk2)
        {
            if (!string.IsNullOrEmpty(kk1))
                txtKK.Text = kk1;
           
        }
    }
}