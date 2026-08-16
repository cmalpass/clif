using System.Diagnostics;

namespace CLIF.Core;

/// <summary>Discovers and inspects desktop processes that expose WPF windows.</summary>
public interface IProcessService
{
    /// <summary>Gets processes that appear to host WPF applications.</summary>
    /// <returns>The discovered process information.</returns>
    Task<List<ProcessInfo>> GetWpfProcessesAsync();
    /// <summary>Finds a process by executable or process name.</summary>
    /// <param name="processName">Name to match.</param>
    /// <returns>The matching process, or <see langword="null"/> when none is found.</returns>
    Task<ProcessInfo?> FindProcessByNameAsync(string processName);
    /// <summary>Finds a process by its main window title.</summary>
    /// <param name="windowTitle">Window title to match.</param>
    /// <returns>The matching process, or <see langword="null"/> when none is found.</returns>
    Task<ProcessInfo?> FindProcessByWindowTitleAsync(string windowTitle);
    /// <summary>Finds a process by identifier.</summary>
    /// <param name="processId">Process identifier to inspect.</param>
    /// <returns>The matching process, or <see langword="null"/> when none is found.</returns>
    Task<ProcessInfo?> FindProcessByIdAsync(int processId);
    /// <summary>Checks whether a process is still running.</summary>
    /// <param name="processId">Process identifier to check.</param>
    /// <returns><see langword="true"/> when the process is alive.</returns>
    Task<bool> IsProcessAliveAsync(int processId);
}

/// <summary>Describes a discovered desktop process.</summary>
public class ProcessInfo
{
    /// <summary>Gets or sets the process identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the process name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the main window title.</summary>
    public string WindowTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the executable path.</summary>
    public string ExecutablePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the process start time.</summary>
    public DateTime StartTime { get; set; }
    /// <summary>Gets or sets whether the process has a main window.</summary>
    public bool HasMainWindow { get; set; }
}
