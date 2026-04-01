using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;

namespace Spexts;

public partial class App : Application
{
    public static bool IsAdmin { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handler — log crash details to file
        DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            IsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            IsAdmin = false;
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
        File.WriteAllText(logPath, $"[{DateTime.Now}] DISPATCHER EXCEPTION:\n{e.Exception}\n");
        MessageBox.Show(e.Exception.ToString(), "Spexts Crash", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = false;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
        File.WriteAllText(logPath, $"[{DateTime.Now}] DOMAIN EXCEPTION:\n{e.ExceptionObject}\n");
    }
}
