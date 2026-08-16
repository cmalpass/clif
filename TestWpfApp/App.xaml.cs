using System;
using System.IO;
using System.Windows;

namespace TestWpfApp;

public partial class App : Application
{
    private static readonly string CrashLog = Path.Combine(Path.GetTempPath(), "TestWpfApp_crash.log");

    public App()
    {
        // Register handlers before InitializeComponent so we can catch XAML load errors
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        try
        {
            File.AppendAllText(CrashLog, $"[Startup] {DateTime.Now:O} - Before InitializeComponent\n");
            InitializeComponent();
            File.AppendAllText(CrashLog, $"[Startup] {DateTime.Now:O} - After InitializeComponent\n");
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(CrashLog, $"[Startup Exception] {DateTime.Now:O}\n{ex}\n"); } catch { }
            throw;
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var txt = $"[DispatcherUnhandledException] {DateTime.Now:O}\n{e.Exception}\n\n";
            File.AppendAllText(CrashLog, txt);
        }
        catch { }
        // let the process terminate after logging
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var ex = e.ExceptionObject as Exception;
            var txt = $"[DomainUnhandledException] {DateTime.Now:O}\n{ex}\nIsTerminating={e.IsTerminating}\n\n";
            File.AppendAllText(CrashLog, txt);
        }
        catch { }
    }
}
