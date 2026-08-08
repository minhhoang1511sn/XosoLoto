using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Xosoloto.Models;
using Xosoloto.Services;

namespace Xosoloto
{
    public partial class InitLocXuan : Window
    {
        private const int MIN_VONG = 2;
        private const int MAX_VONG = 12;
        private const int DEFAULT_VONG = 5;

        private readonly string _username;

        private string imagePath = string.Empty;
        private string logoPath = string.Empty;

        /// <summary>
        /// True nếu người dùng bấm "🔁 Đổi loại game" (ở đây hoặc từ màn hình quay giải
        /// LuckyDrawWindow bên trong) để thoát khỏi Lộc Xuân Đầu Năm và quay lại màn hình
        /// chọn loại game, thay vì đóng hẳn ứng dụng. MainWindow.ShowLocXuan() kiểm tra cờ
        /// này sau khi ShowDialog() trả về để quyết định có mở lại màn hình chọn game hay không.
        /// </summary>
        public bool ChangeGameRequested { get; private set; } = false;

        // Số lượng phần tử ĐỘNG (không còn cố định 5) — mỗi phần tử là đường dẫn ảnh giải.
        private List<string> prizePaths = new();
        // Các Image control preview tương ứng, theo cùng thứ tự với prizePaths.
        private List<Image> prizePreviews = new();

        public InitLocXuan() : this(string.Empty) { }

        public InitLocXuan(string username)
        {
            InitializeComponent();
            _username = string.IsNullOrWhiteSpace(username) ? string.Empty : username.Trim();

            // Đảm bảo cửa sổ luôn vừa với màn hình, kể cả màn hình nhỏ
            this.Loaded += (s, e) =>
            {
                var workArea = SystemParameters.WorkArea;
                this.MaxWidth = workArea.Width;
                this.MaxHeight = workArea.Height;
                if (this.Width > workArea.Width) this.Width = workArea.Width;
                if (this.Height > workArea.Height) this.Height = workArea.Height;
            };

            btnAddImg.Click += BtnAddImg_Click;
            btnAddLogo.Click += BtnAddLogo_Click;
            btnDone.Click += BtnDone_Click;
            btnChangeGame.Click += BtnChangeGame_Click;
            btnSoVongMinus.Click += (s, e) => SetSoVong(CurrentSoVong - 1);
            btnSoVongPlus.Click += (s, e) => SetSoVong(CurrentSoVong + 1);

            // Nạp lại cấu hình đã lưu (nếu có) cho tài khoản này, nếu không thì dùng mặc định.
            if (!TryLoadSavedConfig())
            {
                BuildPrizeRows(DEFAULT_VONG);
            }
        }

        private int CurrentSoVong => prizePaths.Count;

        private void SetSoVong(int newCount)
        {
            newCount = Math.Max(MIN_VONG, Math.Min(MAX_VONG, newCount));
            if (newCount == CurrentSoVong) return;
            BuildPrizeRows(newCount);
        }

        /// <summary>
        /// Sinh động danh sách các dòng "chọn ảnh giải thưởng" theo đúng số vòng hiện tại.
        /// Giữ lại đường dẫn ảnh đã chọn trước đó cho các vị trí còn tồn tại (nếu tăng/giảm số vòng).
        /// </summary>
        private void BuildPrizeRows(int count)
        {
            var oldPaths = prizePaths;

            prizePaths = new List<string>(count);
            prizePreviews = new List<Image>(count);
            pnlPrizes.Children.Clear();

            for (int i = 0; i < count; i++)
            {
                string existingPath = oldPaths != null && i < oldPaths.Count ? oldPaths[i] : string.Empty;
                prizePaths.Add(existingPath ?? string.Empty);

                bool isLastSlot = i == count - 1;
                string label = isLastSlot ? "Khuyến khích:" : $"Prize {i + 1}:";

                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
                row.Children.Add(new Label { Content = label, Width = 100, VerticalAlignment = VerticalAlignment.Center });

                int capturedIndex = i;
                var btn = new Button { Content = "Select Image", Width = 100, Height = 30, Margin = new Thickness(0, 0, 10, 0), Tag = capturedIndex };
                btn.Click += (s, e) => BtnAddPrize_Click(capturedIndex);
                row.Children.Add(btn);

                var border = new Border { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(1), Width = 120, Height = 90 };
                var img = new Image { Stretch = System.Windows.Media.Stretch.Uniform };
                if (!string.IsNullOrWhiteSpace(existingPath) && File.Exists(existingPath))
                {
                    img.Source = LoadPreview(existingPath);
                }
                border.Child = img;
                row.Children.Add(border);

                prizePreviews.Add(img);
                pnlPrizes.Children.Add(row);
            }

            txtSoVong.Text = count.ToString();
        }

        private static BitmapImage LoadPreview(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        private void BtnAddImg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";
            openFileDialog.Title = "Select Background Image";

            if (openFileDialog.ShowDialog() == true)
            {
                imagePath = openFileDialog.FileName;
                imgBackgroundPreview.Source = LoadPreview(imagePath);
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
                imgLogoPreview.Source = LoadPreview(logoPath);
            }
        }

        private void BtnAddPrize_Click(int prizeIndex)
        {
            if (prizeIndex < 0 || prizeIndex >= prizePaths.Count) return;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";
            openFileDialog.Title = $"Select Prize {prizeIndex + 1} Image";

            if (openFileDialog.ShowDialog() == true)
            {
                prizePaths[prizeIndex] = openFileDialog.FileName;
                prizePreviews[prizeIndex].Source = LoadPreview(prizePaths[prizeIndex]);
            }
        }

