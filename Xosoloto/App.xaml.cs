using OfficeOpenXml;
using System.Configuration;
using System.Data;
using System.Windows;
using Xosoloto.Services;

namespace Xosoloto;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Quan trọng: mặc định ShutdownMode = OnLastWindowClose. Vì LoginWindow là cửa sổ
        // đầu tiên (và duy nhất) đang mở, khi nó Close() sau khi đăng nhập thành công, WPF
        // sẽ coi đó là "cửa sổ cuối cùng đóng lại" và tự bắt đầu shutdown ứng dụng NGAY LẬP
        // TỨC — trước khi kịp tạo/Show() MainWindow, gây lỗi
        // "Cannot set Visibility ... after a Window has closed". Tắt auto-shutdown tạm thời
        // cho tới khi MainWindow đã Show() thành công.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Set license cho EPPlus
        //ExcelPackage.License = LicenseMode.NonCommercial;

        // Luôn hiển thị màn hình đăng nhập khi mở app.
        // Nếu có tài khoản đã "ghi nhớ đăng nhập" từ lần trước, chỉ dùng để điền sẵn
        // username cho tiện (xem LoginWindow), chứ không bỏ qua bước đăng nhập.
        var loginWindow = new LoginWindow();
        bool? result = loginWindow.ShowDialog();
        if (result != true || string.IsNullOrEmpty(loginWindow.LoggedInUsername))
        {
            Shutdown();
            return;
        }
        string username = loginWindow.LoggedInUsername;

        // Sau khi đăng nhập thành công, MainWindow mới kiểm tra dữ liệu cấu hình đã lưu
        // (TryLoadSavedSettings) cho tài khoản này.
        var mainWindow = new MainWindow(username);

        // Nếu người dùng hủy ở một bước bắt buộc trong lúc khởi tạo MainWindow (chọn loại
        // game, thiết lập vòng loại/Lộc Xuân...), ứng dụng đã tự Shutdown() rồi — không được
        // gọi Show() nữa vì window đã đóng, sẽ ném InvalidOperationException.
        if (mainWindow.IsShuttingDown) return;

        this.MainWindow = mainWindow;
        mainWindow.Show();

        // Khôi phục hành vi mặc định: đóng ứng dụng khi cửa sổ chính (MainWindow) đóng.
        ShutdownMode = ShutdownMode.OnLastWindowClose;
    }
}
