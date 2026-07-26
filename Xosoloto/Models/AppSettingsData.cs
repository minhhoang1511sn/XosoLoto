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
    /// Toàn bộ cấu hình mà một tài khoản đã thiết lập: loại game, các vòng loại,
    /// màu vé đang chọn... Được lưu ra file JSON riêng cho từng tài khoản
    /// (%AppData%\Xosoloto\settings_{username}.json) và tự nạp lại ở lần đăng nhập sau.
    /// </summary>
    public class AppSettingsData
    {
        public string GameType { get; set; } = "LotoVuiXuan";
        public List<VongLoaiSettingDto> VongLoaiList { get; set; } = new();
        public string SelectedMauVe { get; set; } = string.Empty;
        public DateTime LastSaved { get; set; } = DateTime.Now;
    }
}
