// Licensed under the MIT License.

using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// Verifies lifecycle safeguards that can be tested without launching a real fixture.
/// </summary>
public sealed class WindowSessionManagerTests
{
    [Fact]
    public void LaunchApp_CanceledBeforeStart_StopsBeforeSpawningProcess()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var manager = new WindowSessionManager();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            manager.LaunchApp("definitely-not-a-real-application.exe", cancellationToken: cancellation.Token));
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var manager = new WindowSessionManager();
        manager.Dispose();

        manager.Dispose();
    }
}
