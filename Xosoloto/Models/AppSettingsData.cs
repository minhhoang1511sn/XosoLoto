using System;
using System.Collections.Generic;

namespace Xosoloto.Models
{
    /// <summary>
    /// Một "vòng loại" đã cấu hình, ở dạng có thể lưu ra JSON
    /// (SolidColorBrush được quy đổi thành chuỗi mã màu hex).
    /// </summary>
    public class VongLoaiSettingDto
    {
        public int VongNumber { get; set; }
        public List<int> Numbers { get; set; } = new();
        public decimal GiaVe { get; set; }
        public string ColorHex { get; set; } = "#FF1E90FF";
    }

    /// <summary>
    /// Cấu hình đã thiết lập cho màn hình "Lộc Xuân Đầu Năm": tiêu đề, logo, ảnh nền,
    /// danh sách ảnh giải thưởng (số lượng ĐỘNG, không còn cố định 5) và số vòng/giải
    /// hiện đang được cấu hình. Được lưu ra JSON cùng với AppSettingsData để tự nạp lại
    /// ở lần mở sau (đường dẫn ảnh có thể không còn tồn tại nếu đổi máy).
    /// </summary>
    public class LocXuanSettingDto
    {
        public string Title { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string LogoPath { get; set; } = string.Empty;
        public List<string> PrizePaths { get; set; } = new();

        /// <summary>Số vòng/giải hiện tại (động, người dùng có thể tăng/giảm ở màn hình thiết lập).</summary>
        public int SoVong { get; set; } = 5;
    }

    /// <summary>
    /// Toàn bộ cấu hình mà một tài khoản đã thiết lập: loại game, các vòng loại,
    /// màu vé đang chọn, cấu hình Lộc Xuân... Được lưu ra file JSON riêng cho từng
    /// tài khoản (%AppData%\Xosoloto\settings_{username}.json) và tự nạp lại ở lần
    /// đăng nhập sau.
    /// </summary>
    public class AppSettingsData
    {
        public string GameType { get; set; } = "LotoVuiXuan";
        public List<VongLoaiSettingDto> VongLoaiList { get; set; } = new();
        public string SelectedMauVe { get; set; } = string.Empty;

        /// <summary>Cấu hình màn hình Lộc Xuân Đầu Năm (null nếu tài khoản chưa thiết lập bao giờ).</summary>
        public LocXuanSettingDto? LocXuan { get; set; }
        public DateTime LastSaved { get; set; } = DateTime.Now;
    }
}
