using System.Linq;
using System.Windows;
using Forms = System.Windows.Forms;

namespace Xosoloto.Services
{
    /// <summary>
    /// Hỗ trợ đặt "màn hình Trình chiếu" (Show Window) lên đúng màn hình vật lý mong muốn,
    /// giống PowerPoint: màn hình Chỉnh sửa (Editor) nằm ở màn hình chính của người điều khiển,
    /// còn màn hình Trình chiếu (Show) sẽ tự động nhảy sang màn hình phụ (máy chiếu / TV) nếu có.
    /// Nếu máy chỉ có 1 màn hình, Show Window vẫn là một cửa sổ RIÊNG BIỆT (không dùng chung
    /// cửa sổ với Editor) để giữ đúng kiến trúc "2 màn hình tách biệt".
    /// </summary>
    public static class MonitorHelper
    {
        /// <summary>Có nhiều hơn 1 màn hình vật lý đang kết nối hay không.</summary>
        public static bool HasSecondaryMonitor => Forms.Screen.AllScreens.Length > 1;

        /// <summary>
        /// Mô tả ngắn gọn chế độ trình chiếu hiện tại, để hiển thị trên màn hình Trình chiếu
        /// (LocXuanShowWindow / LotoShowWindow) giúp người dùng biết ngay đang chạy 1 hay 2 màn
        /// hình, thay vì phải đoán - đây chính là nguyên nhân gây "kẹt" màn hình khi test trên
        /// máy chỉ có 1 màn hình (màn hình Trình chiếu che kín màn hình Chỉnh sửa bên dưới).
        /// </summary>
        public static string DescribeMode()
        {
            return HasSecondaryMonitor
                ? $"🖥️ Đang trình chiếu ở màn hình phụ ({Forms.Screen.AllScreens.Length} màn hình)"
                : "⚠️ Chỉ có 1 màn hình – đang dùng chung màn hình chính";
        }

        /// <summary>
        /// Đưa <paramref name="window"/> ra toàn màn hình phụ (nếu có), hoặc toàn màn hình chính
        /// nếu máy chỉ có 1 màn hình. Phải gọi SAU khi window đã Show() (hoặc trong Loaded) để
        /// WPF đã có handle hợp lệ; an toàn khi gọi nhiều lần (ví dụ khi cắm thêm màn hình).
        /// </summary>
        public static void PlaceOnShowMonitor(Window window)
        {
            var screens = Forms.Screen.AllScreens;
            var target = screens.Length > 1
                // Ưu tiên màn hình phụ đầu tiên khác với màn hình chính (Primary) làm màn hình Trình chiếu.
                ? (screens.FirstOrDefault(s => !s.Primary) ?? screens[1])
                : Forms.Screen.PrimaryScreen;

            if (target == null) return;

            var bounds = target.Bounds; // Toạ độ theo pixel vật lý của toàn bộ màn hình đó.

            window.WindowStyle = WindowStyle.None;
            window.ResizeMode = ResizeMode.NoResize;
            window.Topmost = true;
            window.WindowState = WindowState.Normal;
            window.WindowStartupLocation = WindowStartupLocation.Manual;

            // Dùng toạ độ "device-independent pixel" bằng cách quy đổi qua DPI của chính window đó.
            var source = System.Windows.PresentationSource.FromVisual(window);
            double dpiX = 1.0, dpiY = 1.0;
            if (source?.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            window.Left = bounds.Left / dpiX;
            window.Top = bounds.Top / dpiY;
            window.Width = bounds.Width / dpiX;
            window.Height = bounds.Height / dpiY;
            window.WindowState = WindowState.Maximized;
        }
    }
}
