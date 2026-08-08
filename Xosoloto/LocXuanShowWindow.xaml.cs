using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Xosoloto
{
    /// <summary>
    /// Màn hình TRÌNH CHIẾU (Show Window) cho trò chơi "Lộc Xuân Đầu Năm" - dành cho khán giả /
    /// máy chiếu. Hiển thị lại chính xác nội dung của LuckyDrawWindow (title, logo, ảnh giải,
    /// các ô số trúng thưởng) nhưng KHÔNG có bất kỳ nút bấm/điều khiển nào. Mọi cập nhật (mở ô
    /// giải, nhập số trúng thưởng...) được đẩy từ màn hình CHỈNH SỬA (LuckyDrawWindow + 
    /// PrizeDisplayWindow) sang đây ngay lập tức - giống mô hình "Slide Show" của PowerPoint.
    /// </summary>
    public partial class LocXuanShowWindow : Window
    {
        private const double BASE_WIDTH = 1600;
        private const double BASE_HEIGHT = 900;

        private List<TextBlock> _prizeNumberBlocks = new();

        // Lưu lại số trúng thưởng của từng giải để có thể nạp ngay khi chuyển sang
        // DetailPanel (không cần chờ LuckyDrawWindow gọi UpdatePrizeNumber lại).
        private List<string> _prizeNumbers = new();

        // Index của giải đang được hiển thị CHI TIẾT (DetailPanel), -1 = đang ở màn hình TỔNG.
        private int _detailPrizeIndex = -1;

        public LocXuanShowWindow()
        {
            InitializeComponent();
        }

        /// <summary>Cập nhật dòng chữ trạng thái (đang trình chiếu ở màn hình phụ hay dùng
        /// chung màn hình chính) để người dùng luôn biết rõ đang ở chế độ nào.</summary>
        public void SetMonitorStatus(string text) => txtMonitorStatus.Text = text ?? string.Empty;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Đảm bảo cửa sổ nhận được sự kiện bàn phím (ESC) ngay cả khi Topmost.
            this.Focus();
            Keyboard.Focus(this);
        }

        /// <summary>Cho phép thoát màn hình Trình chiếu bằng phím ESC, tránh bị "kẹt" không có
        /// nút thoát khi cửa sổ này che kín màn hình Chỉnh sửa (đặc biệt khi máy chỉ có 1 màn hình).</summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.Close();
            }
        }

        private void BtnCloseShow_Click(object sender, RoutedEventArgs e) => this.Close();

        /// <summary>Nạp dữ liệu ban đầu: tiêu đề, logo, ảnh nền/giải và số lượng ô giải (bằng đúng số vòng đã cấu hình).</summary>
        public void Initialize(string title, string logoPath, string imagePath, int prizeCount)
        {
            txtTitle.Text = string.IsNullOrEmpty(title) ? "ĐẠI HỘI TẾT" : title;

            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                imgLogo.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                imgPrize.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));

            BuildPrizeSlots(prizeCount);
            UpdatePositions();
        }

        private void BuildPrizeSlots(int count)
        {
            _prizeNumberBlocks = new List<TextBlock>(count);
            _prizeNumbers = new List<string>(count);
            for (int i = 0; i < count; i++) _prizeNumbers.Add("- - - -");

            // Xoá các ô số cũ (nếu Initialize được gọi lại), giữ lại logo/title/ảnh giải.
            for (int i = MainCanvas.Children.Count - 1; i >= 0; i--)
            {
                if (MainCanvas.Children[i] is TextBlock tb && tb != txtTitle)
                    MainCanvas.Children.RemoveAt(i);
            }

            for (int i = 0; i < count; i++)
            {
                bool isLastSlot = i == count - 1;
                var block = new TextBlock
                {
                    Text = "- - - -",
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    FontSize = isLastSlot ? 35 : 56
                };
                _prizeNumberBlocks.Add(block);
                MainCanvas.Children.Add(block);
            }
        }

        /// <summary>Cập nhật số trúng thưởng cho một ô giải cụ thể (đẩy real-time từ LuckyDrawWindow).
        /// Cập nhật CẢ ô số ở màn hình Tổng LẪN màn hình Chi tiết (nếu đang mở đúng giải đó),
        /// để dù đang ở chế độ nào thì số hiển thị cũng luôn khớp.</summary>
        public void UpdatePrizeNumber(int prizeIndex, string prizeNumber)
        {
            if (prizeIndex >= 0 && prizeIndex < _prizeNumbers.Count)
                _prizeNumbers[prizeIndex] = prizeNumber;

            if (prizeIndex >= 0 && prizeIndex < _prizeNumberBlocks.Count)
            {
                _prizeNumberBlocks[prizeIndex].Text = prizeNumber;
                _prizeNumberBlocks[prizeIndex].Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            }

            if (_detailPrizeIndex == prizeIndex)
                txtDetailNumber.Text = prizeNumber;
        }

        /// <summary>
        /// Chuyển màn hình Trình chiếu sang chế độ CHI TIẾT 1 giải — khớp 100% bố cục của
        /// PrizeDisplayWindow (ảnh nền DetailsPrize.png, tên giải, số trúng thưởng cỡ lớn,
        /// ảnh giải). Gọi khi người điều khiển mở giải đó (LuckyDrawWindow.OpenPrizeWindow)
        /// hoặc chuyển sang giải khác bằng phím trái/phải trong PrizeDisplayWindow.
        /// </summary>
        public void ShowPrizeDetail(int prizeIndex, string prizeName, string prizeImagePath, string logoPath, string title)
        {
            _detailPrizeIndex = prizeIndex;

            txtDetailTitle.Text = string.IsNullOrEmpty(title) ? "ĐẠI HỘI TẾT" : title;

            try
            {
                if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                    imgDetailLogo.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));

                imgDetailBackground.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/DetailsPrize.png"));

                imgDetailPrize.Source = (!string.IsNullOrEmpty(prizeImagePath) && File.Exists(prizeImagePath))
                    ? new BitmapImage(new Uri(prizeImagePath, UriKind.Absolute))
                    : null;
            }
            catch
            {
                imgDetailPrize.Source = null;
            }

            txtDetailPrizeName.Text = prizeName ?? string.Empty;
            txtDetailNumber.Text = (prizeIndex >= 0 && prizeIndex < _prizeNumbers.Count) ? _prizeNumbers[prizeIndex] : "? ? ? ?";

            OverviewPanel.Visibility = Visibility.Collapsed;
            DetailPanel.Visibility = Visibility.Visible;
        }

        /// <summary>Đưa màn hình Trình chiếu quay lại chế độ TỔNG (hiển thị tất cả các giải cùng lúc).</summary>
        public void ShowOverview()
        {
            _detailPrizeIndex = -1;
            DetailPanel.Visibility = Visibility.Collapsed;
            OverviewPanel.Visibility = Visibility.Visible;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => UpdatePositions();

        private void UpdatePositions()
        {
            double actualWidth = MainCanvas.ActualWidth > 0 ? MainCanvas.ActualWidth : this.ActualWidth;
            double actualHeight = MainCanvas.ActualHeight > 0 ? MainCanvas.ActualHeight : this.ActualHeight;
            if (actualWidth == 0 || actualHeight == 0) return;

            double scaleY = actualHeight / BASE_HEIGHT;

            txtTitle.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(txtTitle, (actualWidth - txtTitle.DesiredSize.Width) / 2);
            Canvas.SetTop(txtTitle, 130 * scaleY);

            // Vị trí các ô số - xem giải thích chi tiết trong LuckyDrawControl.xaml.cs.UpdatePositions()
            // (phải giữ ĐÚNG các hằng số này ở cả 2 nơi để màn hình Trình chiếu khớp 100% với
            // màn hình Chỉnh sửa).
            const double COL0_X_FRAC = 0.4453;
            const double COL1_X_FRAC = 0.7688;
            const double ROW0_Y_FRAC = 0.4583;
            const double ROW_GAP_FRAC = 0.2174;

            int mainCount = Math.Max(0, _prizeNumberBlocks.Count - 1);
            for (int i = 0; i < mainCount; i++)
            {
                var block = _prizeNumberBlocks[i];
                int row = i / 2;
                int col = i % 2;

                block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double x = actualWidth * (col == 0 ? COL0_X_FRAC : COL1_X_FRAC);
                double y = actualHeight * (ROW0_Y_FRAC + row * ROW_GAP_FRAC);
                Canvas.SetLeft(block, x - block.DesiredSize.Width / 2);
                Canvas.SetTop(block, y - block.DesiredSize.Height / 2);
            }

            const double KK_X_FRAC = 0.5768;
            const double KK_Y_FRAC = 0.8815;
            if (_prizeNumberBlocks.Count > 0)
            {
                var kk = _prizeNumberBlocks[_prizeNumberBlocks.Count - 1];
                kk.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(kk, actualWidth * KK_X_FRAC - kk.DesiredSize.Width / 2);
                Canvas.SetTop(kk, actualHeight * KK_Y_FRAC - kk.DesiredSize.Height / 2);
            }
        }
    }
}
