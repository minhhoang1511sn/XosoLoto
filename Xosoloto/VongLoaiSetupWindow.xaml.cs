using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Xosoloto
{
    public class ColorOption
    {
        public string Name { get; set; }
        public SolidColorBrush Color { get; set; }
    }

    public class VongLoaiInfo
    {
        public List<int> Numbers { get; set; }
        public decimal GiaVe { get; set; }
        public SolidColorBrush Color { get; set; }
    }

    public partial class VongLoaiSetupWindow : Window
    {
        public Dictionary<int, VongLoaiInfo> VongLoaiData { get; private set; }

        // Public static để các class khác (MainWindow) dùng chung
        public static List<ColorOption> AvailableColors { get; private set; }

        private int vongCount = 1;

        public VongLoaiSetupWindow()
        {
            InitializeComponent();
            VongLoaiData = new Dictionary<int, VongLoaiInfo>();
            ExcelPackage.License.SetNonCommercialPersonal("Hoang");

            InitializeColors();
            PopulateColorComboBox(Vong1ColorComboBox);
        }

        private void InitializeColors()
        {
            AvailableColors = new List<ColorOption>
            {
               new ColorOption { Name = "Hồng đậm", Color = new SolidColorBrush(Color.FromRgb(199, 21, 133)) },
               new ColorOption { Name = "Xanh", Color = new SolidColorBrush(Color.FromRgb(30, 144, 255)) },
                new ColorOption { Name = "Xanh lá", Color = new SolidColorBrush(Color.FromRgb(60,  179, 113)) },
                new ColorOption { Name = "Vàng",    Color = new SolidColorBrush(Color.FromRgb(255, 215, 0))   },
            };
        }

        private void PopulateColorComboBox(ComboBox comboBox)
        {
            comboBox.ItemsSource = AvailableColors;
            comboBox.SelectedIndex = 0;
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void AddVong_Click(object sender, RoutedEventArgs e)
        {
            vongCount++;

            Grid grid = new Grid { Margin = new Thickness(0, 3, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(155) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });

            TextBlock label = new TextBlock
            {
                Text = $"Vòng {vongCount}:",
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(label, 0);

            TextBox soVongTextBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(soVongTextBox, 1);

            TextBox giaVeTextBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(giaVeTextBox, 2);

            ComboBox colorComboBox = new ComboBox
            {
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0),
                ItemTemplate = Vong1ColorComboBox.ItemTemplate
            };
            PopulateColorComboBox(colorComboBox);
            colorComboBox.SelectionChanged += ColorComboBox_SelectionChanged;
            Grid.SetColumn(colorComboBox, 3);

            Button removeBtn = new Button
            {
                Content = "X",
                Width = 30,
                Height = 30,
                Tag = vongCount
            };
            removeBtn.Click += RemoveVong_Click;
            Grid.SetColumn(removeBtn, 4);

            grid.Children.Add(label);
            grid.Children.Add(soVongTextBox);
            grid.Children.Add(giaVeTextBox);
            grid.Children.Add(colorComboBox);
            grid.Children.Add(removeBtn);

            VongLoaiStackPanel.Children.Add(grid);
        }

        private void RemoveVong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Parent is Grid grid)
                VongLoaiStackPanel.Children.Remove(grid);
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            while (VongLoaiStackPanel.Children.Count > 1)
                VongLoaiStackPanel.Children.RemoveAt(VongLoaiStackPanel.Children.Count - 1);

            if (VongLoaiStackPanel.Children[0] is Grid grid)
            {
                if (grid.Children[1] is TextBox tb) tb.Clear();
                if (grid.Children[2] is TextBox tbGia) tbGia.Clear();
                if (grid.Children[3] is ComboBox cb) cb.SelectedIndex = 0;
            }

            vongCount = 1;
            ExcelStatusTextBlock.Text = "Chưa import file";
            ExcelStatusTextBlock.Foreground = Brushes.Gray;
            ExcelStatusTextBlock.FontStyle = FontStyles.Italic;
        }

        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx;*.xls",
                Title = "Chọn file Excel"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    ClearAll_Click(null, null);

                    using (var package = new ExcelPackage(new FileInfo(openFileDialog.FileName)))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        int rowCount = worksheet.Dimension?.Rows ?? 0;

                        if (rowCount == 0)
                        {
                            MessageBox.Show("File Excel trống!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        bool firstRow = true;
                        int successCount = 0;

                        for (int row = 1; row <= rowCount; row++)
                        {
                            List<int> numbers = new List<int>();
                            for (int col = 1; col <= 6; col++)
                            {
                                var cellValue = worksheet.Cells[row, col].Value;
                                if (cellValue != null && int.TryParse(cellValue.ToString(), out int num))
                                    numbers.Add(num);
                            }

                            string numberString = string.Concat(numbers);

                            if (firstRow)
                            {
                                if (VongLoaiStackPanel.Children[0] is Grid g && g.Children[1] is TextBox tb)
                                    tb.Text = numberString;
                                firstRow = false;
                            }
                            else
                            {
                                AddVong_Click(null, null);
                                int last = VongLoaiStackPanel.Children.Count - 1;
                                if (VongLoaiStackPanel.Children[last] is Grid g && g.Children[1] is TextBox tb)
                                    tb.Text = numberString;
                            }
                            successCount++;
                        }

                        ExcelStatusTextBlock.Text = $"✓ Đã import {successCount} vòng từ {Path.GetFileName(openFileDialog.FileName)}";
                        ExcelStatusTextBlock.Foreground = Brushes.Green;
                        ExcelStatusTextBlock.FontStyle = FontStyles.Normal;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi đọc file Excel:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    ExcelStatusTextBlock.Text = "✗ Lỗi import file";
                    ExcelStatusTextBlock.Foreground = Brushes.Red;
                }
            }
        }

        private void DownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                FileName = "Template_VongLoai.xlsx",
                Title = "Lưu file template"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Vòng Loại");

                        int[][] sampleData = new int[][]
                        {
                            new int[] { 1, 2, 3, 4, 5, 6 },
                            new int[] { 6, 5, 4, 3, 2, 1 },
                            new int[] { 4, 5, 6, 7, 8, 9 },
                            new int[] { 9, 8, 7, 6, 5, 4 }
                        };

                        for (int row = 0; row < sampleData.Length; row++)
                            for (int col = 0; col < 6; col++)
                                worksheet.Cells[row + 1, col + 1].Value = sampleData[row][col];

                        for (int col = 1; col <= 6; col++)
                            worksheet.Column(col).Width = 10;

                        package.SaveAs(new FileInfo(saveFileDialog.FileName));

                        MessageBox.Show($"Template đã được lưu tại:\n{saveFileDialog.FileName}",
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tạo template:\n{ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            VongLoaiData.Clear();
            bool hasError = false;
            int vongNumber = 1;

            foreach (UIElement element in VongLoaiStackPanel.Children)
            {
                if (!(element is Grid grid) || grid.Children.Count < 5)
                    continue;

                TextBox soVongTB = grid.Children[1] as TextBox;
                TextBox giaVeTB = grid.Children[2] as TextBox;
                ComboBox colorCB = grid.Children[3] as ComboBox;

                if (soVongTB == null || giaVeTB == null || colorCB == null)
                    continue;

                string inputText = soVongTB.Text.Trim().Replace(" ", "");
                if (string.IsNullOrEmpty(inputText))
                {
                    MessageBox.Show($"Vòng {vongNumber} chưa nhập số!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    hasError = true;
                    break;
                }

                List<int> vongNumbers = new List<int>();
                foreach (char c in inputText)
                {
                    if (!char.IsDigit(c))
                    {
                        MessageBox.Show($"Vòng {vongNumber} chứa ký tự không hợp lệ: '{c}'",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        hasError = true;
                        break;
                    }
                    vongNumbers.Add(c - '0');
                }

                if (hasError) break;

                string giaVeText = giaVeTB.Text.Trim().Replace(",", "").Replace(".", "");
                if (string.IsNullOrEmpty(giaVeText) || !decimal.TryParse(giaVeText, out decimal giaVe) || giaVe <= 0)
                {
                    MessageBox.Show($"Vòng {vongNumber} chưa nhập giá vé hợp lệ!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    hasError = true;
                    break;
                }

                ColorOption selectedColor = colorCB.SelectedItem as ColorOption;
                SolidColorBrush selectedBrush = selectedColor?.Color ?? Brushes.White;

                VongLoaiData[vongNumber] = new VongLoaiInfo
                {
                    Numbers = vongNumbers,
                    GiaVe = giaVe,
                    Color = selectedBrush
                };

                vongNumber++;
            }

            if (!hasError && VongLoaiData.Count > 0)
            {
                DialogResult = true;
                Close();
            }
            else if (!hasError && VongLoaiData.Count == 0)
            {
                MessageBox.Show("Vui lòng nhập ít nhất một vòng!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}