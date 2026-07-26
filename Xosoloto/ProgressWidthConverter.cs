using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace YourApp
{
    /// <summary>
    /// Chuyển Value của ProgressBar thành Width pixel dựa trên ActualWidth của parent.
    /// Dùng trong ControlTemplate để tính độ rộng phần fill.
    /// </summary>
    public class ProgressWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value = ProgressBar.Value (0–100)
            // parameter = ActualWidth của track (bind trong template)
            if (value is double progressValue && parameter is double totalWidth)
            {
                return totalWidth * (progressValue / 100.0);
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }
}

/*
 * ============================================================
 * CÁCH DÙNG CONVERTER TRONG TEMPLATE (nếu cần bind ActualWidth):
 *
 * Thay vì dùng Converter ở trên, cách đơn giản nhất trong WPF
 * là dùng sẵn PART_Indicator bên trong ProgressBar template.
 * WPF tự động xử lý width của PART_Indicator qua built-in logic.
 *
 * Chỉ cần đặt đúng tên x:Name="PART_Indicator" trên Border fill,
 * WPF sẽ tự scale theo Value/Maximum mà không cần Converter.
 *
 * Xem lại StripedProgressBar.xaml — các Border fill đã đặt
 * x:Name="PART_Indicator" đúng chuẩn WPF ProgressBar contract.
 * ============================================================
 */
