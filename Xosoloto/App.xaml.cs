using OfficeOpenXml;
using System.Configuration;
using System.Data;
using System.Windows;

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
    }
}

