using System.Windows;
using System.Windows.Input;
using Xosoloto.Services;

namespace Xosoloto
{
    public partial class LoginWindow : Window
    {
        /// <summary>Tên tài khoản vừa đăng nhập thành công (null nếu người dùng hủy).</summary>
        public string? LoggedInUsername { get; private set; }

        private bool _isRegisterMode = false;

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                // Nếu có tài khoản đã "ghi nhớ đăng nhập" từ lần trước, điền sẵn username
                // và tick sẵn checkbox "Ghi nhớ" để người dùng chỉ cần nhập mật khẩu.
                string? remembered = AccountService.GetRememberedUser();
                if (!string.IsNullOrEmpty(remembered))
                {
                    txtUsername.Text = remembered;
                    chkRemember.IsChecked = true;
                    txtPassword.Focus();
                }
                else
                {
                    txtUsername.Focus();
                }
            };
        }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                txtError.Text = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return;
            }

            if (_isRegisterMode)
            {
                if (password != txtConfirmPassword.Password)
                {
                    txtError.Text = "Mật khẩu xác nhận không khớp.";
                    return;
                }

                var (success, message) = AccountService.Register(username, password);
                if (!success)
                {
                    txtError.Text = message;
                    return;
                }

                MessageBox.Show("Tạo tài khoản thành công! Vui lòng đăng nhập.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                txtPassword.Password = "";
                txtConfirmPassword.Password = "";
                SwitchMode(false);
            }
            else
            {
                var (success, message) = AccountService.Login(username, password);
                if (!success)
                {
                    txtError.Text = message;
                    return;
                }

                if (chkRemember.IsChecked == true)
                    AccountService.RememberUser(username);
                else
                    AccountService.ForgetUser();

                LoggedInUsername = username;
                DialogResult = true;
                Close();
            }
        }

        private void txtToggleMode_Click(object sender, MouseButtonEventArgs e)
        {
            SwitchMode(!_isRegisterMode);
        }

        private void SwitchMode(bool registerMode)
        {
            _isRegisterMode = registerMode;
            txtError.Text = "";
            ConfirmPasswordPanel.Visibility = registerMode ? Visibility.Visible : Visibility.Collapsed;
            chkRemember.Visibility = registerMode ? Visibility.Collapsed : Visibility.Visible;
            btnSubmit.Content = registerMode ? "Tạo tài khoản" : "Đăng nhập";
            txtSubtitle.Text = registerMode ? "Tạo tài khoản mới" : "Đăng nhập để tiếp tục";
            txtToggleMode.Text = registerMode ? "Đã có tài khoản? Đăng nhập" : "Chưa có tài khoản? Đăng ký ngay";
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
