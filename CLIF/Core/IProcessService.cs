using System.Diagnostics;

namespace CLIF.Core;

public interface IProcessService
{
    Task<List<ProcessInfo>> GetWpfProcessesAsync();
    Task<ProcessInfo?> FindProcessByNameAsync(string processName);
    Task<ProcessInfo?> FindProcessByWindowTitleAsync(string windowTitle);
    Task<ProcessInfo?> FindProcessByIdAsync(int processId);
    Task<bool> IsProcessAliveAsync(int processId);
}

public class ProcessInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public bool HasMainWindow { get; set; }
}