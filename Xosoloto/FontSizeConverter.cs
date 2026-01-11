using System;
using System.Globalization;
using System.Windows.Data;

namespace Xosoloto
{
    public class FontSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double windowWidth = (double)value;
            // Tùy chỉnh công thức này để có kết quả phù hợp với bạn
            // Ví dụ: chia chiều rộng cho một số để có font size phù hợp
            return windowWidth / 50;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}