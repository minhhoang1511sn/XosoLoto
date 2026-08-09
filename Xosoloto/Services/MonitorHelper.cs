using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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

        // Theo dõi window nào đã được gắn hook WM_GETMINMAXINFO rồi, để không gắn trùng lặp
        // khi PlaceOnShowMonitor được gọi lại nhiều lần (mỗi lần mở 1 giải, đổi giải...).
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Window, object> _hookedWindows = new();

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

            // FIX "không full màn hình khi 2 màn hình có Windows Display Scale khác nhau":
            // Tự tính Width/Height bằng cách chia bounds vật lý cho DPI đo được ở code C# (dù
            // đo tại thời điểm nào) LUÔN có rủi ro sai lệch, vì WPF là PerMonitorV2 - giá trị
            // DPI "đúng" chỉ chốt lại SAU khi cửa sổ đã thật sự đứng trên màn hình đích, mà việc
            // đó lại xảy ra bất đồng bộ (WM_DPICHANGED). Thay vì tự đoán DPI, ta chặn thẳng
            // thông điệp Win32 WM_GETMINMAXINFO - đây là thông điệp chính HĐH gửi tới cửa sổ để
            // HỎI "khi Maximize thì kích thước/vị trí bao nhiêu?", và ta trả lời bằng đúng
            // rcMonitor (toàn bộ màn hình, đã tính đúng theo DPI THẬT của màn hình đó) lấy từ
            // MonitorFromWindow + GetMonitorInfo của Win32 - hai hàm này luôn cho kết quả ĐÚNG
            // 100% bất kể 2 màn hình lệch tỉ lệ scale bao nhiêu, vì đó là chính là API Windows
            // dùng nội bộ để tự Maximize cửa sổ bình thường. Cách này bỏ hẳn việc tự quy đổi DPI
            // thủ công (vốn là nguồn gốc lỗi viền đen/trắng thừa trước đây).
            EnsureMaxBoundsHook(window);

            // Bước 1: đưa cửa sổ về trạng thái Normal, kích thước nhỏ, neo vào GÓC TRÊN-TRÁI
            // của màn hình đích (chỉ cần điểm neo rơi đúng vào màn hình đích để
            // MonitorFromWindow ở bước 2 chọn đúng màn hình - sai số DPI ở bước này không
            // quan trọng vì bước 2 sẽ ghi đè lại kích thước chính xác qua hook).
            var source = PresentationSource.FromVisual(window);
            double dpiX = 1.0, dpiY = 1.0;
            if (source?.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }
            window.WindowState = WindowState.Normal;
            window.Left = (bounds.Left / dpiX) + 10;
            window.Top = (bounds.Top / dpiY) + 10;
            window.Width = 200;
            window.Height = 200;

            // Bước 2: Maximize - HĐH sẽ gửi WM_GETMINMAXINFO tới đúng cửa sổ này, hook ở trên
            // trả lời bằng bounds CHÍNH XÁC của màn hình đích (đã tính đúng DPI bởi chính
            // Windows), nên kết quả luôn full kín màn hình dù 2 màn hình lệch tỉ lệ scale.
            window.WindowState = WindowState.Maximized;
        }

        /// <summary>Gắn (đúng 1 lần) hook chặn WM_GETMINMAXINFO để tự trả lời đúng kích thước/
        /// vị trí Maximize theo màn hình mà cửa sổ ĐANG đứng (MonitorFromWindow tự xác định
        /// đúng màn hình + đúng DPI, không cần code tự đoán).</summary>
        private static void EnsureMaxBoundsHook(Window window)
        {
            if (_hookedWindows.TryGetValue(window, out _)) return;

            var hwndSource = (HwndSource)PresentationSource.FromVisual(window);
            if (hwndSource == null) return; // Chưa có handle (chưa Show()) - bỏ qua, lần gọi sau sẽ gắn được.

            hwndSource.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                const int WM_GETMINMAXINFO = 0x0024;
                if (msg == WM_GETMINMAXINFO)
                {
                    ApplyMonitorMaxBounds(hwnd, lParam);
                    handled = true;
                }
                return IntPtr.Zero;
            });

            _hookedWindows.Add(window, new object());
        }

        /// <summary>Ghi đè MINMAXINFO (kích thước khi Maximize) bằng đúng toàn bộ vùng của màn
        /// hình vật lý đang chứa cửa sổ (rcMonitor - lấy full màn hình, KHÔNG trừ taskbar, vì
        /// đây là cửa sổ trình chiếu toàn màn hình / borderless).</summary>
        private static void ApplyMonitorMaxBounds(IntPtr hwnd, IntPtr lParam)
        {
            const int MONITOR_DEFAULTTONEAREST = 2;

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) return;

            var monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            if (!GetMonitorInfo(monitor, ref monitorInfo)) return;

            RECT rc = monitorInfo.rcMonitor;

            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // ptMaxPosition tính TƯƠNG ĐỐI so với góc trên-trái của chính màn hình đó (không
            // phải toạ độ tuyệt đối trên toàn bộ desktop ảo) - đúng theo tài liệu Win32.
            mmi.ptMaxPosition.X = 0;
            mmi.ptMaxPosition.Y = 0;
            mmi.ptMaxSize.X = rc.Right - rc.Left;
            mmi.ptMaxSize.Y = rc.Bottom - rc.Top;
            mmi.ptMaxTrackSize.X = mmi.ptMaxSize.X;
            mmi.ptMaxTrackSize.Y = mmi.ptMaxSize.Y;

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        #region Win32 interop (WM_GETMINMAXINFO)

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        #endregion
    }
}
