using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Diagnostics;
using System.IO;

namespace CLIF.Tests.Utilities;

/// <summary>
/// Provides helper methods and utilities for testing
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a mock logger for the specified type
    /// </summary>
    /// <typeparam name="T">The type to create a logger for</typeparam>
    /// <returns>A mock logger</returns>
    public static Mock<ILogger<T>> CreateMockLogger<T>() where T : class
    {
        var mock = new Mock<ILogger<T>>();

        // Setup common logging methods to avoid null reference exceptions
        mock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        return mock;
    }

    /// <summary>
    /// Creates a test service provider with common test services
    /// </summary>
    /// <param name="configureServices">Optional action to configure additional services</param>
    /// <returns>A configured service provider</returns>
    public static IServiceProvider CreateTestServiceProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add default test services
        services.AddTransient<TestDataBuilder>();
        services.AddTransient<MockFactory>();

        // Allow custom service configuration
        configureServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Starts a test process and waits for it to be ready
    /// </summary>
    /// <param name="applicationPath">Path to the application to start</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    /// <returns>The started process</returns>
    public static async Task<Process> StartTestProcessAsync(string applicationPath, int timeoutMs = 10000)
    {
        if (!File.Exists(applicationPath))
        {
            throw new FileNotFoundException($"Test application not found: {applicationPath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = applicationPath,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Normal,
            CreateNoWindow = false
        };

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start test process");
        }

        // Wait for the process to be ready
        await WaitForProcessReady(process, timeoutMs);

        return process;
    }

    /// <summary>
    /// Waits for a process to be ready (have a main window)
    /// </summary>
    /// <param name="process">The process to wait for</param>
    /// <param name="timeoutMs">Timeout in milliseconds</param>
    private static async Task WaitForProcessReady(Process process, int timeoutMs)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                {
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // Process might have exited
                break;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Process failed to become ready within {timeoutMs}ms");
    }

    /// <summary>
    /// Safely kills a process
    /// </summary>
    /// <param name="process">The process to kill</param>
    public static void SafeKillProcess(Process? process)
    {
        if (process == null || process.HasExited)
            return;

        try
        {
            process.Kill();
            process.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error killing process: {ex.Message}");
        }
        finally
        {
            process?.Dispose();
        }
    }

    /// <summary>
    /// Gets the absolute path to test data
    /// </summary>
    /// <param name="relativePath">Relative path from test data directory</param>
    /// <returns>Absolute path to test data</returns>
    public static string GetTestDataPath(string relativePath)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDirectory, "TestData", relativePath);
    }

    /// <summary>
    /// Gets a temporary file path
    /// </summary>
    /// <param name="extension">File extension</param>
    /// <returns>Temporary file path</returns>
    public static string GetTempFilePath(string extension = ".tmp")
    {
        return Path.Combine(Path.GetTempPath(), $"clif_test_{Guid.NewGuid()}{extension}");
    }

    /// <summary>
    /// Creates a temporary file with content
    /// </summary>
    /// <param name="content">File content</param>
    /// <param name="extension">File extension</param>
    /// <returns>Path to created file</returns>
    public static async Task<string> CreateTempFileAsync(string content, string extension = ".tmp")
    {
        var filePath = GetTempFilePath(extension);
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Cleans up a temporary file
    /// </summary>
    /// <param name="filePath">Path to file to cleanup</param>
    public static void CleanupTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error cleaning up temp file {filePath}: {ex.Message}");
        }
    }
}