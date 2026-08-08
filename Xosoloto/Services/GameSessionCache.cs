using System.Collections.Generic;
using Xosoloto;

namespace Xosoloto.Services
{
    /// <summary>
    /// Snapshot toàn bộ trạng thái ĐANG CHƠI của màn hình "Loto Vui Xuân" (vòng loại đang
    /// chọn, màu vé, số vừa quay, lịch sử các số đã quay...) tại thời điểm người dùng bấm
    /// "🔁 Đổi loại game" để chuyển sang game khác.
    /// </summary>
    public class LotoVuiXuanSession
    {
        public Dictionary<int, VongLoaiInfo> VongLoaiConfig { get; set; }
        public int SelectedVongLoaiIndex { get; set; }
        public string SelectedMauVe { get; set; }
        public List<HistoryItem> HistoryNumbers { get; set; } = new();
        public string CurrentNumber { get; set; } = "";
        public bool IsNumberColorRed { get; set; } = true;
    }

    /// <summary>
    /// Snapshot trạng thái của "Lộc Xuân Đầu Năm" tại thời điểm người dùng bấm
    /// "🔁 Đổi loại game", ở CẢ HAI mức: (1) các trường đang nhập ở màn hình thiết lập
    /// (InitLocXuan) dù chưa bấm "Xong", và (2) nếu đã vào tới màn hình quay giải
    /// (LuckyDrawWindow), các số đã quay được của từng giải, để có thể tiếp tục quay
    /// đúng chỗ đang dở thay vì phải quay lại từ đầu.
    /// </summary>
    public class LocXuanDauNamSession
    {
        public string Title { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string LogoPath { get; set; } = "";
        public List<string> PrizePaths { get; set; } = new();

        /// <summary>True nếu đã từng vào tới màn hình quay giải (LuckyDrawWindow) ở lần chơi
        /// trước, tức là <see cref="PrizeNumbers"/> chứa kết quả quay dở cần khôi phục lại.</summary>
        public bool HasDrawStarted { get; set; } = false;

        /// <summary>Số đã quay được của từng giải (theo đúng thứ tự PrizePaths), "- - - -" nếu
        /// giải đó chưa quay tới. Chỉ có ý nghĩa khi <see cref="HasDrawStarted"/> = true.</summary>
        public List<string> PrizeNumbers { get; set; } = new();
    }

    /// <summary>
    /// Bộ nhớ đệm TRONG PHIÊN LÀM VIỆC (mất khi tắt hẳn ứng dụng) giữ lại trạng thái đang
    /// chơi dở của từng loại game, để khi người dùng bấm "🔁 Đổi loại game" qua lại giữa
    /// Loto Vui Xuân và Lộc Xuân Đầu Năm, mỗi game vẫn được nạp lại đúng trạng thái đang dở
    /// thay vì phải thiết lập/quay lại từ đầu mỗi lần chuyển qua chuyển lại.
    /// </summary>
    public static class GameSessionCache
    {
        public static LotoVuiXuanSession LotoSession { get; set; }
        public static LocXuanDauNamSession LocXuanSession { get; set; }

        /// <summary>Xoá sạch bộ nhớ đệm (ví dụ khi đăng xuất tài khoản).</summary>
        public static void ClearAll()
        {
            LotoSession = null;
            LocXuanSession = null;
        }
    }
}
