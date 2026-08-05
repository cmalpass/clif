namespace CLIF.Core;

/// <summary>Loads, validates, saves, and executes CLIF automation scripts.</summary>
public interface IScriptService
{
    /// <summary>Executes a script loaded from a file.</summary>
    /// <param name="scriptPath">Path to the script file.</param>
    /// <param name="processIdOverride">Optional process identifier overriding the script target.</param>
    /// <returns>The execution result.</returns>
    Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptPath, int? processIdOverride = null);
    /// <summary>Executes script content supplied as JSON.</summary>
    /// <param name="jsonContent">JSON representation of the script.</param>
    /// <param name="processIdOverride">Optional process identifier overriding the script target.</param>
    /// <returns>The execution result.</returns>
    Task<ScriptExecutionResult> ExecuteScriptContentAsync(string jsonContent, int? processIdOverride = null);
    /// <summary>Validates a script file without executing it.</summary>
    /// <param name="scriptPath">Path to the script file.</param>
    /// <returns><see langword="true"/> when the script is valid.</returns>
    Task<bool> ValidateScriptAsync(string scriptPath);
    /// <summary>Loads a script from a file.</summary>
    /// <param name="scriptPath">Path to the script file.</param>
    /// <returns>The loaded script, or <see langword="null"/> when it cannot be loaded.</returns>
    Task<Script?> LoadScriptAsync(string scriptPath);
    /// <summary>Saves a script to a file.</summary>
    /// <param name="script">Script to serialize.</param>
    /// <param name="scriptPath">Destination path.</param>
    Task SaveScriptAsync(Script script, string scriptPath);
}

/// <summary>Manages an interactive CLIF automation session.</summary>
public interface IInteractiveService
{
    /// <summary>Starts an interactive session.</summary>
    /// <param name="processId">Optional process to attach to initially.</param>
    Task StartInteractiveSessionAsync(int? processId = null);
    /// <summary>Executes one interactive command.</summary>
    /// <param name="command">Command text to execute.</param>
    /// <returns><see langword="true"/> when the command succeeds.</returns>
    Task<bool> ExecuteCommandAsync(string command);
    /// <summary>Displays interactive command help.</summary>
    Task ShowHelpAsync();
    /// <summary>Gets the current interactive prompt.</summary>
    /// <returns>The prompt text.</returns>
    Task<string> GetPromptAsync();
    /// <summary>Gets whether an interactive session is active.</summary>
    bool IsSessionActive { get; }
}

/// <summary>Represents a serializable CLIF automation script.</summary>
public class Script
{
    /// <summary>Gets or sets the script name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the script description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the script version.</summary>
    public string Version { get; set; } = "1.0";
    /// <summary>Gets or sets the process targeting settings.</summary>
    public ScriptTarget Target { get; set; } = new();
    /// <summary>Gets or sets the ordered script steps.</summary>
    public List<ScriptStep> Steps { get; set; } = new();
    /// <summary>Gets or sets variables available to script steps.</summary>
    public Dictionary<string, object> Variables { get; set; } = new();
    /// <summary>Gets or sets execution options.</summary>
    public ScriptOptions Options { get; set; } = new();
}

/// <summary>Identifies the process targeted by a script.</summary>
public class ScriptTarget
{
    /// <summary>Gets or sets the target process name.</summary>
    public string ProcessName { get; set; } = string.Empty;
    /// <summary>Gets or sets the target window title.</summary>
    public string WindowTitle { get; set; } = string.Empty;
    /// <summary>Gets or sets the target process identifier.</summary>
    public int ProcessId { get; set; }
    /// <summary>Gets or sets the target timeout in milliseconds.</summary>
    public int TimeoutMs { get; set; } = 30000;
}

/// <summary>Describes one action in an automation script.</summary>
public class ScriptStep
{
    /// <summary>Gets or sets the action name.</summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>Gets or sets the target element selector.</summary>
    public string Element { get; set; } = string.Empty;
    /// <summary>Gets or sets the primary action value.</summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>Gets or sets action-specific parameters.</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
    /// <summary>Gets or sets whether errors for this step are ignored.</summary>
    public bool IgnoreErrors { get; set; } = false;
    /// <summary>Gets or sets the delay after the step, in milliseconds.</summary>
    public int DelayMs { get; set; } = 0;
    /// <summary>Gets or sets a human-readable step description.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>Controls script execution behavior.</summary>
public class ScriptOptions
{
    /// <summary>Gets or sets whether execution stops after an error.</summary>
    public bool StopOnError { get; set; } = true;
    /// <summary>Gets or sets whether individual steps are logged.</summary>
    public bool LogSteps { get; set; } = true;
    /// <summary>Gets or sets whether screenshots are captured during execution.</summary>
    public bool TakeScreenshots { get; set; } = false;
    /// <summary>Gets or sets the default step timeout in milliseconds.</summary>
    public int DefaultTimeoutMs { get; set; } = 5000;
    /// <summary>Gets or sets the directory used for execution output.</summary>
    public string OutputPath { get; set; } = string.Empty;
}

/// <summary>Reports the outcome of script execution.</summary>
public class ScriptExecutionResult
{
    /// <summary>Gets or sets whether the script completed successfully.</summary>
    public bool Success { get; set; }
    /// <summary>Gets or sets the result message.</summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>Gets or sets the execution log entries.</summary>
    public List<string> ExecutionLog { get; set; } = new();
    /// <summary>Gets or sets values produced during execution.</summary>
    public Dictionary<string, object> Results { get; set; } = new();
    /// <summary>Gets or sets the total execution duration.</summary>
    public TimeSpan ExecutionTime { get; set; }
    /// <summary>Gets or sets the number of completed steps.</summary>
    public int StepsExecuted { get; set; }
    /// <summary>Gets or sets the number of failed steps.</summary>
    public int StepsFailed { get; set; }
}
