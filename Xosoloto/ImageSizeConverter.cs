using System;
using System.Globalization;
using System.Windows.Data;

namespace Xosoloto
{
    public class ImageSizeConverter : IValueConverter
    {
        // Tỷ lệ gốc width/height của image mà bạn muốn giữ
        public double AspectRatio { get; set; } = 1.0; // ví dụ 16/9 = 1.7778

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double windowWidth)
            {
                // Chia tỉ lệ: scale width theo window, tự động tính height theo aspect ratio
                if (parameter?.ToString() == "Height")
                {
                    // Nếu bind Height thì trả về width / aspect ratio
                    return windowWidth / AspectRatio;
                }
                else
                {
                    // Bind Width thì lấy windowWidth * hệ số (ví dụ 0.5)
                    return windowWidth * 0.5;
                }
            }
            return 100; // default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
