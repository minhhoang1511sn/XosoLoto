using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Xosoloto
{
    public partial class InitLocXuan : Window
    {
        private string imagePath = string.Empty;
        private string logoPath = string.Empty;
        private string[] prizePaths = new string[5];

        public InitLocXuan()
        {
            InitializeComponent();
            
            // Khởi tạo mảng prize paths
            for (int i = 0; i < prizePaths.Length; i++)
            {
                prizePaths[i] = string.Empty;
            }
            
            // Gắn sự kiện cho các button
            btnAddImg.Click += BtnAddImg_Click;
            btnAddLogo.Click += BtnAddLogo_Click;
            btnDone.Click += BtnDone_Click;
            
            // Gắn sự kiện cho các button prize
            btnAddPrize1.Click += BtnAddPrize_Click;
            btnAddPrize2.Click += BtnAddPrize_Click;
            btnAddPrize3.Click += BtnAddPrize_Click;
            btnAddPrize4.Click += BtnAddPrize_Click;
            btnAddPrize5.Click += BtnAddPrize_Click;
        }

        private void BtnAddImg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";
            openFileDialog.Title = "Select Background Image";
            
            if (openFileDialog.ShowDialog() == true)
            {
                imagePath = openFileDialog.FileName;
                
                // Hiển thị preview
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                imgBackgroundPreview.Source = bitmap;
                
            }
        }

        private void BtnAddLogo_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";
            openFileDialog.Title = "Select a Logo";
            
            if (openFileDialog.ShowDialog() == true)
            {
                logoPath = openFileDialog.FileName;
                
                // Hiển thị preview
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(logoPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                imgLogoPreview.Source = bitmap;
            }
        }

        private void BtnAddPrize_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button == null) return;
            
            int prizeIndex = int.Parse(button.Tag.ToString()) - 1;
            
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";
            openFileDialog.Title = $"Select Prize {prizeIndex + 1} Image";
            
            if (openFileDialog.ShowDialog() == true)
            {
                prizePaths[prizeIndex] = openFileDialog.FileName;
                
                // Hiển thị preview cho prize tương ứng
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(prizePaths[prizeIndex]);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                
                // Gán hình ảnh vào Image control tương ứng
                switch (prizeIndex)
                {
                    case 0:
                        imgPrize1Preview.Source = bitmap;
                        break;
                    case 1:
                        imgPrize2Preview.Source = bitmap;
                        break;
                    case 2:
                        imgPrize3Preview.Source = bitmap;
                        break;
                    case 3:
                        imgPrize4Preview.Source = bitmap;
                        break;
                    case 4:
                        imgPrize5Preview.Source = bitmap;
                        break;
                }
                
            }
        }

        private void BtnDone_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem đã nhập đủ dữ liệu chưa
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(logoPath))
            {
               
                return;
            }
            
            // Kiểm tra xem đã chọn đủ 5 hình ảnh giải thưởng chưa
            for (int i = 0; i < prizePaths.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(prizePaths[i]))
                {
                  
                    return;
                }
            }
            
            MessageBoxResult result = MessageBox.Show("Are you sure you want to finish?",
                                                      "Confirm",
                                                      MessageBoxButton.YesNo,
                                                      MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                // Truyền dữ liệu sang LuckyDrawWindow
                LuckyDrawWindow luc = new LuckyDrawWindow(imagePath, txtTitle.Text, logoPath, prizePaths);
                
                // Ẩn cửa sổ hiện tại
                this.Hide();
                
                // Hiển thị LuckyDrawWindow
                luc.ShowDialog();
                
                // Đóng cửa sổ InitLocXuan sau khi LuckyDrawWindow đóng
                this.Close();
            }
        }
    }
}