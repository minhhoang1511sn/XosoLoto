using System.Collections.Generic;
using System.Windows;
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
