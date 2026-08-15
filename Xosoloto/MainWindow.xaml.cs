using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Xosoloto.Models;
using Xosoloto.Services;
// (Xosoloto.Services đã được import ở trên - dùng cho NumberRenderHelper, MonitorHelper)

namespace Xosoloto
{
    public class HistoryItem
    {
        public string Value { get; set; }
        public SolidColorBrush Color { get; set; }
    }

    public partial class MainWindow : Window
    {
        private List<HistoryItem> _historyNumbers = new();
        double fontSize = 40;
        private bool isFullScreen = false;
        public Dictionary<int, VongLoaiInfo> VongLoaiConfig { get; set; }
        public GameType CurrentGameType { get; private set; }
        public string TieuDe { get; private set; }
        private BangGiaWindow _bangGiaWindow;
        private string _currentUsername = "";

        /// <summary>
        /// Màn hình Trình chiếu (Show Window) dành cho khán giả/máy chiếu, tách biệt với
        /// màn hình Chỉnh sửa (MainWindow này) - giống mô hình Presenter View / Slide Show
        /// của PowerPoint. Mọi thay đổi trên MainWindow được đẩy sang cửa sổ này ngay lập tức.
        /// </summary>
        private LotoShowWindow _showWindow;

        /// <summary>
        /// True nếu trong quá trình khởi tạo, người dùng đã hủy (bấm Thoát/Hủy) ở một bước
        /// bắt buộc (chọn loại game, thiết lập Lộc Xuân...) và ứng dụng đã bắt đầu Shutdown().
        /// App.xaml.cs cần kiểm tra cờ này trước khi gọi Show(), vì gọi Show() trên một
        /// window đã bị đóng do Shutdown() sẽ ném InvalidOperationException.
        /// </summary>
        public bool IsShuttingDown { get; private set; } = false;

        public MainWindow() : this("Khách") { }

        public MainWindow(string username)
        {
            InitializeComponent();
            _currentUsername = string.IsNullOrWhiteSpace(username) ? "Khách" : username.Trim();

            this.Closed += (s, e) => CloseShowWindow();

            if (TryLoadSavedSettings())
            {
                if (CurrentGameType == GameType.LotoVuiXuan)
                {
                    mediaElement.Play();
                    SetNumber("");
                    EnsureShowWindow();
                }
                return;
            }

            SelectAndSetupGameLoop(
                exitAppOnCancel: true,
                onSelectionCancelled: () =>
                {
                    IsShuttingDown = true;
                    Application.Current.Shutdown();
                },
                onGameReady: () =>
                {
                    if (CurrentGameType == GameType.LotoVuiXuan)
                    {
                        mediaElement.Play();
                        EnsureShowWindow();
                        SyncShowWindowFull();
                    }
                    // LocXuanDauNam: InitLocXuan/LuckyDrawWindow tự lo giao diện của chúng.
                });
        }

        /// <summary>
        /// Hiện màn hình chọn loại game rồi thiết lập game tương ứng, LẶP LẠI nếu người dùng
        /// bấm "🔁 Đổi loại game" ngay trong lúc đang thiết lập/chơi Lộc Xuân Đầu Năm (ở
        /// InitLocXuan hoặc LuckyDrawWindow) — thay vì phải đóng hẳn ứng dụng hoặc bị kẹt
        /// lại không có đường quay ra màn hình chọn game.
        /// </summary>
        /// <param name="exitAppOnCancel">Có Shutdown() ứng dụng khi người dùng hủy các bước thiết lập bắt buộc hay không.</param>
        /// <param name="onSelectionCancelled">Gọi khi người dùng hủy ngay ở màn hình chọn loại game (không chọn gì cả).</param>
        /// <param name="onGameReady">Gọi sau khi một loại game đã được chọn và thiết lập xong (không bị "Đổi loại game" tiếp).</param>
        private void SelectAndSetupGameLoop(bool exitAppOnCancel, Action onSelectionCancelled, Action onGameReady)
        {
            while (true)
            {
                if (!ShowGameTypeSelection())
                {
                    onSelectionCancelled?.Invoke();
                    return;
                }

                if (CurrentGameType == GameType.LotoVuiXuan)
                {
                    // Nếu đã có trạng thái Loto Vui Xuân được lưu lại từ lần trước (do người
                    // dùng vừa "Đổi loại game" từ Loto Vui Xuân sang game khác), khôi phục lại
                    // đúng trạng thái đang dở đó thay vì bắt thiết lập lại từ đầu.
                    if (GameSessionCache.LotoSession != null)
                        RestoreLotoSessionFromCache();
                    else
                        ShowVongLoaiSetup(exitAppOnCancel: exitAppOnCancel);
                    onGameReady?.Invoke();
                    return;
                }

                if (CurrentGameType == GameType.LocXuanDauNam)
                {
                    bool wantsChangeGame = ShowLocXuan(exitAppOnCancel: exitAppOnCancel);
                    if (wantsChangeGame) continue; // quay lại chọn loại game khác
                    onGameReady?.Invoke();
                    return;
                }

                return;
            }
        }