        /// <summary>
        /// Nạp lại cấu hình Lộc Xuân đã lưu trước đó cho tài khoản hiện tại (nếu có).
        /// Trả về false nếu không có tài khoản / không có cấu hình đã lưu.
        /// </summary>
        private bool TryLoadSavedConfig()
        {
            if (string.IsNullOrEmpty(_username)) return false;

            var saved = AccountService.LoadSettings(_username);
            var locXuan = saved?.LocXuan;
            if (locXuan == null) return false;

            int soVong = locXuan.SoVong > 0 ? locXuan.SoVong : DEFAULT_VONG;
            soVong = Math.Max(MIN_VONG, Math.Min(MAX_VONG, soVong));

            txtTitle.Text = locXuan.Title ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(locXuan.ImagePath) && File.Exists(locXuan.ImagePath))
            {
                imagePath = locXuan.ImagePath;
                imgBackgroundPreview.Source = LoadPreview(imagePath);
            }

            if (!string.IsNullOrWhiteSpace(locXuan.LogoPath) && File.Exists(locXuan.LogoPath))
            {
                logoPath = locXuan.LogoPath;
                imgLogoPreview.Source = LoadPreview(logoPath);
            }

            prizePaths = new List<string>(locXuan.PrizePaths ?? new List<string>());
            while (prizePaths.Count < soVong) prizePaths.Add(string.Empty);
            if (prizePaths.Count > soVong) prizePaths = prizePaths.Take(soVong).ToList();

            BuildPrizeRows(soVong);
            return true;
        }

        /// <summary>Lưu lại cấu hình Lộc Xuân hiện tại cho tài khoản đang đăng nhập.</summary>
        private void SaveCurrentConfig()
        {
            if (string.IsNullOrEmpty(_username)) return;

            var data = AccountService.LoadSettings(_username) ?? new AppSettingsData();
            data.GameType = GameType.LocXuanDauNam.ToString();
            data.LocXuan = new LocXuanSettingDto
            {
                Title = txtTitle.Text ?? string.Empty,
                ImagePath = imagePath,
                LogoPath = logoPath,
                PrizePaths = new List<string>(prizePaths),
                SoVong = CurrentSoVong
            };

            AccountService.SaveSettings(_username, data);
        }

        /// <summary>
        /// Thoát khỏi màn hình thiết lập Lộc Xuân và quay lại màn hình chọn loại game,
        /// thay vì phải đóng hẳn ứng dụng.
        /// </summary>
        private void BtnChangeGame_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Bạn có muốn thoát khỏi thiết lập Lộc Xuân Đầu Năm và chọn lại loại game khác không?",
                "Đổi loại game", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            ChangeGameRequested = true;
            // DialogResult = true để nơi gọi (ShowLocXuan) không hiểu nhầm là "hủy" và tự
            // Shutdown() ứng dụng - xem ChangeGameRequested để biết cần mở lại màn hình chọn game.
            CloseAsDialogResult(true);
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

            // Kiểm tra xem đã chọn đủ ảnh cho tất cả các giải (số vòng động) chưa
            for (int i = 0; i < prizePaths.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(prizePaths[i]))
                {
                    return;
                }
            }

            // Lưu cấu hình lại cho tài khoản hiện tại trước khi mở màn hình quay số
            SaveCurrentConfig();

            LuckyDrawWindow luc = new LuckyDrawWindow(imagePath, txtTitle.Text, logoPath, prizePaths.ToArray());

            // Ẩn cửa sổ hiện tại
            this.Hide();

            // Hiển thị LuckyDrawWindow
            luc.ShowDialog();

            // Nếu người dùng bấm "Đổi loại game" TRONG màn hình quay giải (LuckyDrawWindow),
            // truyền cờ đó lên cho nơi gọi (ShowLocXuan) để mở lại màn hình chọn loại game.
            if (luc.ChangeGameRequested)
                ChangeGameRequested = true;

            // Báo cho nơi gọi (ShowLocXuan) biết là thiết lập đã HOÀN TẤT thành công,
            // không phải bị hủy — nếu không set DialogResult, ShowDialog() ở nơi gọi
            // sẽ trả về null (không phải true), khiến ứng dụng hiểu nhầm là "hủy" và
            // có thể tự thoát ứng dụng ngay sau khi người dùng quay xong Lộc Xuân.
            // Đóng cửa sổ InitLocXuan sau khi LuckyDrawWindow đóng
            CloseAsDialogResult(true);
        }

        /// <summary>
        /// Set DialogResult rồi Close() một cách AN TOÀN. WPF chỉ cho set DialogResult khi
        /// cửa sổ đang thực sự ở trạng thái được mở bằng ShowDialog(); nếu vì lý do nào đó
        /// (ví dụ luồng đóng lồng nhau của Lộc Xuân: InitLocXuan → LuckyDrawWindow →
        /// PrizeDisplayWindow) cửa sổ không còn ở trạng thái đó nữa, việc set sẽ ném
        /// InvalidOperationException ("DialogResult can only be set after Window has been
        /// created and displayed as a dialog box"). Bọc lại để trường hợp đó chỉ đơn giản là
        /// Close() bình thường thay vì làm crash toàn bộ ứng dụng.
        /// </summary>
        private void CloseAsDialogResult(bool result)
        {
            try
            {
                // Việc set DialogResult (nếu thành công) tự động Close() cửa sổ luôn.
                this.DialogResult = result;
                return;
            }
            catch (InvalidOperationException)
            {
                // Cửa sổ không còn ở trạng thái ShowDialog (đã đóng/đang đóng) - bỏ qua,
                // rơi xuống Close() thường bên dưới để vẫn đảm bảo cửa sổ được dọn dẹp.
            }

            try { this.Close(); } catch (InvalidOperationException) { /* đã đóng rồi */ }
        }
    }
}
