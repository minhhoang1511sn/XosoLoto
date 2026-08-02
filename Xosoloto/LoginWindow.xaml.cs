using System.Windows;
using System.Windows.Input;
using Xosoloto.Services;

namespace Xosoloto
{
    public partial class LoginWindow : Window
    {
        /// <summary>Tên tài khoản vừa đăng nhập thành công (null nếu người dùng hủy).</summary>
        public string? LoggedInUsername { get; private set; }

        private enum Mode { Login, Register, RegisterOtp }

        private Mode _mode = Mode.Login;

        // Lưu tạm thông tin đăng ký trong lúc chờ người dùng nhập OTP.
        private string _pendingUsername = "";
        private string _pendingEmail = "";
        private string _pendingPassword = "";

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

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";

            switch (_mode)
            {
                case Mode.Login:
                    await HandleLoginAsync();
                    break;

                case Mode.Register:
                    await HandleRegisterSubmitAsync();
                    break;

                case Mode.RegisterOtp:
                    await HandleOtpVerifyAsync();
                    break;
            }
        }

        private async System.Threading.Tasks.Task HandleLoginAsync()
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                txtError.Text = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return;
            }

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
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task HandleRegisterSubmitAsync()
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                txtError.Text = "Vui lòng nhập đầy đủ thông tin.";
                return;
            }

            if (password != txtConfirmPassword.Password)
            {
                txtError.Text = "Mật khẩu xác nhận không khớp.";
                return;
            }

            var (valid, message) = AccountService.ValidateRegistration(username, email, password);
            if (!valid)
            {
                txtError.Text = message;
                return;
            }

            // Gửi OTP tới email trước khi tạo tài khoản.
            btnSubmit.IsEnabled = false;
            btnSubmit.Content = "Đang gửi mã...";
            string otp = OtpService.GenerateOtp(email);
            var (sent, sendMessage) = await EmailService.SendOtpEmailAsync(email, otp, "register");
            btnSubmit.IsEnabled = true;

            if (!sent)
            {
                txtError.Text = sendMessage;
                btnSubmit.Content = "Tạo tài khoản";
                return;
            }

            _pendingUsername = username;
            _pendingEmail = email;
            _pendingPassword = password;

            SwitchToRegisterOtpMode();
        }

        private async System.Threading.Tasks.Task HandleOtpVerifyAsync()
        {
            string otp = txtOtp.Text.Trim();
            if (string.IsNullOrWhiteSpace(otp))
            {
                txtError.Text = "Vui lòng nhập mã OTP.";
                return;
            }

            var (verified, verifyMessage) = OtpService.VerifyOtp(_pendingEmail, otp);
            if (!verified)
            {
                txtError.Text = verifyMessage;
                return;
            }

            var (success, message) = AccountService.CompleteRegistration(_pendingUsername, _pendingEmail, _pendingPassword);
            if (!success)
            {
                txtError.Text = message;
                return;
            }

            MessageBox.Show("Tạo tài khoản thành công! Vui lòng đăng nhập.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);

            txtPassword.Password = "";
            txtConfirmPassword.Password = "";
            txtOtp.Text = "";
            txtUsername.Text = _pendingUsername;
            SwitchMode(Mode.Login);
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async void txtResendOtp_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_pendingEmail)) return;

            txtError.Text = "";
            txtResendOtp.IsEnabled = false;
            string otp = OtpService.GenerateOtp(_pendingEmail);
            var (sent, sendMessage) = await EmailService.SendOtpEmailAsync(_pendingEmail, otp, "register");
            txtResendOtp.IsEnabled = true;

            txtError.Text = sent ? "Đã gửi lại mã OTP." : sendMessage;
        }

        private void txtToggleMode_Click(object sender, MouseButtonEventArgs e)
        {
            SwitchMode(_mode == Mode.Login ? Mode.Register : Mode.Login);
        }

        private void txtForgotPassword_Click(object sender, MouseButtonEventArgs e)
        {
            var forgotWindow = new ForgotPasswordWindow
            {
                Owner = this
            };
            bool? result = forgotWindow.ShowDialog();
            if (result == true && !string.IsNullOrEmpty(forgotWindow.ResetUsername))
            {
                SwitchMode(Mode.Login);
                txtUsername.Text = forgotWindow.ResetUsername;
                txtPassword.Password = "";
                txtError.Text = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
                txtPassword.Focus();
            }
        }

        private void SwitchToRegisterOtpMode()
        {
            _mode = Mode.RegisterOtp;
            txtError.Text = "";

            UsernamePanel.Visibility = Visibility.Collapsed;
            EmailPanel.Visibility = Visibility.Collapsed;
            PasswordPanel.Visibility = Visibility.Collapsed;
            ConfirmPasswordPanel.Visibility = Visibility.Collapsed;
            OtpPanel.Visibility = Visibility.Visible;
            chkRemember.Visibility = Visibility.Collapsed;
            txtForgotPassword.Visibility = Visibility.Collapsed;
            txtToggleMode.Visibility = Visibility.Collapsed;

            btnSubmit.Content = "Xác nhận mã OTP";
            txtSubtitle.Text = $"Nhập mã OTP đã gửi tới {_pendingEmail}";
            txtOtp.Focus();
        }

        private void SwitchMode(Mode mode)
        {
            _mode = mode;
            bool registerMode = mode == Mode.Register;

            txtError.Text = "";
            UsernamePanel.Visibility = Visibility.Visible;
            PasswordPanel.Visibility = Visibility.Visible;
            EmailPanel.Visibility = registerMode ? Visibility.Visible : Visibility.Collapsed;
            ConfirmPasswordPanel.Visibility = registerMode ? Visibility.Visible : Visibility.Collapsed;
            OtpPanel.Visibility = Visibility.Collapsed;
            chkRemember.Visibility = registerMode ? Visibility.Collapsed : Visibility.Visible;
            txtForgotPassword.Visibility = registerMode ? Visibility.Collapsed : Visibility.Visible;
            txtToggleMode.Visibility = Visibility.Visible;

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
