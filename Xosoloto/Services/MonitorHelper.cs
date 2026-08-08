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
            window.WindowStartupLocation = WindowStartupLocation.Manual;

            // QUAN TRỌNG - vì sao KHÔNG tự tính Width/Height bằng cách chia bounds cho DPI:
            // Ngay tại thời điểm này, "source" (PresentationSource của window) vẫn đang phản
            // ánh DPI của màn hình MÀ WINDOW ĐANG ĐỨNG (thường là màn hình chính, nơi Editor
            // mở lên), KHÔNG PHẢI DPI của "target" (màn hình đích ta sắp chuyển cửa sổ sang).
            // Nếu 2 màn hình có tỉ lệ Windows Display Scale khác nhau (rất phổ biến khi dùng
            // máy chiếu/TV), việc chia bounds vật lý của màn hình đích cho DPI của màn hình
            // NGUỒN sẽ ra sai số -> nội dung bị lệch tỉ lệ so với khung Viewbox.
            //
            // Cách làm ĐÚNG (và cũng là khuyến nghị chuẩn của Windows cho app PerMonitorV2 -
            // xem app.manifest): chỉ cần đặt (Left, Top) của cửa sổ SAO CHO PHẦN LỚN diện tích
            // cửa sổ rơi vào bên trong màn hình đích, sau đó để chính Windows tự Maximize().
            // Khi Maximize, HĐH tự tính lại đúng Width/Height theo DPI THẬT của màn hình đích -
            // ứng dụng không cần tự làm phép tính DPI thủ công nữa, và vì đã khai báo
            // PerMonitorV2 nên WPF sẽ tự vẽ lại (DpiChanged) đúng nét, không bị bitmap-stretch.
            var source = System.Windows.PresentationSource.FromVisual(window);
            double dpiX = 1.0, dpiY = 1.0;
            if (source?.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }

            // Bước 1: đưa cửa sổ về trạng thái Normal, kích thước nhỏ, neo vào GÓC TRÊN-TRÁI
            // của màn hình đích (sai số quy đổi DPI ở bước này không quan trọng - chỉ cần điểm
            // neo rơi đúng vào màn hình đích để bước Maximize bên dưới chọn đúng màn hình).
            window.WindowState = WindowState.Normal;
            window.Left = (bounds.Left / dpiX) + 10;
            window.Top = (bounds.Top / dpiY) + 10;
            window.Width = 200;
            window.Height = 200;

            // Bước 2: Maximize - Windows tự nhận diện màn hình chứa phần lớn cửa sổ hiện tại
            // (vừa neo ở bước 1) và tự tính đúng kích thước full-screen theo DPI thật của
            // CHÍNH màn hình đó, không lệ thuộc vào phép chia DPI thủ công ở trên nữa.
            window.WindowState = WindowState.Maximized;
        }
    }
}
