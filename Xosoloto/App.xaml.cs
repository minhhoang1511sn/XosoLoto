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

        // Set license cho EPPlus
        //ExcelPackage.License = LicenseMode.NonCommercial;

        // Nếu có tài khoản được "ghi nhớ đăng nhập" từ lần trước thì vào thẳng, không cần hỏi lại
        string? username = AccountService.GetRememberedUser();

        if (string.IsNullOrEmpty(username))
        {
            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();
            if (result != true || string.IsNullOrEmpty(loginWindow.LoggedInUsername))
            {
                Shutdown();
                return;
            }
            username = loginWindow.LoggedInUsername;
        }

        var mainWindow = new MainWindow(username);
        this.MainWindow = mainWindow;
        mainWindow.Show();
    }
}
