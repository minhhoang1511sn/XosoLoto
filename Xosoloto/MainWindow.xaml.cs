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

        public MainWindow() : this("Khách") { }

        public MainWindow(string username)
        {
            InitializeComponent();
            _currentUsername = string.IsNullOrWhiteSpace(username) ? "Khách" : username.Trim();

            if (TryLoadSavedSettings())
            {
                if (CurrentGameType == GameType.LotoVuiXuan)
                {
                    mediaElement.Play();
                    SetNumber("");
                }
                return;
            }

            if (!ShowGameTypeSelection())
            {
                Application.Current.Shutdown();
                return;
            }

            if (CurrentGameType == GameType.LotoVuiXuan)
            {
                ShowVongLoaiSetup();
                mediaElement.Play();
                SetNumber("");
            }
            if (CurrentGameType == GameType.LocXuanDauNam)
            {
                ShowLocXuan();
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
                    Numbers = dto.Numbers,
                    GiaVe = dto.GiaVe,
                    Color = new SolidColorBrush(color)
                };
            }

            VongLoaiComboBox.Items.Clear();
            foreach (var vong in VongLoaiConfig.Keys.OrderBy(k => k))
            {
                string displayText = string.Join(" ", VongLoaiConfig[vong].Numbers);
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
                    Numbers = kv.Value.Numbers,
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

            AccountService.ForgetUser();

            var loginWindow = new LoginWindow();
            if (loginWindow.ShowDialog() == true && !string.IsNullOrEmpty(loginWindow.LoggedInUsername))
            {
                var newMain = new MainWindow(loginWindow.LoggedInUsername);
                Application.Current.MainWindow = newMain;
                newMain.Show();
                this.Close();
            }
            else
            {
                Application.Current.Shutdown();
            }
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

        private void ShowVongLoaiSetup()
        {
            VongLoaiSetupWindow setupWindow = new VongLoaiSetupWindow();
            if (setupWindow.ShowDialog() == true)
            {
                VongLoaiConfig = setupWindow.VongLoaiData;
                // XÓA dòng: if (!string.IsNullOrEmpty(setupWindow.TieuDe)) TieuDe = setupWindow.TieuDe;
                VongLoaiComboBox.Items.Clear();
                foreach (var vong in VongLoaiConfig.Keys.OrderBy(k => k))
                {
                    string displayText = string.Join(" ", VongLoaiConfig[vong].Numbers);
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

                SaveCurrentSettings();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }


        private void ShowLocXuan()
        {
            InitLocXuan setupWindow = new InitLocXuan();
            if (setupWindow.ShowDialog() == true)
            {
            }
            else
            {
                Application.Current.Shutdown();
            }
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
        }

        private void ClearBingoButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _historyNumbers)
                item.Color = Brushes.Red;
            UpdateHistoryNumbers();
        }

        private void UpdateHistoryNumbers()
        {
            HistoryNumberContainer.Children.Clear();
            double x = 0, y = -5;
            double maxWidth = HistoryNumberContainer.ActualWidth;
            var typeface = new Typeface(
                CurrentNumberTextBlock.FontFamily,
                CurrentNumberTextBlock.FontStyle,
                CurrentNumberTextBlock.FontWeight,
                CurrentNumberTextBlock.FontStretch);
            foreach (var item in _historyNumbers.AsEnumerable().Reverse())
            {
                var ft = new FormattedText(item.Value, CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, typeface, fontSize, item.Color, 96);
                if (x + ft.Width > maxWidth) { x = 0; y += 40; }
                var path = new Path
                {
                    Data = ft.BuildGeometry(new Point(x, y)),
                    Fill = item.Color,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                HistoryNumberContainer.Children.Add(path);
                x += ft.Width + 10;
            }
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
                var match = VongLoaiSetupWindow.AvailableColors
                    .FirstOrDefault(c => c.Name == selectedItem.Content.ToString());
                if (match != null)
                    MauVeBorder.Background = match.Color;

                SaveCurrentSettings();
            }
        }

        private void SetNumber(string number)
        {
            var typeface = new Typeface(
                CurrentNumberTextBlock.FontFamily,
                CurrentNumberTextBlock.FontStyle,
                CurrentNumberTextBlock.FontWeight,
                CurrentNumberTextBlock.FontStretch);
            var ft = new FormattedText(number, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, CurrentNumberTextBlock.FontSize,
                Brushes.Red, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            NumberPath.Data = ft.BuildGeometry(new Point(0, -10));
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