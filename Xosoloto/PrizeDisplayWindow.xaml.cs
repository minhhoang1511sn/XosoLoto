using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Xosoloto
{
    public partial class PrizeDisplayWindow : Window
    {
        private string backgroundPath;
        private string logoPath;
        private string[] prizePaths;
        private string eventTitle;
        private string imagePath;
        private int prizeIndex;
        private LuckyDrawWindow parentWindow;
        private bool isUpdating = false; // Để tránh vòng lặp vô hạn

        public PrizeDisplayWindow(string title, string logoPath, string prizeName, int prizeIndex, string[] prizePaths, string backgroundPath = null, string imagePath = null, LuckyDrawWindow parent = null)
        {
            InitializeComponent();
            this.eventTitle = title;
            this.logoPath = logoPath;
            this.prizeIndex = prizeIndex;
            this.prizePaths = prizePaths ?? new string[5];
            this.backgroundPath = backgroundPath;
            this.imagePath = imagePath;
            this.parentWindow = parent;

            this.Loaded += (s, e) => LoadInitialData();
            this.KeyDown += (s, e) => {
                if (e.Key == Key.Left) MovePrize(-1);
                else if (e.Key == Key.Right) MovePrize(1);
                else if (e.Key == Key.Escape) this.Close();
            };

            // Thêm sự kiện cho txtPrizeNumber
            txtPrizeNumber.PreviewTextInput += TxtPrizeNumber_PreviewTextInput;
            txtPrizeNumber.TextChanged += TxtPrizeNumber_TextChanged;
            txtPrizeNumber.KeyDown += TxtPrizeNumber_KeyDown;
        }

        private void TxtPrizeNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            char c = e.Text[0];
            e.Handled = !(char.IsDigit(c) || c == '-');
        }

        private void TxtPrizeNumber_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (isUpdating) return;

            string raw = txtPrizeNumber.Text;

            // =================================================
            // 🎯 GIẢI 5 → TEXT THUẦN (không format gì)
            // =================================================
            if (IsKhuyenKhichSlot(prizeIndex))
            {
                parentWindow?.UpdatePrizeNumber(prizeIndex, raw);
                return;
            }

            // =================================================
            // 🎯 GIẢI 1–4 (vẫn format 4 số như cũ)
            // =================================================
            isUpdating = true;

            string text = raw.Replace(" ", "")
                             .Replace("\r", "")
                             .Replace("\n", "");

            string numbers = "";

            foreach (char c in text)
            {
                if (char.IsDigit(c) || c == '-')
                    numbers += c;

                if (numbers.Length >= 4)
                    break;
            }

            string result = string.Join(" ", numbers.ToCharArray());

            txtPrizeNumber.Text = result;
            txtPrizeNumber.CaretIndex = result.Length;

            isUpdating = false;

            parentWindow?.UpdatePrizeNumber(prizeIndex, result);
        }
        private void TxtPrizeNumber_KeyDown(object sender, KeyEventArgs e)
        {
            // Khi nhấn Enter, tự động back về màn hình chính

            if (IsKhuyenKhichSlot(prizeIndex) && e.Key == Key.Enter)
            {
                e.Handled = true; // 🔥 CHẶN xuống dòng

                btnBack_Click(null, null); // quay về trang chính
            }

            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                return;
            }
            if (e.Key == Key.Enter)
            {
                string prizeNumber = txtPrizeNumber.Text.Trim();

                if (!string.IsNullOrEmpty(prizeNumber) && prizeNumber != "? ? ? ?")
                {
                    // Cập nhật lên parent window
                    if (parentWindow != null)
                    {
                        parentWindow.UpdatePrizeNumber(prizeIndex, prizeNumber);
                    }

                    // Back về màn hình chính
                    BackToMainScreen();
                }
            }
        }

        /// <summary>
        /// Giải "Khuyến khích" luôn là giải CUỐI CÙNG trong danh sách, bất kể số vòng
        /// (số giải) hiện đang cấu hình là bao nhiêu (trước đây cố định là index 4/5 giải).
        /// </summary>
        private bool IsKhuyenKhichSlot(int index) => prizePaths != null && prizePaths.Length > 0 && index == prizePaths.Length - 1;

        private void LoadInitialData()
        {
            try
            {
                imgBackground.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Images/DetailsPrize.png"));

                // Nạp Title
                txtTitle.Text = !string.IsNullOrEmpty(eventTitle) ? eventTitle : "ĐẠI HỘI TẾT";

                // Nạp Logo
                if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                    imgLogo.Source = new BitmapImage(new Uri(logoPath, UriKind.Absolute));

                DisplayPrize(prizeIndex);

                // Focus vào textbox để sẵn sàng nhập
                txtPrizeNumber.Focus();
                txtPrizeNumber.SelectAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo: " + ex.Message);
            }
        }

        private void DisplayPrize(int index)
        {
            if (index < 0 || index >= prizePaths.Length) return;
            prizeIndex = index;
            txtPrizeName.Text = !IsKhuyenKhichSlot(index) ? $"LỘC XUÂN {index + 1}" : "9 GIẢI KHUYẾN KHÍCH";
            if (IsKhuyenKhichSlot(index)) // Giải cuối cùng (khuyến khích)
            {
                
                txtPrizeNumber.FontSize = 35;
                txtPrizeNumber.Padding = new Thickness(-100, 0, 0, 0);
                txtPrizeName.Margin = new Thickness(-150, -70, 0, 30);
                txtPrizeName.FontSize = 50;
                txtPrizeNumber.Height = 180;
            }
            // Lấy số giải hiện tại từ parent window nếu có
            if (parentWindow != null)
            {
                string currentNumber = parentWindow.GetPrizeNumber(index);
                txtPrizeNumber.Text = !string.IsNullOrEmpty(currentNumber) ? currentNumber : "? ? ? ?";
            }
            else
            {
                txtPrizeNumber.Text = "? ? ? ?";
            }

            try
            {
                if (!string.IsNullOrEmpty(prizePaths[index]) && File.Exists(prizePaths[index]))
                    imgPrize.Source = new BitmapImage(new Uri(prizePaths[index], UriKind.Absolute));
                else
                    imgPrize.Source = null;
            }
            catch
            {
                imgPrize.Source = null;
            }

            // Focus lại vào textbox sau khi chuyển giải
            txtPrizeNumber.Focus();
            txtPrizeNumber.SelectAll();
        }

        private void MovePrize(int direction)
        {
            // Lưu số giải hiện tại trước khi chuyển
            string currentNumber = txtPrizeNumber.Text.Trim();
            if (parentWindow != null && !string.IsNullOrEmpty(currentNumber) && currentNumber != "? ? ? ?")
            {
                parentWindow.UpdatePrizeNumber(prizeIndex, currentNumber);
            }

            int nextIndex = prizeIndex + direction;
            if (nextIndex >= 0 && nextIndex < prizePaths.Length)
                DisplayPrize(nextIndex);
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
                // Lưu số giải hiện tại trước khi back
                string currentNumber = txtPrizeNumber.Text.Trim();
                if (parentWindow != null && !string.IsNullOrEmpty(currentNumber))
                {
                    parentWindow.UpdatePrizeNumber(prizeIndex, currentNumber);
                }

                BackToMainScreen();
        }

        private void BackToMainScreen()
        {
            if (parentWindow != null)
            {
                // Đóng cửa sổ hiện tại và quay về parent
                this.Close();
            }
            else
            {
                // Trường hợp không có parent (không nên xảy ra)
                LuckyDrawWindow luc = new LuckyDrawWindow(imagePath, txtTitle.Text, logoPath, prizePaths);
                this.Hide();
                luc.ShowDialog();
                this.Close();
            }
        }
    }
}