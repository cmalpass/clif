using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System.IO;

namespace CLIF.Tests.WpfUI;

/// <summary>
/// Declares the xUnit collection that shares a single <see cref="WpfTestAppFixture"/>
/// across all WPF UI integration tests, ensuring they run sequentially on one window.
/// </summary>
[CollectionDefinition("WpfUI")]
public class WpfUiCollection : ICollectionFixture<WpfTestAppFixture> { }

/// <summary>
/// xUnit class fixture that launches <c>TestWpfApp.exe</c> once for the entire
/// <c>"WpfUI"</c> test collection and exposes the running FlaUI application handle.
/// </summary>
/// <remarks>
/// The fixture discovers the executable by:
/// <list type="number">
///   <item>Checking the <c>TEST_WPF_APP_PATH</c> environment variable (CI override).</item>
///   <item>
///     Walking up four directories from the test-assembly output folder and looking
///     for the TestWpfApp binary in both the <c>Release</c> and <c>Debug</c>
///     configurations.
///   </item>
/// </list>
/// If the binary cannot be found or the launch fails the fixture marks itself as
/// unavailable; individual tests call <see cref="SkipIfUnavailable"/> which throws an
/// exception whose message starts with the xUnit dynamic-skip token
/// (<c>"$XunitDynamicSkip$"</c>) so the xUnit 2.x runner reports them as
/// <em>Skipped</em> rather than silently passing.
/// </remarks>
public sealed class WpfTestAppFixture : IDisposable
{
    /// <summary>Gets the FlaUI application wrapper, or <c>null</c> when unavailable.</summary>
    public Application? App { get; private set; }

    /// <summary>Gets the UIA3 automation instance, or <c>null</c> when unavailable.</summary>
    public UIA3Automation? Automation { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the TestWpfApp was started successfully and
    /// is ready for UI automation.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Gets a human-readable description of why the app is unavailable, or
    /// <c>null</c> when <see cref="IsAvailable"/> is <c>true</c>.
    /// </summary>
    public string? UnavailableReason { get; private set; }

    /// <summary>Initialises the fixture and attempts to start the TestWpfApp.</summary>
    public WpfTestAppFixture()
    {
        TryStart();
    }

    private void TryStart()
    {
        var exePath = ResolveTestWpfAppPath();
        if (exePath is null)
        {
            UnavailableReason = "TestWpfApp.exe not found. Build the solution first, or set the TEST_WPF_APP_PATH environment variable.";
            return;
        }

        try
        {
            Automation = new UIA3Automation();
            App = Application.Launch(exePath);
            App.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(15));
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            UnavailableReason = $"Failed to launch TestWpfApp ('{exePath}'): {ex.GetType().Name}: {ex.Message}";
            Cleanup();
        }
    }

    private static string? ResolveTestWpfAppPath()
    {
        // Allow CI or a developer to pin an explicit path via the TEST_WPF_APP_PATH
        // environment variable.  In .runsettings this can be set under <EnvironmentVariables>.
        var envPath = Environment.GetEnvironmentVariable("TEST_WPF_APP_PATH");
        if (envPath is not null && File.Exists(envPath))
        {
            return envPath;
        }

        // Navigate from the test assembly output directory to the TestWpfApp binary.
        //   Test assembly : clif/CLIF.Tests/bin/{config}/net8.0-windows/
        //   TestWpfApp    : clif/TestWpfApp/bin/{config}/net8.0-windows/TestWpfApp.exe
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configs = new[] { "Release", "Debug" };
        foreach (var config in configs)
        {
            var candidate = Path.GetFullPath(
                Path.Combine(
                    baseDir,
                    "..", "..", "..", "..",
                    "TestWpfApp", "bin", config, "net8.0-windows", "TestWpfApp.exe"));

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Throws an exception recognised by the xUnit 2.x runner as a skip signal when the
    /// TestWpfApp could not be launched, so the test is reported as <em>Skipped</em>
    /// rather than passing silently.  Call this at the start of every test.
    /// </summary>
    public void SkipIfUnavailable()
    {
        if (!IsAvailable)
        {
            // Prefixing the exception message with "$XunitDynamicSkip$" is the
            // xUnit 2.x contract for runtime-conditional skipping (DynamicSkipToken.Value
            // is internal so we use the literal string). The runner strips the prefix
            // and uses the remainder as the skip reason.
            throw new Exception(
                "$XunitDynamicSkip$"
                + (UnavailableReason ?? "TestWpfApp is not available."));
        }
    }

    /// <summary>
    /// Returns the main window of the running TestWpfApp.
    /// Callers should only invoke this when <see cref="IsAvailable"/> is <c>true</c>.
    /// </summary>
    public Window GetMainWindow() => App!.GetMainWindow(Automation!);

    private void Cleanup()
    {
        try { App?.Close(); }
        catch { /* ignore – process may already be gone */ }

        try { Automation?.Dispose(); }
        catch { /* ignore */ }

        App = null;
        Automation = null;
        IsAvailable = false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }
}