        /// <summary>
        /// Cho phép người dùng thoát ra chọn lại loại game khác bất cứ lúc nào, kể cả khi
        /// đang ở màn hình được nạp tự động từ "cấu hình đã lưu" (trường hợp này trước đây
        /// không có đường quay lại màn hình chọn game). Bọc trong try/catch để nếu có lỗi
        /// bất ngờ xảy ra, người dùng thấy thông báo lỗi thay vì ứng dụng tự đóng/thoát mà
        /// không rõ lý do.
        /// </summary>
        private void ChangeGameButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirm = MessageBox.Show(
                    "Bạn có muốn đổi sang loại game khác không?\n" +
                    "Trạng thái đang chơi dở của game hiện tại sẽ được lưu lại.",
                    "Đổi loại game", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                // Lưu lại trạng thái ĐANG CHƠI DỞ của game hiện tại (nếu là Loto Vui Xuân) vào bộ
                // nhớ đệm trong phiên làm việc VÀ xuống đĩa, để nếu người dùng quay lại game này
                // sau (chuyển qua chuyển lại, hoặc mở lại ứng dụng), trạng thái sẽ được nạp lại
                // đúng chỗ đang dở thay vì mất hết.
                if (CurrentGameType == GameType.LotoVuiXuan)
                {
                    SaveLotoSessionToCache();
                    SaveCurrentSettings();
                }

                // Dừng video/nội dung của game hiện tại và ẩn màn hình chính đi TRƯỚC khi mở
                // màn hình chọn game mới (menu chính). Nếu không, MainWindow (với nội dung/video
                // của game cũ) vẫn còn hiển thị phía sau các cửa sổ chọn game / thiết lập game mới.
                mediaElement.Stop();
                this.Hide();
                CloseShowWindow();

                SelectAndSetupGameLoop(
                    exitAppOnCancel: false,
                    onSelectionCancelled: () =>
                    {
                        // Người dùng hủy chọn game mới -> hiện lại màn hình game hiện tại như cũ.
                        this.Show();
                        if (CurrentGameType == GameType.LotoVuiXuan)
                        {
                            mediaElement.Play();
                            EnsureShowWindow();
                            SyncShowWindowFull();
                        }
                    },
                    onGameReady: () =>
                    {
                        this.Show();
                        if (CurrentGameType == GameType.LotoVuiXuan)
                        {
                            mediaElement.Play();
                            EnsureShowWindow();
                            // Đẩy toàn bộ trạng thái hiện tại (dù là vừa thiết lập mới, hay vừa
                            // được khôi phục từ bộ nhớ đệm) sang màn hình Trình chiếu, thay vì luôn
                            // SetNumber("") - nếu không sẽ xoá mất số vừa được khôi phục.
                            SyncShowWindowFull();
                        }
                        // LocXuanDauNam: MainWindow chỉ hiện lại làm nền, InitLocXuan/LuckyDrawWindow
                        // đã tự lo xong toàn bộ giao diện của chúng.
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đổi loại game: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                // Đảm bảo MainWindow vẫn hiện lại cho người dùng thay vì bị kẹt ở màn hình ẩn.
                if (!this.IsVisible) this.Show();
            }
        }

        /// <summary>
        /// Kiểm tra tài khoản hiện tại đã có cấu hình (loại game, vòng loại, màu vé...) lưu từ
        /// trước hay chưa. Nếu có, hỏi người dùng có muốn dùng lại không, và nạp trực tiếp
        /// thay vì bắt cấu hình lại từ đầu.
        /// </summary>
        private bool TryLoadSavedSettings()
        {
            var saved = AccountService.LoadSettings(_currentUsername);
            if (saved == null || saved.VongLoaiList.Count == 0) return false;

            bool isLocXuan = saved.GameType == GameType.LocXuanDauNam.ToString();
            // Lộc Xuân Đầu Năm cần chọn lại ảnh/logo (đường dẫn file có thể không còn tồn tại
            // trên máy khác), nên chỉ khôi phục tự động cho Loto Vui Xuân.
            if (isLocXuan) return false;

            string gameTypeLabel = isLocXuan ? "Lộc Xuân Đầu Năm" : "Loto Vui Xuân";
            var result = MessageBox.Show(
                $"Tài khoản \"{_currentUsername}\" có cấu hình đã lưu trước đó " +
                $"(loại game: {gameTypeLabel}, {saved.VongLoaiList.Count} vòng loại).\n\n" +
                "Bạn có muốn dùng lại cấu hình này không?",
                "Cấu hình đã lưu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return false;

            CurrentGameType = GameType.LotoVuiXuan;
            this.Title = "Xổ Số Loto - Loto Vui Xuân";

            VongLoaiConfig = new Dictionary<int, VongLoaiInfo>();
            foreach (var dto in saved.VongLoaiList)
            {
                Color color;
                try { color = (Color)ColorConverter.ConvertFromString(dto.ColorHex); }
                catch { color = Colors.LightBlue; }

                VongLoaiConfig[dto.VongNumber] = new VongLoaiInfo
                {
                    // dto.Numbers có thể null nếu dữ liệu lưu trước đó bị thiếu/hỏng
                    Numbers = dto.Numbers ?? new List<int>(),
                    GiaVe = dto.GiaVe,
                    Color = new SolidColorBrush(color)
                };
            }

            VongLoaiComboBox.Items.Clear();
            foreach (var vong in VongLoaiConfig.Keys.OrderBy(k => k))
            {
                string displayText = string.Join(" ", VongLoaiConfig[vong].Numbers ?? new List<int>());
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = displayText,
                    Tag = new
                    {
                        VongNumber = vong,
                        Numbers = VongLoaiConfig[vong].Numbers,
                        Color = VongLoaiConfig[vong].Color
                    }
                };
                VongLoaiComboBox.Items.Add(item);
            }
            if (VongLoaiComboBox.Items.Count > 0)
                VongLoaiComboBox.SelectedIndex = 0;

            if (!string.IsNullOrEmpty(saved.SelectedMauVe))
            {
                foreach (ComboBoxItem item in MauVeComboBox.Items)
                {
                    if (item.Content.ToString() == saved.SelectedMauVe)
                    {
                        MauVeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>Lưu lại cấu hình hiện tại (vòng loại, giá vé, màu vé...) cho tài khoản đang đăng nhập.</summary>
        private void SaveCurrentSettings()
        {
            if (string.IsNullOrEmpty(_currentUsername) || VongLoaiConfig == null) return;

            var data = new AppSettingsData
            {
                GameType = CurrentGameType.ToString(),
                SelectedMauVe = (MauVeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                VongLoaiList = VongLoaiConfig.Select(kv => new VongLoaiSettingDto
                {
                    VongNumber = kv.Key,
                    Numbers = kv.Value.Numbers ?? new List<int>(),
                    GiaVe = kv.Value.GiaVe,
                    ColorHex = kv.Value.Color?.Color.ToString() ?? "#FFFFFF"
                }).ToList()
            };

            AccountService.SaveSettings(_currentUsername, data);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                $"Đăng xuất khỏi tài khoản \"{_currentUsername}\"?",
                "Đăng xuất", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            // Xoá bộ nhớ đệm trạng thái đang chơi dở (Loto Vui Xuân / Lộc Xuân Đầu Năm) của
            // tài khoản này, tránh việc tài khoản đăng nhập tiếp theo lại bị nạp nhầm trạng thái
            // của tài khoản trước đó.
            GameSessionCache.ClearAll();

            AccountService.ForgetUser();

            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true && !string.IsNullOrEmpty(loginWindow.LoggedInUsername))
            {
                var newMain = new MainWindow(loginWindow.LoggedInUsername);
                if (newMain.IsShuttingDown) return; // đã Shutdown() trong constructor, không Show() nữa

                Application.Current.MainWindow = newMain;
                newMain.Show();
                this.Close();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Mở (hoặc đưa lên foreground) màn hình Trình chiếu dành cho khán giả, tách biệt hoàn
        /// toàn với màn hình Chỉnh sửa (MainWindow này). Nếu máy có 2 màn hình trở lên, màn hình
        /// Trình chiếu sẽ tự động đặt full-screen ở màn hình phụ (giống PowerPoint: Presenter
        /// View ở màn hình chính, Slide Show ở máy chiếu/màn hình phụ).
        /// </summary>
        private void EnsureShowWindow()
        {
            if (_showWindow == null)
            {
                _showWindow = new LotoShowWindow();
                _showWindow.Show();
            }
            else if (!_showWindow.IsVisible)
            {
                // Cửa sổ đã bị người dùng ẩn đi (bấm ESC/"✕ Đóng" -> giờ Hide() thay vì Close(),
                // xem LotoShowWindow.xaml.cs) - hiện lại thay vì tạo mới.
                _showWindow.Show();
            }
            MonitorHelper.PlaceOnShowMonitor(_showWindow);
            _showWindow.SetMonitorStatus(MonitorHelper.DescribeMode());
            _showWindow.PlayVideo();
            SyncShowWindowFull();
            // Đưa focus bàn phím sang màn hình Trình chiếu để phím ESC đóng được ngay,
            // kể cả khi đang Topmost đè lên màn hình Chỉnh sửa (trường hợp chỉ có 1 màn hình).
            _showWindow.Activate();
        }

        private void CloseShowWindow()
        {
            if (_showWindow == null) return;
            _showWindow.StopVideo();
            _showWindow.Close();
            _showWindow = null;
        }

        /// <summary>Đẩy toàn bộ trạng thái hiện tại (vòng loại, màu vé, số vừa gọi, lịch sử) sang màn hình Trình chiếu.</summary>
        private void SyncShowWindowFull()
        {
            if (_showWindow == null) return;
            var typeface = new Typeface(
                CurrentNumberTextBlock.FontFamily,
                CurrentNumberTextBlock.FontStyle,
                CurrentNumberTextBlock.FontWeight,
                CurrentNumberTextBlock.FontStretch);
            _showWindow.SetVongLoai(VongLoaiTextBlock.Text, MauVeBorder.Background as SolidColorBrush);
            _showWindow.SetCurrentNumber(CurrentNumberTextBlock.Text, isNumberColorRed ? Brushes.Red : Brushes.Black);
            _showWindow.SetHistory(_historyNumbers, typeface, fontSize);
        }

        private bool ShowGameTypeSelection()
        {
            GameTypeSelectionWindow gameTypeWindow = new GameTypeSelectionWindow();
            if (gameTypeWindow.ShowDialog() == true)
            {
                CurrentGameType = gameTypeWindow.SelectedGameType;
                switch (CurrentGameType)
                {
                    case GameType.LotoVuiXuan:
                        this.Title = "Xổ Số Loto - Loto Vui Xuân";
                        //TieuDe = gameTypeWindow.TieuDe;
                        return true;
                    case GameType.LocXuanDauNam:
                        this.Title = "Xổ Số Loto - Lộc Xuân Đầu Năm";
                        //TieuDe = gameTypeWindow.TieuDe;
                        return true;
                }
                return true;
            }
            return false;
        }

        private void ShowVongLoaiSetup(bool exitAppOnCancel = true)
        {
            VongLoaiSetupWindow setupWindow = new VongLoaiSetupWindow();
            if (setupWindow.ShowDialog() == true)
            {
                VongLoaiConfig = setupWindow.VongLoaiData;
                // XÓA dòng: if (!string.IsNullOrEmpty(setupWindow.TieuDe)) TieuDe = setupWindow.TieuDe;
                VongLoaiComboBox.Items.Clear();
                foreach (var vong in VongLoaiConfig.Keys.OrderBy(k => k))
                {
                    string displayText = string.Join(" ", VongLoaiConfig[vong].Numbers ?? new List<int>());
                    ComboBoxItem item = new ComboBoxItem
                    {
                        Content = displayText,
                        Tag = new
                        {
                            VongNumber = vong,
                            Numbers = VongLoaiConfig[vong].Numbers,
                            Color = VongLoaiConfig[vong].Color
                        }
                    };
                    VongLoaiComboBox.Items.Add(item);
                }
                if (VongLoaiComboBox.Items.Count > 0)
                    VongLoaiComboBox.SelectedIndex = 0;

                // Đây là một lượt thiết lập MỚI (không phải khôi phục từ bộ nhớ đệm) -> làm
                // sạch số đang hiển thị/lịch sử của lượt chơi trước đó, đúng hành vi cũ.
                _historyNumbers.Clear();
                CurrentNumberTextBlock.Text = "";
                NumberPath.Data = Geometry.Empty;
                UpdateHistoryNumbers();

                SaveCurrentSettings();
            }
            else if (exitAppOnCancel)
            {
                IsShuttingDown = true;
                Application.Current.Shutdown();
            }
            // else: người dùng hủy khi đang đổi game giữa chừng -> giữ nguyên trạng thái hiện tại
        }

        /// <summary>
        /// Lưu lại trạng thái ĐANG CHƠI DỞ của Loto Vui Xuân (vòng loại đang chọn, màu vé, số
        /// vừa quay, lịch sử số đã quay...) vào bộ nhớ đệm trong phiên làm việc, gọi ngay trước
        /// khi rời khỏi game này (bấm "🔁 Đổi loại game") để có thể khôi phục lại đúng chỗ đang
        /// dở nếu người dùng quay lại chơi Loto Vui Xuân sau đó.
        /// </summary>
        private void SaveLotoSessionToCache()
        {
            if (VongLoaiConfig == null) return;

            GameSessionCache.LotoSession = new LotoVuiXuanSession
            {
                VongLoaiConfig = VongLoaiConfig,
                SelectedVongLoaiIndex = VongLoaiComboBox.SelectedIndex,
                SelectedMauVe = (MauVeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                HistoryNumbers = _historyNumbers.Select(h => new HistoryItem { Value = h.Value, Color = h.Color }).ToList(),
                CurrentNumber = CurrentNumberTextBlock.Text,
                IsNumberColorRed = isNumberColorRed
            };
        }

        /// <summary>
        /// Khôi phục lại trạng thái Loto Vui Xuân đã lưu ở <see cref="SaveLotoSessionToCache"/>
        /// (vòng loại, màu vé, số vừa quay, lịch sử...), gọi khi người dùng chọn lại "Loto Vui
        /// Xuân" trong lúc đã có sẵn bộ nhớ đệm cho game này - thay vì bắt thiết lập lại từ đầu.
        /// </summary>
        private void RestoreLotoSessionFromCache()
        {
            var session = GameSessionCache.LotoSession;
            if (session == null) return;

            VongLoaiConfig = session.VongLoaiConfig;

            VongLoaiComboBox.Items.Clear();
            foreach (var vong in VongLoaiConfig.Keys.OrderBy(k => k))
            {
                string displayText = string.Join(" ", VongLoaiConfig[vong].Numbers ?? new List<int>());
                ComboBoxItem item = new ComboBoxItem
                {
                    Content = displayText,
                    Tag = new
                    {
                        VongNumber = vong,
                        Numbers = VongLoaiConfig[vong].Numbers,
                        Color = VongLoaiConfig[vong].Color
                    }
                };
                VongLoaiComboBox.Items.Add(item);
            }
            if (VongLoaiComboBox.Items.Count > 0)
            {
                int idx = session.SelectedVongLoaiIndex;
                VongLoaiComboBox.SelectedIndex = (idx >= 0 && idx < VongLoaiComboBox.Items.Count) ? idx : 0;
            }

            if (!string.IsNullOrEmpty(session.SelectedMauVe))
            {
                foreach (ComboBoxItem item in MauVeComboBox.Items)
                {
                    if (item.Content.ToString() == session.SelectedMauVe)
                    {
                        MauVeComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            isNumberColorRed = session.IsNumberColorRed;
            CurrentNumberTextBlock.Foreground = isNumberColorRed ? Brushes.Red : Brushes.Black;
            NumberPath.Fill = isNumberColorRed ? Brushes.Red : Brushes.Black;

            _historyNumbers = session.HistoryNumbers?
                .Select(h => new HistoryItem { Value = h.Value, Color = h.Color }).ToList()
                ?? new List<HistoryItem>();
            CurrentNumberTextBlock.Text = session.CurrentNumber ?? "";
            UpdateHistoryNumbers();
            SetNumber(session.CurrentNumber ?? "");
        }

        /// <summary>
        /// Mở màn hình thiết lập/chơi Lộc Xuân Đầu Năm.
        /// Trả về true nếu người dùng bấm "🔁 Đổi loại game" (từ InitLocXuan hoặc
        /// LuckyDrawWindow bên trong) để quay lại màn hình chọn loại game - khi đó
        /// SelectAndSetupGameLoop() sẽ tự lặp lại và mở màn hình chọn loại game.
        /// </summary>
        private bool ShowLocXuan(bool exitAppOnCancel = true)
        {
            InitLocXuan setupWindow = new InitLocXuan(_currentUsername);
            bool? dialogResult = setupWindow.ShowDialog();

            // QUAN TRỌNG: kiểm tra cờ ChangeGameRequested TRƯỚC, không phụ thuộc vào việc
            // dialogResult có đúng bằng "true" hay không. Cờ này được set trực tiếp ngay khi
            // người dùng bấm "Đổi loại game" (kể cả bấm từ bên trong LuckyDrawWindow, lồng 2
            // cấp cửa sổ bên trong InitLocXuan), nên đáng tin cậy hơn giá trị trả về của
            // ShowDialog() - giá trị này có thể không phải "true" tuỳ theo cách cửa sổ con được
            // đóng lại qua nhiều tầng lồng nhau. Trước đây code chỉ tin vào "dialogResult ==
            // true" nên có trường hợp bấm "Đổi loại game" nhưng bị hiểu nhầm thành "người dùng
            // hủy" và gọi Shutdown() thoát hẳn ứng dụng thay vì quay lại màn hình chọn game.
            if (setupWindow.ChangeGameRequested)
            {
                return true;
            }

            if (dialogResult == true)
            {
                return false; // hoàn tất thiết lập/chơi Lộc Xuân bình thường, không đổi game
            }

            if (exitAppOnCancel)
            {
                IsShuttingDown = true;
                Application.Current.Shutdown();
            }
            // else: người dùng hủy khi đang đổi game giữa chừng -> giữ nguyên trạng thái hiện tại
            return false;
        }

        private void BangGiaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_bangGiaWindow == null)
                _bangGiaWindow = new BangGiaWindow(this);

            _bangGiaWindow.LoadData(VongLoaiConfig); // XÓA tham số TieuDe

            this.Hide();
            _bangGiaWindow.Show();
            _bangGiaWindow.Activate();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                if (isFullScreen) ExitFullScreen();
                else EnterFullScreen();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && isFullScreen)
            {
                ExitFullScreen();
                e.Handled = true;
            }
        }

        private void EnterFullScreen()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            Topmost = true;
            isFullScreen = true;
        }

        private void ExitFullScreen()
        {
            Topmost = false;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
            isFullScreen = false;
        }

        private void NumberInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            string number = tb.Text;
            switch (tb.Tag?.ToString())
            {
                case "Add": HandleAddNumber(number); break;
                case "Delete": HandleRemoveNumber(number); break;
                case "Color": HandleBingoNumber(number); break;
            }
            tb.Clear();
            e.Handled = true;
        }

        private void AddNumberToHistory(string number)
        {
            _historyNumbers.Add(new HistoryItem { Value = number, Color = Brushes.Red });
            UpdateHistoryNumbers();
        }

        private void RemoveNumber(string number)
        {
            var item = _historyNumbers.FirstOrDefault(x => x.Value == number);
            if (item == null) { MessageBox.Show($"Không tìm thấy số {number}"); return; }
            _historyNumbers.Remove(item);
            UpdateHistoryNumbers();
        }

        private void ChangeNumberColor(string number)
        {
            var item = _historyNumbers.FirstOrDefault(x => x.Value == number);
            if (item == null) { MessageBox.Show($"Không tìm thấy số {number}"); return; }
            item.Color = Brushes.Green;
            UpdateHistoryNumbers();
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            _historyNumbers.Clear();
            CurrentNumberTextBlock.Text = "";
            NumberPath.Data = Geometry.Empty;
            UpdateHistoryNumbers();
            _showWindow?.ClearAll();
            _showWindow?.SetCurrentNumber("", isNumberColorRed ? Brushes.Red : Brushes.Black);
        }

        private void ClearBingoButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _historyNumbers)
                item.Color = Brushes.Red;
            UpdateHistoryNumbers();
        }

        private void UpdateHistoryNumbers()
        {
            var typeface = new Typeface(
                CurrentNumberTextBlock.FontFamily,
                CurrentNumberTextBlock.FontStyle,
                CurrentNumberTextBlock.FontWeight,
                CurrentNumberTextBlock.FontStretch);

            NumberRenderHelper.RenderHistory(HistoryNumberContainer, _historyNumbers, typeface, fontSize, HistoryNumberContainer.ActualWidth);

            // Đẩy real-time sang màn hình Trình chiếu (nếu đang mở).
            _showWindow?.SetHistory(_historyNumbers, typeface, fontSize);
        }

        private void VongLoaiComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VongLoaiComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string displayText = selectedItem.Content.ToString();
                VongLoaiTextBlock.Text = string.Join("  ", displayText.Split(' '));
                try
                {
                    dynamic tagData = selectedItem.Tag;
                    if (tagData != null && tagData.Color != null)
                    {
                        SolidColorBrush selectedColor = tagData.Color;
                        MauVeBorder.Background = selectedColor;
                        SetMauVeComboBoxByColor(selectedColor);
                    }
                }
                catch { }
                _showWindow?.SetVongLoai(VongLoaiTextBlock.Text, MauVeBorder.Background as SolidColorBrush);
            }
        }

        private void SetMauVeComboBoxByColor(SolidColorBrush targetColor)
        {
            if (targetColor == null) return;
            string closestColorName = GetClosestColorName(targetColor.Color);
            foreach (ComboBoxItem item in MauVeComboBox.Items)
            {
                if (item.Content.ToString() == closestColorName)
                {
                    MauVeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private string GetClosestColorName(Color color)
        {
            if (VongLoaiSetupWindow.AvailableColors == null || VongLoaiSetupWindow.AvailableColors.Count == 0)
                return "";
            string closestName = VongLoaiSetupWindow.AvailableColors[0].Name;
            double minDistance = double.MaxValue;
            foreach (var option in VongLoaiSetupWindow.AvailableColors)
            {
                double distance = GetColorDistance(color, option.Color.Color);
                if (distance < minDistance) { minDistance = distance; closestName = option.Name; }
            }
            return closestName;
        }

        private double GetColorDistance(Color c1, Color c2)
        {
            int rDiff = c1.R - c2.R, gDiff = c1.G - c2.G, bDiff = c1.B - c2.B;
            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        private void MauVeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MauVeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var match = VongLoaiSetupWindow.AvailableColors?
                    .FirstOrDefault(c => c.Name == selectedItem.Content.ToString());
                if (match != null)
                    MauVeBorder.Background = match.Color;

                SaveCurrentSettings();
                _showWindow?.SetVongLoai(VongLoaiTextBlock.Text, MauVeBorder.Background as SolidColorBrush);
            }
        }

        private void SetNumber(string number)
        {
            var foreground = isNumberColorRed ? Brushes.Red : Brushes.Black;
            NumberRenderHelper.RenderCurrentNumber(NumberPath, CurrentNumberTextBlock, number, foreground);

            // Đẩy real-time sang màn hình Trình chiếu (nếu đang mở).
            _showWindow?.SetCurrentNumber(number, foreground);
        }

        private void HandleAddNumber(string number)
        {
            if (!Regex.IsMatch(number, @"^\d{2}$"))
            {
                MessageBox.Show("Chỉ được nhập đúng 2 chữ số!");
                return;
            }
            int value = int.Parse(number);
            if (value > 60) { MessageBox.Show("Số phải nhỏ hơn hoặc bằng 60"); return; }
            CurrentNumberTextBlock.Text = number;
            SetNumber(number);
            AddNumberToHistory(number);
        }

        private void HandleRemoveNumber(string number)
        {
            number = number.Trim();
            if (string.IsNullOrEmpty(number))
            {
                MessageBox.Show("Vui lòng nhập số để xóa.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var item = _historyNumbers.FirstOrDefault(x => x.Value == number);
            if (item != null)
            {
                _historyNumbers.Remove(item);
                UpdateHistoryNumbers();
                var lastItem = _historyNumbers.LastOrDefault();
                if (lastItem != null) { CurrentNumberTextBlock.Text = lastItem.Value; SetNumber(lastItem.Value); }
                else { CurrentNumberTextBlock.Text = ""; NumberPath.Data = Geometry.Empty; }
            }
            else
            {
                MessageBox.Show($"Không tìm thấy số {number} trong danh sách", "Thông báo");
            }
        }

        private void RemoveSpecificNumberButton_Click(object sender, RoutedEventArgs e)
        {
            HandleRemoveNumber(RemoveNumberInput.Text);
            RemoveNumberInput.Text = "";
        }

        private void HandleBingoNumber(string number)
        {
            number = number.Trim();
            if (string.IsNullOrEmpty(number))
            {
                MessageBox.Show("Vui lòng nhập một số.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var item = _historyNumbers.FirstOrDefault(x => x.Value == number);
            if (item != null) { item.Color = Brushes.Green; UpdateHistoryNumbers(); }
            else MessageBox.Show($"Số '{number}' không tồn tại trong danh sách.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void BingoInputButton_Click(object sender, RoutedEventArgs e)
        {
            HandleBingoNumber(BingoInputTextBox.Text);
            BingoInputTextBox.Text = "";
        }

        private void SoButton_Click(object sender, RoutedEventArgs e)
        {
            HandleAddNumber(NumberInputTextBox.Text);
            NumberInputTextBox.Clear();
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaElement.Position = TimeSpan.FromMilliseconds(1);
            mediaElement.Play();
        }

        private void MediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(e.ErrorException.Message);
        }

        private bool isNumberColorRed = true;

        private void ToggleNumberColorButton_Click(object sender, RoutedEventArgs e)
        {
            isNumberColorRed = !isNumberColorRed;
            if (isNumberColorRed)
            {
                CurrentNumberTextBlock.Foreground = Brushes.Red;
                NumberPath.Fill = Brushes.Red;
            }
            else
            {
                CurrentNumberTextBlock.Foreground = Brushes.Black;
                NumberPath.Fill = Brushes.Black;
            }
            ChangeColorRecursive(HistoryNumberContainer);

            // Đồng bộ màu số hiện tại + lịch sử sang màn hình Trình chiếu.
            _showWindow?.SetCurrentNumber(CurrentNumberTextBlock.Text, isNumberColorRed ? Brushes.Red : Brushes.Black);
            foreach (var item in _historyNumbers)
                if (item.Color != Brushes.Green) item.Color = isNumberColorRed ? Brushes.Red : Brushes.Black;
            UpdateHistoryNumbers();
        }

        private void ChangeColorRecursive(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBlock tb) tb.Foreground = isNumberColorRed ? Brushes.Red : Brushes.Black;
                else if (child is Path path) path.Fill = isNumberColorRed ? Brushes.Red : Brushes.Black;
                ChangeColorRecursive(child);
            }
        }
    }
}