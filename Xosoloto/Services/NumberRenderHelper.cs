using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Xosoloto; // HistoryItem được định nghĩa trong MainWindow.xaml.cs (namespace Xosoloto)

namespace Xosoloto.Services
{
    /// <summary>
    /// Vẽ số vừa gọi (dạng Path/Geometry, giữ đúng font chữ viền như bản gốc) và dải lịch sử
    /// các số đã gọi. Được tách ra thành helper dùng chung để màn hình Chỉnh sửa (MainWindow)
    /// và màn hình Trình chiếu (LotoShowWindow) luôn hiển thị giống hệt nhau, không lặp code.
    /// </summary>
    public static class NumberRenderHelper
    {
        public static void RenderCurrentNumber(Path targetPath, TextBlock referenceTextBlock, string number, Brush foreground)
        {
            if (targetPath == null || referenceTextBlock == null) return;

            var typeface = new Typeface(
                referenceTextBlock.FontFamily,
                referenceTextBlock.FontStyle,
                referenceTextBlock.FontWeight,
                referenceTextBlock.FontStretch);

            if (string.IsNullOrEmpty(number))
            {
                targetPath.Data = Geometry.Empty;
                return;
            }

            var ft = new FormattedText(number, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, referenceTextBlock.FontSize,
                foreground ?? Brushes.Red, 96);
            targetPath.Data = ft.BuildGeometry(new Point(0, -10));
            targetPath.Fill = foreground ?? Brushes.Red;
        }

        public static void RenderHistory(Panel container, IEnumerable<HistoryItem> historyNumbers, Typeface typeface, double fontSize, double referenceMaxWidth)
        {
            if (container == null) return;

            container.Children.Clear();
            double x = 0, y = -5;
            double maxWidth = container.ActualWidth > 0 ? container.ActualWidth : referenceMaxWidth;

            foreach (var item in historyNumbers.Reverse())
            {
                var ft = new FormattedText(item.Value, CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, item.Color, 96);
                if (x + ft.Width > maxWidth) { x = 0; y += 40; }
                var path = new Path
                {
                    Data = ft.BuildGeometry(new Point(x, y)),
                    Fill = item.Color,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                container.Children.Add(path);
                x += ft.Width + 10;
            }
        }
    }
}
