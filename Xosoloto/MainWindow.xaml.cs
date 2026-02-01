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

        public MainWindow()
        {
            InitializeComponent();

            // Hiển thị cửa sổ chọn loại game trước
            if (!ShowGameTypeSelection())
            {
                Application.Current.Shutdown();
                return;
            }

            // CHỈ hiển thị cửa sổ setup vòng loại nếu là LotoVuiXuan
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
                        return true; // Tiếp tục với LotoVuiXuan

                    case GameType.LocXuanDauNam:
                        this.Title = "Xổ Số Loto - Lộc Xuân Đầu Năm";
                        
                        return true; 
                }
                return true;
            }
            return false; // User cancel
        }

        private void ShowVongLoaiSetup()
        {
            VongLoaiSetupWindow setupWindow = new VongLoaiSetupWindow();
            if (setupWindow.ShowDialog() == true)
            {
                VongLoaiConfig = setupWindow.VongLoaiData;
                // Load vòng loại vào ComboBox
                VongLoaiComboBox.Items.Clear();
                foreach (var vong in VongLoaiConfig.Keys.OrderBy(k => k))
                {
                    // Hiển thị 6 số thay vì chỉ số vòng
                    string displayText = string.Join(" ", VongLoaiConfig[vong].Numbers);
                    ComboBoxItem item = new ComboBoxItem
                    {
                        Content = displayText,  // Hiển thị: "1 2 3 4 5 6"
                        Tag = new
                        {
                            VongNumber = vong,
                            Numbers = VongLoaiConfig[vong].Numbers,
                            Color = VongLoaiConfig[vong].Color
                        }
                    };
                    VongLoaiComboBox.Items.Add(item);
                }
                // Chọn vòng đầu tiên
                if (VongLoaiComboBox.Items.Count > 0)
                {
                    VongLoaiComboBox.SelectedIndex = 0;
                }
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

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // F11: bật / tắt fullscreen
            if (e.Key == Key.F11)
            {
                if (isFullScreen)
                    ExitFullScreen();
                else
                    EnterFullScreen();
                e.Handled = true;
            }
            // ESC: chỉ thoát fullscreen
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

        // ================= ENTER DÙNG CHUNG =================
        private void NumberInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            string number = tb.Text;
            switch (tb.Tag?.ToString())
            {
                case "Add":
                    HandleAddNumber(number);
                    break;
                case "Delete":
                    HandleRemoveNumber(number);
                    break;
                case "Color":
                    HandleBingoNumber(number);
                    break;
            }
            tb.Clear();
            e.Handled = true;
        }

        // ================= LOGIC =================
        private void AddNumberToHistory(string number)
        {
            _historyNumbers.Add(new HistoryItem
            {
                Value = number,
                Color = Brushes.Red
            });
            UpdateHistoryNumbers();
        }

        private void RemoveNumber(string number)
        {
            var item = _historyNumbers.FirstOrDefault(x => x.Value == number);
            if (item == null)
            {
                MessageBox.Show($"Không tìm thấy số {number}");
                return;
            }
            _historyNumbers.Remove(item);
            UpdateHistoryNumbers();
        }

        private void ChangeNumberColor(string number)
        {
            var item = _historyNumbers.FirstOrDefault(x => x.Value == number);
            if (item == null)
            {
                MessageBox.Show($"Không tìm thấy số {number}");
                return;
            }
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
                CurrentNumberTextBlock.FontStretch
            );
            foreach (var item in _historyNumbers.AsEnumerable().Reverse())
            {
                var ft = new FormattedText(
                    item.Value,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    item.Color,
                    96);
                if (x + ft.Width > maxWidth)
                {
                    x = 0;
                    y += 40;
                }
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
            Color color = targetColor.Color;
            string closestColorName = GetClosestColorName(color);
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
            var colorMap = new Dictionary<string, Color>
            {
                { "Xanh", Colors.Blue },
                { "Đỏ", Colors.Red },
                { "Vàng", Colors.Yellow },
                { "Xanh lá", Colors.Green }
            };
            string closestName = "Đỏ";
            double minDistance = double.MaxValue;
            foreach (var kvp in colorMap)
            {
                double distance = GetColorDistance(color, kvp.Value);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestName = kvp.Key;
                }
            }
            return closestName;
        }

        private double GetColorDistance(Color c1, Color c2)
        {
            int rDiff = c1.R - c2.R;
            int gDiff = c1.G - c2.G;
            int bDiff = c1.B - c2.B;
            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff);
        }

        private void MauVeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MauVeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string mauVe = selectedItem.Content.ToString();
                switch (mauVe)
                {
                    case "Xanh":
                        MauVeBorder.Background = Brushes.Blue;
                        break;
                    case "Đỏ":
                        MauVeBorder.Background = Brushes.Red;
                        break;
                    case "Vàng":
                        MauVeBorder.Background = Brushes.Yellow;
                        break;
                    case "Xanh lá":
                        MauVeBorder.Background = Brushes.Green;
                        break;
                }
            }
        }

        private void SetNumber(string number)
        {
            var typeface = new Typeface(
                CurrentNumberTextBlock.FontFamily,
                CurrentNumberTextBlock.FontStyle,
                CurrentNumberTextBlock.FontWeight,
                CurrentNumberTextBlock.FontStretch);
            var ft = new FormattedText(
                number,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                CurrentNumberTextBlock.FontSize,
                Brushes.Red,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
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
            if (value > 60)
            {
                MessageBox.Show("Số phải nhỏ hơn hoặc bằng 60");
                return;
            }
            CurrentNumberTextBlock.Text = number;
            SetNumber(number);
            AddNumberToHistory(number);
        }

        private void HandleRemoveNumber(string number)
        {
            number = number.Trim();
            if (string.IsNullOrEmpty(number))
            {
                MessageBox.Show("Vui lòng nhập số để xóa.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var item = _historyNumbers.FirstOrDefault(x => x.Value == number);
            if (item != null)
            {
                _historyNumbers.Remove(item);
                UpdateHistoryNumbers();
                var lastItem = _historyNumbers.LastOrDefault();
                if (lastItem != null)
                {
                    CurrentNumberTextBlock.Text = lastItem.Value;
                    SetNumber(lastItem.Value);
                }
                else
                {
                    CurrentNumberTextBlock.Text = "";
                    NumberPath.Data = Geometry.Empty;
                }
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
                MessageBox.Show("Vui lòng nhập một số.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var item = _historyNumbers.FirstOrDefault(x => x.Value == number);
            if (item != null)
            {
                item.Color = Brushes.Green;
                UpdateHistoryNumbers();
            }
            else
            {
                MessageBox.Show($"Số '{number}' không tồn tại trong danh sách.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                if (child is TextBlock textBlock)
                {
                    textBlock.Foreground = isNumberColorRed ? Brushes.Red : Brushes.Black;
                }
                else if (child is Path path)
                {
                    path.Fill = isNumberColorRed ? Brushes.Red : Brushes.Black;
                }
                ChangeColorRecursive(child);
            }
        }
    }
}
