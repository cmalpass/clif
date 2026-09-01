// <copyright file="ProcessService.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;
using CLIF.Core;
using Microsoft.Extensions.Logging;

namespace CLIF.Services;

/// <summary>Discovers and inspects processes that expose desktop windows.</summary>
[SupportedOSPlatform("windows7.0")]
public class ProcessService : IProcessService
{
    private readonly ILogger<ProcessService> logger;

    /// <summary>Initializes a new instance of the <see cref="ProcessService"/> class.</summary>
    /// <param name="logger">Logger used to record process discovery failures.</param>
    public ProcessService(ILogger<ProcessService> logger)
    {
        this.logger = logger;
    }

    /// <summary>Gets the currently running processes that are candidates for desktop UI Automation.</summary>
    /// <returns>The discovered process information, ordered by process name.</returns>
    public async Task<List<ProcessInfo>> GetDesktopProcessesAsync()
    {
        return await Task.Run(() =>
        {
            var desktopProcesses = new List<ProcessInfo>();

            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    using (process)
                    {
                        try
                        {
                            if (!this.TryGetDesktopProcessInfo(process, out var processInfo))
                            {
                                continue;
                            }

                            desktopProcesses.Add(processInfo);
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogWarning(ex, "Failed to inspect a desktop process.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to get desktop automation candidates");
            }

            return desktopProcesses.OrderBy(p => p.Name).ToList();
        });
    }

    /// <summary>Finds a desktop automation candidate by its executable or process name.</summary>
    /// <param name="processName">The process name to match, ignoring case.</param>
    /// <returns>The matching process, or <see langword="null"/> when no process matches.</returns>
    public async Task<ProcessInfo?> FindProcessByNameAsync(string processName)
    {
        var processes = await this.GetDesktopProcessesAsync();
        return processes.FirstOrDefault(p =>
            string.Equals(p.Name, processName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Finds a desktop automation candidate by a substring of its main window title.</summary>
    /// <param name="windowTitle">The window-title text to match, ignoring case.</param>
    /// <returns>The matching process, or <see langword="null"/> when no process matches.</returns>
    public async Task<ProcessInfo?> FindProcessByWindowTitleAsync(string windowTitle)
    {
        ArgumentNullException.ThrowIfNull(windowTitle);
        if (string.IsNullOrWhiteSpace(windowTitle))
        {
            return null;
        }

        var processes = await this.GetDesktopProcessesAsync();
        return processes.FirstOrDefault(p =>
            p.WindowTitle.Contains(windowTitle, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Finds a desktop automation candidate by process identifier.</summary>
    /// <param name="processId">The process identifier to inspect.</param>
    /// <returns>The matching process, or <see langword="null"/> when it has no accessible main window or cannot be found.</returns>
    public async Task<ProcessInfo?> FindProcessByIdAsync(int processId)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                if (!this.TryGetDesktopProcessInfo(process, out var processInfo))
                {
                    return null;
                }

                return processInfo;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"Process {processId} not found: {ex.Message}");
                return null;
            }
        });
    }

    /// <summary>Checks whether a process is still running.</summary>
    /// <param name="processId">The process identifier to check.</param>
    /// <returns><see langword="true"/> when the process exists and has not exited; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> IsProcessAliveAsync(int processId)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        });
    }

    private bool TryGetDesktopProcessInfo(Process process, out ProcessInfo processInfo)
    {
        processInfo = null!;
        try
        {
            if (process.HasExited || process.MainWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            processInfo = new ProcessInfo
            {
                Id = process.Id,
                Name = process.ProcessName,
                WindowTitle = process.MainWindowTitle,
                ExecutablePath = this.GetExecutablePath(process),
                StartTime = process.StartTime,
                HasMainWindow = true,
            };
            return true;
        }
        catch
        {
            // Access-denied and exited processes are not safe attachment candidates.
            return false;
        }
    }

    private string GetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
