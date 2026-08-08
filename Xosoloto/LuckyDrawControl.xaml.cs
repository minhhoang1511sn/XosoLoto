using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xosoloto.Services;

namespace Xosoloto
{
    public partial class LuckyDrawWindow : Window
    {
        /// <summary>
        /// Màn hình Trình chiếu (Show Window) dành cho khán giả/máy chiếu khi chơi "Lộc Xuân
        /// Đầu Năm", tách biệt hoàn toàn với cửa sổ điều khiển này (màn hình Chỉnh sửa) - giống
        /// mô hình Presenter View / Slide Show của PowerPoint. Mọi thao tác nhập số trúng
        /// thưởng (kể cả từ PrizeDisplayWindow) được đẩy sang đây ngay lập tức.
        /// </summary>
        private LocXuanShowWindow _showWindow;
        // Kích thước chuẩn
        private const double BASE_WIDTH = 1600;
        private const double BASE_HEIGHT = 900;

        // Bảng màu xoay vòng cho các nút bấm (số vòng động, không còn giới hạn 5).
        private static readonly string[] ButtonColors =
        {
            "#4CAF50", "#2196F3", "#FF9800", "#E91E63", "#9C27B0",
            "#00BCD4", "#795548", "#607D8B", "#8BC34A", "#3F51B5",
            "#FFC107", "#009688"
        };

        // THÊM CÁC PROPERTIES
        public string ImagePath { get; set; }
        public string LogoPath { get; set; }
        public string[] PrizePaths { get; set; }

        /// <summary>
        /// True nếu người dùng bấm "🔁 Đổi loại game" ngay trong màn hình quay giải này để
        /// thoát ra và quay lại màn hình chọn loại game, thay vì phải đóng hẳn ứng dụng.
        /// InitLocXuan.BtnDone_Click kiểm tra cờ này sau khi ShowDialog() trả về.
        /// </summary>
        public bool ChangeGameRequested { get; private set; } = false;

        // Ô số hiển thị mỗi giải (sinh động theo PrizePaths.Length).
        private List<TextBlock> prizeNumberBlocks = new();
        private List<string> prizeNumbers = new();
        private List<string> prizeNames = new();

        public LuckyDrawWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
        }

        public LuckyDrawWindow(string imagePath, string title, string logoPath, string[] prizePaths)
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            this.Closed += (s, e) => CloseShowWindow();

            this.ImagePath = imagePath;
            this.Title = title;
            this.LogoPath = logoPath;
            this.PrizePaths = prizePaths ?? Array.Empty<string>();

