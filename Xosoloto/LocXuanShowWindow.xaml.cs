using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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

        public LocXuanShowWindow()
        {
            InitializeComponent();
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
            UpdatePositions();
        }

        private void BuildPrizeSlots(int count)
        {
            _prizeNumberBlocks = new List<TextBlock>(count);
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

        /// <summary>Cập nhật số trúng thưởng cho một ô giải cụ thể (đẩy real-time từ LuckyDrawWindow).</summary>
        public void UpdatePrizeNumber(int prizeIndex, string prizeNumber)
        {
            if (prizeIndex < 0 || prizeIndex >= _prizeNumberBlocks.Count) return;
            _prizeNumberBlocks[prizeIndex].Text = prizeNumber;
            _prizeNumberBlocks[prizeIndex].Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => UpdatePositions();

        private void UpdatePositions()
        {
            double actualWidth = MainCanvas.ActualWidth > 0 ? MainCanvas.ActualWidth : this.ActualWidth;
            double actualHeight = MainCanvas.ActualHeight > 0 ? MainCanvas.ActualHeight : this.ActualHeight;
            if (actualWidth == 0 || actualHeight == 0) return;

            double scaleX = actualWidth / BASE_WIDTH;
            double scaleY = actualHeight / BASE_HEIGHT;

            txtTitle.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(txtTitle, (actualWidth - txtTitle.DesiredSize.Width) / 2);
            Canvas.SetTop(txtTitle, 130 * scaleY);

            // Lưới 2 cột cho các giải chính (giống LuckyDrawWindow.UpdatePositions), ô cuối
            // cùng (Khuyến khích) canh giữa phía dưới.
            int mainCount = Math.Max(0, _prizeNumberBlocks.Count - 1);
            for (int i = 0; i < mainCount; i++)
            {
                var block = _prizeNumberBlocks[i];
                int row = i / 2;
                int col = i % 2;

                block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double x = actualWidth - 1000 * scaleX + (col == 0 ? 100 : 600) * scaleX;
                double y = 340 * scaleY + 30 + row * 200;
                Canvas.SetLeft(block, x - block.DesiredSize.Width / 2);
                Canvas.SetTop(block, y);
            }

            if (_prizeNumberBlocks.Count > 0)
            {
                var kk = _prizeNumberBlocks[_prizeNumberBlocks.Count - 1];
                kk.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(kk, ((actualWidth - kk.DesiredSize.Width) / 2) + 60 - 55);
                Canvas.SetTop(kk, actualHeight - 160 * scaleY);
            }
        }
    }
}
