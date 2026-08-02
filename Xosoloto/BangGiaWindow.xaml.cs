using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Xosoloto
{
    public partial class BangGiaWindow : Window
    {
        private bool isFullScreen = false;
        private Dictionary<int, VongLoaiInfo> _vongLoaiConfig;
        private MainWindow _mainWindow;
        private bool _isCellGreen = true; // mặc định xanh lá
        private double _currentPhanTram = 0;
        public Dictionary<int, VongLoaiInfo> VongLoaiConfig { get; set; }
        public BangGiaWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;

            // Dùng absolute path thay vì relative URI
            string videoPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Images", "media1.mov");

            if (System.IO.File.Exists(videoPath))
                mediaElement.Source = new Uri(videoPath, UriKind.Absolute);
            else
                MessageBox.Show("Không tìm thấy: " + videoPath); // debug

            this.Loaded += (s, e) =>
            {
                mediaElement.Play();
            };
        }

        private void ToggleCellColorButton_Click(object sender, RoutedEventArgs e)
        {
            _isCellGreen = !_isCellGreen;
            if (_isCellGreen)
            {
                ToggleCellColorButton.Background = new SolidColorBrush(Color.FromRgb(0x22, 0x99, 0x00));
                ToggleCellColorButton.Content = "🟢 Xanh lá";
            }
            else
            {
                ToggleCellColorButton.Background = new SolidColorBrush(Color.FromRgb(0xCC, 0x00, 0x00));
                ToggleCellColorButton.Content = "🔴 Đỏ";
            }
            // Vẽ lại với màu mới
            UpdateProgressBar(_currentPhanTram);
        }

        private void UpdateProgressBar(double phanTram)
        {
            _currentPhanTram = phanTram;
            int totalCells = 12;
            int filledCells = (int)Math.Round(phanTram / 100.0 * totalCells);

            var cells = new[] { Cell01, Cell02, Cell03, Cell04, Cell05, Cell06,
                                 Cell07, Cell08, Cell09, Cell10, Cell11, Cell12 };

            for (int i = 0; i < totalCells; i++)
            {
                if (i < filledCells)
                {
                    var brush = new LinearGradientBrush();
                    brush.StartPoint = new Point(0, 0);
                    brush.EndPoint = new Point(0, 1);

                    if (_isCellGreen)
                    {
                        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x66, 0xDD, 0x22), 0.0));
                        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x22, 0x99, 0x00), 0.5));
                        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0x44, 0xBB, 0x11), 1.0));
                    }
                    else
                    {
                        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xFF, 0x55, 0x22), 0.0));
                        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xCC, 0x00, 0x00), 0.5));
                        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xEE, 0x22, 0x00), 1.0));
                    }
                    cells[i].Background = brush;
                }
                else
                {
                    cells[i].Background = new SolidColorBrush(Color.FromRgb(0xEE, 0xFA, 0xEE));
                }
            }

            PhanTramTextBlock.Text = $"{phanTram:0}%";
            PhanTramTextBlock.Foreground = phanTram >= 100
                ? new SolidColorBrush(Color.FromRgb(0, 170, 68))
                : new SolidColorBrush(Color.FromRgb(255, 140, 0));
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaElement.Position = TimeSpan.FromMilliseconds(1);
            mediaElement.Play();
        }

        public void LoadData(Dictionary<int, VongLoaiInfo> vongLoaiConfig)
        {
            _vongLoaiConfig = vongLoaiConfig;
            VongLoaiSelectComboBox.Items.Clear();
            foreach (var vong in vongLoaiConfig.Keys.OrderBy(k => k))
            {
                string displayText = string.Join(" ", vongLoaiConfig[vong].Numbers ?? new List<int>()); // ← giống MainWindow
                var item = new ComboBoxItem
                {
                    Content = displayText,
                    Tag = vong
                };
                VongLoaiSelectComboBox.Items.Add(item);
            }
            if (VongLoaiSelectComboBox.Items.Count > 0)
                VongLoaiSelectComboBox.SelectedIndex = 0;
        }
        private void VongLoaiSelectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_vongLoaiConfig == null) return;
            if (VongLoaiSelectComboBox.SelectedItem is ComboBoxItem selected && selected.Tag is int vongSo)
            {
                var info = _vongLoaiConfig[vongSo];
                VongSoTextBlock.Text = string.Join(" ", info.Numbers ?? new List<int>());
                GiaTienTextBlock.Text = info.GiaVe.ToString("N0").Replace(",", ".");
                MauVeDisplayBorder.Background = info.Color;
            }
        }

        private void PhanTramInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { ApplyPhanTram(); e.Handled = true; }
        }

        private void PhanTramOK_Click(object sender, RoutedEventArgs e)
        {
            ApplyPhanTram();
        }

        private void ApplyPhanTram()
        {
            string text = PhanTramInputTextBox.Text.Trim().Replace("%", "");
            if (double.TryParse(text, out double phanTram))
            {
                phanTram = Math.Clamp(phanTram, 0, 100);
                UpdateProgressBar(phanTram);
            }
        }

        public void SetPhanTram(int phanTram) => UpdateProgressBar(Math.Clamp(phanTram, 0, 100));

        public void SetMauVe(SolidColorBrush color) => MauVeDisplayBorder.Background = color;

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow?.Show();
            _mainWindow?.Activate();
            this.Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                if (isFullScreen) ExitFullScreen(); else EnterFullScreen();
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
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
            isFullScreen = false;
        }
    }
}