            BuildDynamicPrizeSlots();
            LoadData();
            EnsureShowWindow();
        }

        /// <summary>
        /// Mở (hoặc đưa lên foreground) màn hình Trình chiếu. Nếu máy có 2 màn hình trở lên,
        /// màn hình này tự động full-screen ở màn hình phụ (máy chiếu/TV), tách biệt với cửa
        /// sổ điều khiển hiện tại.
        /// </summary>
        private void EnsureShowWindow()
        {
            if (_showWindow == null)
            {
                _showWindow = new LocXuanShowWindow();
                _showWindow.Show();
            }
            MonitorHelper.PlaceOnShowMonitor(_showWindow);
            _showWindow.Initialize(this.Title, this.LogoPath, this.ImagePath, PrizePaths?.Length ?? 0);
            for (int i = 0; i < prizeNumbers.Count; i++)
                _showWindow.UpdatePrizeNumber(i, prizeNumbers[i]);
        }

        private void CloseShowWindow()
        {
            if (_showWindow == null) return;
            _showWindow.Close();
            _showWindow = null;
        }

        /// <summary>
        /// Sinh động (theo số vòng = PrizePaths.Length) các ô TextBlock hiển thị số trúng
        /// thưởng và các nút mở màn hình nhập số cho từng giải. Vị trí ô số được sắp theo
        /// lưới 2 cột (giống bố cục gốc cho 4 giải "Lộc Xuân"); ô cuối cùng luôn là giải
        /// "Khuyến khích" và được canh giữa phía dưới, giữ đúng hành vi cũ khi có 5 giải.
        /// </summary>
        private void BuildDynamicPrizeSlots()
        {
            int count = PrizePaths?.Length ?? 0;

            prizeNumberBlocks = new List<TextBlock>(count);
            prizeNumbers = new List<string>(count);
            prizeNames = new List<string>(count);
            spButtons.Children.Clear();

            for (int i = 0; i < count; i++)
            {
                bool isLastSlot = i == count - 1;
                prizeNames.Add(isLastSlot ? "KHUYẾN KHÍCH" : $"LỘC XUÂN {i + 1}");
                prizeNumbers.Add("- - - -");

                var block = new TextBlock
                {
                    Text = "- - - -",
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    FontSize = isLastSlot ? 35 : 56
                };
                prizeNumberBlocks.Add(block);
                MainCanvas.Children.Add(block);

                int capturedIndex = i;
                var btn = new Button
                {
                    Content = (i + 1).ToString(),
                    Width = 50,
                    Height = 50,
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ButtonColors[i % ButtonColors.Length])),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                var cornerStyle = new Style(typeof(Border));
                cornerStyle.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(25)));
                btn.Resources.Add(typeof(Border), cornerStyle);
                btn.Click += (s, e) => OpenPrizeWindow(capturedIndex);
                spButtons.Children.Add(btn);
            }
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

        private void btnToggleShow_Click(object sender, RoutedEventArgs e)
        {
            if (_showWindow == null) EnsureShowWindow();
            else CloseShowWindow();
        }

        /// <summary>
        /// Thoát khỏi màn hình quay giải Lộc Xuân hiện tại và quay lại màn hình chọn loại
        /// game, thay vì phải đóng hẳn ứng dụng. Đóng luôn màn hình Trình chiếu (nếu đang
        /// mở) trước khi thoát.
        /// </summary>
        private void btnChangeGame_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có muốn thoát khỏi Lộc Xuân Đầu Năm và chọn lại loại game khác không?\n" +
                "Kết quả quay giải hiện tại (nếu có) sẽ không được lưu lại.",
                "Đổi loại game", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            ChangeGameRequested = true;
            this.Close();
        }

        // PUBLIC METHOD - Cập nhật số giải thưởng từ PrizeDisplayWindow
        public void UpdatePrizeNumber(int prizeIndex, string prizeNumber)
        {
            if (prizeIndex < 0 || prizeIndex >= prizeNumberBlocks.Count) return;

            prizeNumbers[prizeIndex] = prizeNumber;
            prizeNumberBlocks[prizeIndex].Text = prizeNumber;

            // Đẩy real-time sang màn hình Trình chiếu (khán giả/máy chiếu).
            _showWindow?.UpdatePrizeNumber(prizeIndex, prizeNumber);
        }

        // PUBLIC METHOD - Lấy số giải thưởng hiện tại
        public string GetPrizeNumber(int prizeIndex)
        {
            if (prizeIndex < 0 || prizeIndex >= prizeNumbers.Count)
                return "? ? ? ?";

            return prizeNumbers[prizeIndex];
        }

        // PUBLIC METHOD - Cập nhật tất cả số trúng thưởng
        public void UpdateAllPrizeNumbers(string[] winningNumbers)
        {
            if (winningNumbers == null) return;

            int max = Math.Min(prizeNumberBlocks.Count, winningNumbers.Length);
            for (int i = 0; i < max; i++)
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
            Canvas.SetTop(txtTitle, 130 * scaleY);

            // Các ô số giải (trừ ô cuối cùng = Khuyến khích) được xếp lưới 2 cột,
            // đúng bố cục gốc khi có 4 giải "Lộc Xuân" (0,0) (0,1) (1,0) (1,1)...
            int mainCount = Math.Max(0, prizeNumberBlocks.Count - 1);
            for (int i = 0; i < mainCount; i++)
            {
                var block = prizeNumberBlocks[i];
                int row = i / 2;
                int col = i % 2;

                block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double x = actualWidth - 1000 * scaleX + (col == 0 ? 100 : 600) * scaleX;
                double y = 340 * scaleY + 30 + row * 200;
                Canvas.SetLeft(block, x - block.DesiredSize.Width / 2);
                Canvas.SetTop(block, y);
            }

            // Buttons
            spButtons.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(spButtons, 100 * scaleX);
            Canvas.SetTop(spButtons, actualHeight - 120 * scaleY);

            // Ô số cuối cùng = giải Khuyến khích, canh giữa phía dưới (như txtKK gốc).
            if (prizeNumberBlocks.Count > 0)
            {
                var kk = prizeNumberBlocks[prizeNumberBlocks.Count - 1];
                kk.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(kk, ((actualWidth - kk.DesiredSize.Width) / 2) + 60 - 55);
                Canvas.SetTop(kk, actualHeight - 160 * scaleY);
            }
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
                for (int i = 0; i < prizeNumbers.Count; i++)
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
    }
}
