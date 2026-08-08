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
