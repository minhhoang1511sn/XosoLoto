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
        /// nút thoát khi cửa sổ này che kín màn hình Chỉnh sửa (đặc biệt khi máy chỉ có 1 màn hình).
        /// DÙNG Hide() THAY VÌ Close(): đóng hẳn (Close) cửa sổ Topmost/borderless này có rủi ro
        /// bị WPF hiểu nhầm là "không còn cửa sổ nào đang mở" tại một số thời điểm trong luồng
        /// khởi tạo lồng nhau (InitLocXuan -> LuckyDrawWindow -> LocXuanShowWindow), khiến
        /// ShutdownMode=OnLastWindowClose tự động thoát LUÔN CẢ ỨNG DỤNG một cách ngoài ý muốn.
        /// Hide() vẫn giữ cửa sổ trong danh sách Application.Windows nên không bao giờ gây ra
        /// tình huống đó, trong khi vẫn đạt đúng mục đích: cửa sổ biến mất khỏi màn hình chiếu.</summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                SafeHide();
            }
        }

        private void BtnCloseShow_Click(object sender, RoutedEventArgs e) => SafeHide();

        /// <summary>Ẩn cửa sổ Trình chiếu một cách an toàn, bọc try/catch để nếu có lỗi bất ngờ
        /// xảy ra, người dùng thấy thông báo lỗi thay vì ứng dụng tự đóng/thoát mà không rõ lý do.</summary>
        private void SafeHide()
        {
            try
            {
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đóng màn hình Trình chiếu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Nạp dữ liệu ban đầu: tiêu đề, logo, ảnh nền/giải và số lượng ô giải (bằng đúng số vòng đã cấu hình).</summary>
        public void Initialize(string title, string logoPath, string imagePath, int prizeCount)
        {
            txtTitle.Text = string.IsNullOrEmpty(title) ? "ĐẠI HỘI TẾT" : title;

            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                imgLogo.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                imgPrize.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));

            BuildPrizeSlots(prizeCount);
            CenterTitle();
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
                PositionPrizeBlock(block, i, isLastSlot);
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
                var block = _prizeNumberBlocks[prizeIndex];
                block.Text = prizeNumber;
                // Nội dung đổi độ dài (vd "- - - -" -> "1 2 3 4") làm DesiredSize đổi theo,
                // nên phải tính lại vị trí để ô số vẫn canh giữa đúng khung, không bị lệch.
                bool isLastSlot = prizeIndex == _prizeNumberBlocks.Count - 1;
                PositionPrizeBlock(block, prizeIndex, isLastSlot);
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

            // Giải Khuyến khích có nội dung dài hơn (thường 8-9 số, không format cách nhau) -
            // giảm cỡ chữ để vẫn nằm gọn trong khung số, không bị tràn ra ngoài. Phải khớp
            // đúng logic bên PrizeDisplayWindow.xaml.cs (DisplayPrize) để 2 màn hình giống nhau.
            bool isKhuyenKhich = prizeIndex >= 0 && prizeIndex == _prizeNumberBlocks.Count - 1;
            if (isKhuyenKhich)
            {
                txtDetailNumber.FontSize = 60;
                txtDetailPrizeName.FontSize = 50;
            }
            else
            {
                txtDetailNumber.FontSize = 180;
                txtDetailPrizeName.FontSize = 70;
            }

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

        // Cửa sổ giờ vẽ trên 1 canvas ẢO cố định BASE_WIDTH x BASE_HEIGHT, được Viewbox bên
        // ngoài co giãn ĐỀU cho khớp màn hình/máy chiếu thực tế (xem LocXuanShowWindow.xaml).
        // Không còn cần tính lại vị trí thủ công theo ActualWidth/ActualHeight khi resize nữa.
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) { }

        /// <summary>Không còn cần tự đo/canh giữa tiêu đề bằng tay nữa: txtTitle giờ được bọc
        /// trong 1 Viewbox kích thước cố định (xem LocXuanShowWindow.xaml) tự canh giữa và tự
        /// thu nhỏ cỡ chữ nếu tiêu đề quá dài, luôn vừa khít khung trên mọi màn hình. Giữ lại
        /// hàm rỗng này để không phải sửa nơi đang gọi CenterTitle().</summary>
        private void CenterTitle()
        {
        }

        // Vị trí các ô số - xem giải thích chi tiết trong LuckyDrawControl.xaml.cs (phải giữ
        // ĐÚNG các hằng số này ở cả 2 nơi để màn hình Trình chiếu khớp 100% với màn hình Chỉnh sửa).
        private const double COL0_X_FRAC = 0.4453;
        private const double COL1_X_FRAC = 0.7688;
        private const double ROW0_Y_FRAC = 0.4583;
        private const double ROW_GAP_FRAC = 0.2174;
        private const double KK_X_FRAC = 0.5768;
        private const double KK_Y_FRAC = 0.8815;

        /// <summary>Đặt vị trí cố định (trên canvas gốc BASE_WIDTH x BASE_HEIGHT) cho 1 ô số giải.</summary>
        private void PositionPrizeBlock(TextBlock block, int index, bool isLastSlot)
        {
            block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            double x, y;
            if (isLastSlot)
            {
                x = BASE_WIDTH * KK_X_FRAC;
                y = BASE_HEIGHT * KK_Y_FRAC;
            }
            else
            {
                int row = index / 2;
                int col = index % 2;
                x = BASE_WIDTH * (col == 0 ? COL0_X_FRAC : COL1_X_FRAC);
                y = BASE_HEIGHT * (ROW0_Y_FRAC + row * ROW_GAP_FRAC);
            }

            Canvas.SetLeft(block, x - block.DesiredSize.Width / 2);
            Canvas.SetTop(block, y - block.DesiredSize.Height / 2);
        }
    }
}
