using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Logging;
using CLIF.Core;

namespace CLIF.Services;

public class ProcessService : IProcessService
{
    private readonly ILogger<ProcessService> _logger;

    public ProcessService(ILogger<ProcessService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ProcessInfo>> GetWpfProcessesAsync()
    {
        return await Task.Run(() =>
        {
            var wpfProcesses = new List<ProcessInfo>();
            
            try
            {
                var processes = Process.GetProcesses();
                
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.HasExited || process.MainWindowHandle == IntPtr.Zero)
                            continue;

                        // Check if it's a .NET/WPF application by looking at loaded modules
                        if (IsWpfProcess(process))
                        {
                            wpfProcesses.Add(new ProcessInfo
                            {
                                Id = process.Id,
                                Name = process.ProcessName,
                                WindowTitle = process.MainWindowTitle,
                                ExecutablePath = GetExecutablePath(process),
                                StartTime = process.StartTime,
                                HasMainWindow = process.MainWindowHandle != IntPtr.Zero
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to process {process.ProcessName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get WPF processes");
            }
            
            return wpfProcesses.OrderBy(p => p.Name).ToList();
        });
    }

    public async Task<ProcessInfo?> FindProcessByNameAsync(string processName)
    {
        var processes = await GetWpfProcessesAsync();
        return processes.FirstOrDefault(p => 
            string.Equals(p.Name, processName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ProcessInfo?> FindProcessByWindowTitleAsync(string windowTitle)
    {
        var processes = await GetWpfProcessesAsync();
        return processes.FirstOrDefault(p => 
            p.WindowTitle.Contains(windowTitle, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ProcessInfo?> FindProcessByIdAsync(int processId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (process.HasExited || !IsWpfProcess(process))
                    return null;

                return new ProcessInfo
                {
                    Id = process.Id,
                    Name = process.ProcessName,
                    WindowTitle = process.MainWindowTitle,
                    ExecutablePath = GetExecutablePath(process),
                    StartTime = process.StartTime,
                    HasMainWindow = process.MainWindowHandle != IntPtr.Zero
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Process {processId} not found: {ex.Message}");
                return null;
            }
        });
    }

    public async Task<bool> IsProcessAliveAsync(int processId)
    {
        return await Task.Run(() =>
        {
            try
            {
                var process = Process.GetProcessById(processId);
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        });
    }

    private bool IsWpfProcess(Process process)
    {
        try
        {
            // Check for common WPF/Windows Forms indicators
            var modules = process.Modules;
            foreach (ProcessModule module in modules)
            {
                var moduleName = module.ModuleName.ToLowerInvariant();
                if (moduleName.Contains("presentationframework") || 
                    moduleName.Contains("presentationcore") ||
                    moduleName.Contains("windowsbase") ||
                    moduleName.Contains("system.windows.forms"))
                {
                    return true;
                }
            }
            
            // If we can't check modules, assume it's a GUI app if it has a main window
            return process.MainWindowHandle != IntPtr.Zero;
        }
        catch
        {
            // If we can't access modules (permissions), check if it has a window
            return process.MainWindowHandle != IntPtr.Zero;
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