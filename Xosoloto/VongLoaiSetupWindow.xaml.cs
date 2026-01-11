using Microsoft.Win32;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public SolidColorBrush Color { get; set; }
    }

    public partial class VongLoaiSetupWindow : Window
    {
        public Dictionary<int, VongLoaiInfo> VongLoaiData { get; private set; }
        private int vongCount = 1;
        private List<ColorOption> availableColors;

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
            availableColors = new List<ColorOption>
            {
                 new ColorOption { Name = "Đỏ", Color = new SolidColorBrush(Color.FromRgb(220, 20, 60)) },
                new ColorOption { Name = "Xanh Dương", Color = new SolidColorBrush(Color.FromRgb(70, 130, 180)) },
                new ColorOption { Name = "Xanh Lá", Color = new SolidColorBrush(Color.FromRgb(60, 179, 113)) },
                new ColorOption { Name = "Vàng", Color = new SolidColorBrush(Color.FromRgb(255, 215, 0)) },
            };
        }

        private void PopulateColorComboBox(ComboBox comboBox)
        {
            comboBox.ItemsSource = availableColors;
            comboBox.SelectedIndex = 0; // Chọn màu đầu tiên mặc định
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Có thể thêm xử lý khi màu thay đổi nếu cần
        }

        private void AddVong_Click(object sender, RoutedEventArgs e)
        {
            vongCount++;

            Grid grid = new Grid { Margin = new Thickness(0, 5, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock label = new TextBlock
            {
                Text = $"Vòng {vongCount}:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold,
                Width = 60
            };
            Grid.SetColumn(label, 0);

            TextBox textBox = new TextBox
            {
                Name = $"Vong{vongCount}TextBox",
                FontSize = 14,
                Padding = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(textBox, 1);

            ComboBox colorComboBox = new ComboBox
            {
                Name = $"Vong{vongCount}ColorComboBox",
                Width = 150,
                Height = 30,
                Margin = new Thickness(0, 0, 5, 0),
                ItemTemplate = Vong1ColorComboBox.ItemTemplate
            };
            PopulateColorComboBox(colorComboBox);
            colorComboBox.SelectionChanged += ColorComboBox_SelectionChanged;
            Grid.SetColumn(colorComboBox, 2);

            Button removeBtn = new Button
            {
                Content = "X",
                Width = 30,
                Height = 30,
                Margin = new Thickness(5, 0, 0, 0),
                Tag = vongCount
            };
            removeBtn.Click += RemoveVong_Click;
            Grid.SetColumn(removeBtn, 3);

            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(colorComboBox);
            grid.Children.Add(removeBtn);

            VongLoaiStackPanel.Children.Add(grid);
        }

        private void RemoveVong_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Parent is Grid grid)
            {
                VongLoaiStackPanel.Children.Remove(grid);
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            // Xóa tất cả trừ vòng 1
            while (VongLoaiStackPanel.Children.Count > 1)
            {
                VongLoaiStackPanel.Children.RemoveAt(VongLoaiStackPanel.Children.Count - 1);
            }

            // Clear vòng 1
            if (VongLoaiStackPanel.Children[0] is Grid grid)
            {
                if (grid.Children[1] is TextBox textBox)
                {
                    textBox.Clear();
                }
                if (grid.Children[2] is ComboBox colorComboBox)
                {
                    colorComboBox.SelectedIndex = 0;
                }
            }

            vongCount = 1;
            ExcelStatusTextBlock.Text = "Chưa import file";
            ExcelStatusTextBlock.Foreground = Brushes.Gray;
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
                    // Clear existing data
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

                        // Load vòng đầu tiên vào textbox có sẵn
                        bool firstRow = true;
                        int successCount = 0;

                        for (int row = 1; row <= rowCount; row++)
                        {
                            List<int> numbers = new List<int>();

                            // Đọc 6 cột
                            for (int col = 1; col <= 6; col++)
                            {
                                var cellValue = worksheet.Cells[row, col].Value;

                                if (cellValue != null && int.TryParse(cellValue.ToString(), out int num))
                                {
                                    numbers.Add(num);
                                }
                            }

                            string numberString = string.Concat(numbers);

                            if (firstRow)
                            {
                                // Fill vào vòng 1 có sẵn
                                if (VongLoaiStackPanel.Children[0] is Grid grid &&
                                    grid.Children[1] is TextBox textBox)
                                {
                                    textBox.Text = numberString;
                                }
                                firstRow = false;
                            }
                            else
                            {
                                // Thêm vòng mới
                                AddVong_Click(null, null);

                                // Fill data vào vòng vừa tạo
                                if (VongLoaiStackPanel.Children[VongLoaiStackPanel.Children.Count - 1] is Grid grid &&
                                    grid.Children[1] is TextBox textBox)
                                {
                                    textBox.Text = numberString;
                                }
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
                    MessageBox.Show($"Lỗi đọc file Excel:\n{ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
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

                        // Tạo data mẫu
                        int[][] sampleData = new int[][]
                        {
                            new int[] { 1, 2, 3, 4, 5, 6 },
                            new int[] { 6, 5, 4, 3, 2, 1 },
                            new int[] { 4, 5, 6, 7, 8, 9 },
                            new int[] { 9, 8, 7, 6, 5, 4 }
                        };

                        // Fill data
                        for (int row = 0; row < sampleData.Length; row++)
                        {
                            for (int col = 0; col < 6; col++)
                            {
                                worksheet.Cells[row + 1, col + 1].Value = sampleData[row][col];
                            }
                        }

                        // Style header
                        for (int col = 1; col <= 6; col++)
                        {
                            worksheet.Column(col).Width = 10;
                        }

                        // Save file
                        package.SaveAs(new FileInfo(saveFileDialog.FileName));

                        MessageBox.Show($"Template đã được lưu tại:\n{saveFileDialog.FileName}",
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi tạo template:\n{ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
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
                if (element is Grid grid && grid.Children.Count >= 3)
                {
                    TextBox textBox = grid.Children[1] as TextBox;
                    ComboBox colorComboBox = grid.Children[2] as ComboBox;

                    if (textBox == null || colorComboBox == null)
                        continue;

                    string inputText = textBox.Text.Trim().Replace(" ", "");

                    if (string.IsNullOrEmpty(inputText))
                    {
                        MessageBox.Show($"Vòng {vongNumber} chưa nhập số!",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                        hasError = true;
                        break;
                    }

                    List<int> vongNumbers = new List<int>();

                    foreach (char c in inputText)
                    {
                        if (!char.IsDigit(c))
                        {
                            MessageBox.Show(
                                $"Vòng {vongNumber} chứa ký tự không hợp lệ: '{c}'",
                                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                            hasError = true;
                            break;
                        }

                        vongNumbers.Add(c - '0');
                    }

                    if (hasError)
                        break;

                    // Lấy màu đã chọn
                    ColorOption selectedColor = colorComboBox.SelectedItem as ColorOption;
                    SolidColorBrush selectedBrush = selectedColor?.Color ?? Brushes.White;

                    VongLoaiData[vongNumber] = new VongLoaiInfo
                    {
                        Numbers = vongNumbers,
                        Color = selectedBrush
                    };

                    vongNumber++;
                }
            }

            if (!hasError && VongLoaiData.Count > 0)
            {
                DialogResult = true;
                Close();
            }
            else if (VongLoaiData.Count == 0)
            {
                MessageBox.Show("Vui lòng nhập ít nhất một vòng!",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}