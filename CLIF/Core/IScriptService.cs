namespace CLIF.Core;

public interface IScriptService
{
    Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptPath, int? processIdOverride = null);
    Task<ScriptExecutionResult> ExecuteScriptContentAsync(string jsonContent, int? processIdOverride = null);
    Task<bool> ValidateScriptAsync(string scriptPath);
    Task<Script?> LoadScriptAsync(string scriptPath);
    Task SaveScriptAsync(Script script, string scriptPath);
}

public interface IInteractiveService
{
    Task StartInteractiveSessionAsync(int? processId = null);
    Task<bool> ExecuteCommandAsync(string command);
    Task ShowHelpAsync();
    Task<string> GetPromptAsync();
    bool IsSessionActive { get; }
}

public class Script
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public ScriptTarget Target { get; set; } = new();
    public List<ScriptStep> Steps { get; set; } = new();
    public Dictionary<string, object> Variables { get; set; } = new();
    public ScriptOptions Options { get; set; } = new();
}

public class ScriptTarget
{
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public int TimeoutMs { get; set; } = 30000;
}

public class ScriptStep
{
    public string Action { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IgnoreErrors { get; set; } = false;
    public int DelayMs { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
}

public class ScriptOptions
{
    public bool StopOnError { get; set; } = true;
    public bool LogSteps { get; set; } = true;
    public bool TakeScreenshots { get; set; } = false;
    public int DefaultTimeoutMs { get; set; } = 5000;
    public string OutputPath { get; set; } = string.Empty;
}

public class ScriptExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> ExecutionLog { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
    public int StepsExecuted { get; set; }
    public int StepsFailed { get; set; }
}