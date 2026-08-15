using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Xosoloto.Services;

namespace Xosoloto
{
    /// <summary>
    /// Màn hình TRÌNH CHIẾU (Show Window) cho trò chơi "Loto Vui Xuân" - dành cho khán giả /
    /// máy chiếu. Không có bất kỳ control chỉnh sửa nào; toàn bộ nội dung được đẩy sang từ
    /// màn hình CHỈNH SỬA (MainWindow, Editor) theo thời gian thực, giống mô hình
    /// "Slide Show" của PowerPoint (tách biệt với "Presenter View").
    /// </summary>
    public partial class LotoShowWindow : Window
    {
        public LotoShowWindow()
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
        /// bị WPF hiểu nhầm là "không còn cửa sổ nào đang mở" tại một số thời điểm, khiến
        /// ShutdownMode=OnLastWindowClose tự động thoát LUÔN CẢ ỨNG DỤNG ngoài ý muốn. Hide()
        /// vẫn giữ cửa sổ trong danh sách Application.Windows nên không bao giờ gây ra tình
        /// huống đó, trong khi vẫn đạt đúng mục đích: cửa sổ biến mất khỏi màn hình chiếu.</summary>
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
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi khi đóng màn hình Trình chiếu: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PlayVideo() => mediaElement.Play();
        public void StopVideo() => mediaElement.Stop();

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaElement.Position = System.TimeSpan.FromMilliseconds(1);
            mediaElement.Play();
        }

        public void SetVongLoai(string text, SolidColorBrush mauVe)
        {
            VongLoaiTextBlock.Text = text ?? "--";
            if (mauVe != null) MauVeBorder.Background = mauVe;
        }

        public void SetCurrentNumber(string number, Brush foreground)
        {
            NumberRenderHelper.RenderCurrentNumber(NumberPath, CurrentNumberTextBlock, number, foreground);
        }

        public void SetHistory(IEnumerable<HistoryItem> history, Typeface typeface, double fontSize)
        {
            NumberRenderHelper.RenderHistory(HistoryNumberContainer, history, typeface, fontSize, HistoryNumberContainer.ActualWidth);
        }

        public void ClearAll()
        {
            NumberPath.Data = Geometry.Empty;
        }
    }
}
