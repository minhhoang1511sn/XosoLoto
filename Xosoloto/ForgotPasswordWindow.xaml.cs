using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Xosoloto.Services;

namespace Xosoloto
{
    public partial class ForgotPasswordWindow : Window
    {
        /// <summary>Username của tài khoản vừa reset mật khẩu thành công (null nếu người dùng hủy).</summary>
        public string? ResetUsername { get; private set; }

        private enum Step { EnterAccount, EnterOtpAndPassword }

        private Step _step = Step.EnterAccount;

        // Xác định tài khoản đang xử lý (dùng để reset mật khẩu và làm key OTP).
        private string _accountUsername = "";
        private string _accountEmail = "";

        public ForgotPasswordWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => txtAccount.Focus();
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            txtError.Text = "";

            if (_step == Step.EnterAccount)
                await HandleSendOtpAsync();
            else
                HandleResetPassword();
        }

        private async Task HandleSendOtpAsync()
        {
            string input = txtAccount.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                txtError.Text = "Vui lòng nhập tên đăng nhập hoặc email.";
                return;
            }

            string? username = AccountService.FindUsernameForAccount(input);
            string? email = AccountService.FindEmailForAccount(input);

            if (username == null || string.IsNullOrWhiteSpace(email))
            {
                // Không tiết lộ chi tiết tài khoản nào tồn tại hay không, tránh dò email/username.
                txtError.Text = "Không tìm thấy tài khoản phù hợp.";
                return;
            }

            _accountUsername = username;
            _accountEmail = email;

            btnSubmit.IsEnabled = false;
            btnSubmit.Content = "Đang gửi mã...";
            string otp = OtpService.GenerateOtp(_accountEmail);
            var (sent, message) = await EmailService.SendOtpEmailAsync(_accountEmail, otp, "reset");
            btnSubmit.IsEnabled = true;

            if (!sent)
            {
                txtError.Text = message;
                btnSubmit.Content = "Gửi mã OTP";
                return;
            }

            SwitchToStep2();
        }

        private void HandleResetPassword()
        {
            string otp = txtOtp.Text.Trim();
            string newPassword = txtNewPassword.Password;
            string confirmPassword = txtConfirmNewPassword.Password;

            if (string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(newPassword))
            {
                txtError.Text = "Vui lòng nhập đầy đủ mã OTP và mật khẩu mới.";
                return;
            }

            if (newPassword != confirmPassword)
            {
                txtError.Text = "Mật khẩu xác nhận không khớp.";
                return;
            }

            var (verified, verifyMessage) = OtpService.VerifyOtp(_accountEmail, otp);
            if (!verified)
            {
                txtError.Text = verifyMessage;
                return;
            }

            var (success, message) = AccountService.ResetPassword(_accountUsername, newPassword);
            if (!success)
            {
                txtError.Text = message;
                return;
            }

            ResetUsername = _accountUsername;
            DialogResult = true;
            Close();
        }

        private async void txtResendOtp_Click(object sender, MouseButtonEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_accountEmail)) return;

            txtError.Text = "";
            txtResendOtp.IsEnabled = false;
            string otp = OtpService.GenerateOtp(_accountEmail);
            var (sent, message) = await EmailService.SendOtpEmailAsync(_accountEmail, otp, "reset");
            txtResendOtp.IsEnabled = true;

            txtError.Text = sent ? "Đã gửi lại mã OTP." : message;
        }

        private void SwitchToStep2()
        {
            _step = Step.EnterOtpAndPassword;
            txtError.Text = "";

            AccountPanel.Visibility = Visibility.Collapsed;
            OtpPanel.Visibility = Visibility.Visible;
            NewPasswordPanel.Visibility = Visibility.Visible;
            ConfirmPasswordPanel.Visibility = Visibility.Visible;

            btnSubmit.Content = "Đặt lại mật khẩu";
            txtSubtitle.Text = $"Nhập mã OTP đã gửi tới {_accountEmail} và mật khẩu mới";
            txtOtp.Focus();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
