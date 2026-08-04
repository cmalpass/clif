using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System.IO;

namespace CLIF.Tests.CrossPlatformUI;

[CollectionDefinition("CrossPlatformUI")]
public sealed class CrossPlatformUiCollection : ICollectionFixture<CrossPlatformAppFixture>
{
}

/// <summary>
/// Launches the Avalonia fixture through the Windows UI Automation provider.
/// The fixture remains skippable so non-Windows test hosts can still run the
/// rest of the test assembly.
/// </summary>
public sealed class CrossPlatformAppFixture : IDisposable
{
    public Application? App { get; private set; }

    public UIA3Automation? Automation { get; private set; }

    public bool IsAvailable { get; private set; }

    public string? UnavailableReason { get; private set; }

    public CrossPlatformAppFixture()
    {
        TryStart();
    }

    public void SkipIfUnavailable()
    {
        if (!IsAvailable)
        {
            throw new Exception(
                "$XunitDynamicSkip$"
                + (UnavailableReason ?? "TestCrossPlatformApp is not available."));
        }
    }

    public Window GetMainWindow()
    {
        SkipIfUnavailable();
        return App!.GetMainWindow(Automation!)!;
    }

    public void Dispose()
    {
        try
        {
            App?.Close();
        }
        catch
        {
            // The process may already have exited.
        }

        try
        {
            Automation?.Dispose();
        }
        catch
        {
            // The automation provider may already be disconnected.
        }

        App = null;
        Automation = null;
        IsAvailable = false;
        GC.SuppressFinalize(this);
    }

    private void TryStart()
    {
        var executablePath = ResolveExecutablePath();
        if (executablePath is null)
        {
            UnavailableReason =
                "TestCrossPlatformApp.exe was not found. Build the fixture or set TEST_CROSS_PLATFORM_APP_PATH.";
            return;
        }

        try
        {
            Automation = new UIA3Automation();
            App = Application.Launch(executablePath);
            App.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(15));
            IsAvailable = App.MainWindowHandle != IntPtr.Zero;

            if (!IsAvailable)
            {
                UnavailableReason = "TestCrossPlatformApp launched without a main window.";
                Dispose();
            }
        }
        catch (Exception ex)
        {
            UnavailableReason =
                $"Failed to launch TestCrossPlatformApp ('{executablePath}'): {ex.GetType().Name}: {ex.Message}";
            Dispose();
        }
    }

    private static string? ResolveExecutablePath()
    {
        var environmentPath = Environment.GetEnvironmentVariable("TEST_CROSS_PLATFORM_APP_PATH");
        if (!string.IsNullOrWhiteSpace(environmentPath) && File.Exists(environmentPath))
        {
            return environmentPath;
        }

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var configurations = new[] { "Release", "Debug" };

        foreach (var configuration in configurations)
        {
            var candidate = Path.GetFullPath(
                Path.Combine(
                    baseDirectory,
                    "..", "..", "..", "..",
                    "TestCrossPlatformApp", "bin", configuration, "net8.0", "TestCrossPlatformApp.exe"));

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
