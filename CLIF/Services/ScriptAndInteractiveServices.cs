// <copyright file="ScriptAndInteractiveServices.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using CLIF.Core;

namespace CLIF.Services;

/// <summary>
/// Loads, validates, saves, and executes CLIF automation scripts.
/// </summary>
public class ScriptService : IScriptService
{
    private readonly ILogger<ScriptService> logger;
    private readonly IProcessService processService;
    private readonly IAutomationService automationService;
    private readonly ISessionCaptureService captureService;
    private int? attachedProcessId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScriptService"/> class.
    /// Initializes the script service.
    /// </summary>
    /// <param name="logger">Logger used for execution diagnostics.</param>
    /// <param name="processService">Service used to resolve target processes.</param>
    /// <param name="automationService">Service used to perform UI automation.</param>
    /// <param name="captureService">Service used to record execution sessions.</param>
    public ScriptService(ILogger<ScriptService> logger, IProcessService processService, IAutomationService automationService, ISessionCaptureService captureService)
    {
        this.logger = logger;
        this.processService = processService;
        this.automationService = automationService;
        this.captureService = captureService;
    }

    /// <inheritdoc />
    public async Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptPath, int? processIdOverride = null)
    {
        // Start capture session
        var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
        var sessionId = await this.captureService.StartSessionAsync($"{scriptName}_{DateTime.Now:HHmmss}");

        try
        {
            await this.captureService.LogInteractionAsync($"Starting script execution: {scriptPath}");

            if (!File.Exists(scriptPath))
            {
                await this.captureService.LogInteractionAsync($"ERROR: Script file not found: {scriptPath}", LogLevel.Error);
                await this.captureService.EndSessionAsync();
                return new ScriptExecutionResult
                {
                    Success = false,
                    Message = $"Script file not found: {scriptPath}",
                };
            }

            var content = await File.ReadAllTextAsync(scriptPath);
            return await this.ExecuteScriptContentAsync(content, processIdOverride);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error executing script file: {scriptPath}");
            await this.captureService.LogInteractionAsync($"ERROR: Error reading script file: {ex.Message}", LogLevel.Error);
            await this.captureService.EndSessionAsync();
            return new ScriptExecutionResult
            {
                Success = false,
                Message = $"Error reading script file: {ex.Message}",
            };
        }
    }



    /// <inheritdoc />
    public async Task<ScriptExecutionResult> ExecuteScriptContentAsync(string jsonContent, int? processIdOverride = null)
    {
        var result = new ScriptExecutionResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var script = JsonSerializer.Deserialize<Script>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
            if (script == null)
            {
                result.Success = false;
                result.Message = "Failed to parse script JSON";
                return result;
            }

            result.ExecutionLog.Add($"Starting script execution: {script.Name}");
            Console.WriteLine($"🎬 Executing script: {script.Name}");
            Console.WriteLine($"📋 Description: {script.Description}");

            // Attach to target process
            if (!await this.AttachToProcessAsync(script, processIdOverride))
            {
                result.Success = false;
                result.Message = "Failed to attach to target process";
                return result;
            }

            foreach (var step in script.Steps)
            {
                result.StepsExecuted++;

                try
                {
                    Console.WriteLine($"⚡ Step {script.Steps.IndexOf(step) + 1}: {step.Action} - {step.Description}");

                    // Delay regular actions, but let an explicit wait step own its full delay.
                    var delayMs = step.Action.Equals("wait", StringComparison.OrdinalIgnoreCase)
                        ? 0
                        : step.DelayMs > 0
                            ? step.DelayMs
                            : script.Options?.DelayBetweenActionsMs > 0
                                ? script.Options.DelayBetweenActionsMs
                                : 100;
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs);
                    }

                    // Execute the actual automation step with session capture
                    var success = await this.ExecuteAutomationStepAsync(step);

                    if (success)
                    {
                        result.ExecutionLog.Add($"Executed: {step.Action} on {step.Element ?? "N/A"}");
                    }
                    else
                    {
                        result.StepsFailed++;
                        result.ExecutionLog.Add($"Failed: {step.Action} on {step.Element ?? "N/A"}");
                        if (script.Options?.StopOnError == true)
                        {
                            result.Success = false;
                            result.Message = $"Step failed: {step.Action} on {step.Element}";
                            break;
                        }
                    }
                }
                catch (Exception stepEx)
                {
                    result.StepsFailed++;
                    Console.WriteLine($"❌ Step failed: {stepEx.Message}");
                    result.ExecutionLog.Add($"Step failed: {stepEx.Message}");

                    if (script.Options?.StopOnError == true)
                    {
                        throw;
                    }
                }
            }

            if (result.StepsFailed == 0)
            {
                result.Success = true;
                result.Message = "Script execution completed successfully";
                await this.captureService.LogInteractionAsync($"Script completed successfully! Executed {result.StepsExecuted} steps.");
                Console.WriteLine($"✅ Script completed successfully! Executed {result.StepsExecuted} steps.");
            }
            else
            {
                result.Success = false;
                result.Message = string.IsNullOrEmpty(result.Message)
                    ? $"Script execution completed with {result.StepsFailed} failed step(s)"
                    : result.Message;
                await this.captureService.LogInteractionAsync($"Script completed with {result.StepsFailed} failed step(s).", LogLevel.Error);
                Console.WriteLine($"❌ Script completed with {result.StepsFailed} failed step(s).");
            }

        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error executing script content");
            await this.captureService.LogInteractionAsync($"ERROR: Script execution failed: {ex.Message}", LogLevel.Error);
            result.Success = false;
            result.Message = $"Script execution failed: {ex.Message}";
            Console.WriteLine($"❌ Script execution failed: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;

            // End capture session
            await this.captureService.LogInteractionAsync($"Script execution completed in {result.ExecutionTime.TotalSeconds:F2} seconds");
            await this.captureService.EndSessionAsync();
        }

        return result;
    }

    private async Task<bool> AttachToProcessAsync(Script script, int? processIdOverride = null)
    {
        try
        {
            var target = script.Target;
            if (!processIdOverride.HasValue &&
                (target is null ||
                 (target.ProcessId <= 0 &&
                  string.IsNullOrWhiteSpace(target.ProcessName) &&
                  string.IsNullOrWhiteSpace(target.WindowTitle))))
            {
                Console.WriteLine("❌ Script target must specify a process ID, process name, or window title.");
                return false;
            }

            var processes = await this.processService.GetWpfProcessesAsync();
            var targetProcess = processes.FirstOrDefault(p =>
                (processIdOverride.HasValue && p.Id == processIdOverride.Value) ||
                (!processIdOverride.HasValue && (
                    (!string.IsNullOrWhiteSpace(target!.ProcessName) &&
                     p.Name.Equals(target.ProcessName, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(target.WindowTitle) &&
                     p.WindowTitle.Contains(target.WindowTitle, StringComparison.OrdinalIgnoreCase)) ||
                    (target.ProcessId > 0 && p.Id == target.ProcessId))));

            if (targetProcess != null)
            {
                var attached = await this.automationService.AttachToProcessAsync(targetProcess.Id);
                if (!attached)
                {
                    Console.WriteLine($"❌ Could not attach to process: {targetProcess.Name} (PID: {targetProcess.Id})");
                    return false;
                }

                this.attachedProcessId = targetProcess.Id;
                Console.WriteLine($"🔗 Attached to process: {targetProcess.Name} (PID: {targetProcess.Id})");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Could not find target process: {target?.ProcessName ?? "(unspecified)"}");
                return false;
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error attaching to process");
            Console.WriteLine($"❌ Error attaching to process: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExecuteAutomationStepAsync(ScriptStep step)
    {
        try
        {
            switch (step.Action.ToLowerInvariant())
            {
                case "log":
                    Console.WriteLine($"📝 Log: {step.Description}");
                    await this.captureService.LogInteractionAsync($"Log: {step.Description}");
                    return true;

                case "wait":
                    if (step.DelayMs > 0)
                    {
                        Console.WriteLine($"⏱️  Waiting {step.DelayMs}ms...");
                        await this.captureService.LogInteractionAsync($"Waiting {step.DelayMs}ms");
                        await Task.Delay(step.DelayMs);
                    }

                    return true;

                case "screenshot":
                    Console.WriteLine($"📸 Taking screenshot");
                    await this.captureService.CaptureAfterInteractionAsync("SCREENSHOT", step.Description ?? "Manual screenshot capture", true);
                    return true;

                case "clear":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var clearElement = await this.automationService.FindElementAsync(step.Element);
                    if (clearElement != null)
                    {
                        Console.WriteLine($"🧹 Clearing element: {step.Element}");
                        return await this.automationService.SetValueAsync(clearElement, string.Empty);
                    }

                    return false;

                case "type":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var typeElement = await this.automationService.FindElementAsync(step.Element);
                    if (typeElement != null)
                    {
                        Console.WriteLine($"⌨️  Typing '{step.Value}' into: {step.Element}");
                        return await this.automationService.TypeTextAsync(typeElement, step.Value);
                    }

                    return false;

                case "click":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var clickElement = await this.automationService.FindElementAsync(step.Element);
                    if (clickElement != null)
                    {
                        Console.WriteLine($"🖱️  Clicking: {step.Element}");
                        return await this.automationService.ClickAsync(clickElement);
                    }

                    return false;

                case "focus":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var focusElement = await this.automationService.FindElementAsync(step.Element);
                    return focusElement != null && await this.automationService.FocusAsync(focusElement);

                case "select":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var selectElement = await this.automationService.FindElementAsync(step.Element);
                    if (selectElement != null)
                    {
                        Console.WriteLine($"📋 Selecting '{step.Value}' in: {step.Element}");
                        return await this.automationService.SelectComboBoxItemAsync(selectElement, step.Value);
                    }

                    return false;

                case "setvalue":
                case "setValue":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var valueElement = await this.automationService.FindElementAsync(step.Element);
                    if (valueElement != null)
                    {
                        Console.WriteLine($"🎚️  Setting value '{step.Value}' on: {step.Element}");
                        if (double.TryParse(step.Value, out double numValue))
                        {
                            return await this.automationService.SetSliderValueAsync(valueElement, numValue);
                        }
                        else
                        {
                            return await this.automationService.SetValueAsync(valueElement, step.Value);
                        }
                    }

                    return false;

                case "selecttab":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var tabElement = await this.automationService.FindElementAsync(step.Element);
                    if (tabElement != null)
                    {
                        Console.WriteLine($"📑 Switching to tab '{step.Value}' in: {step.Element}");
                        return await this.automationService.SelectTabAsync(tabElement, step.Value);
                    }

                    return false;

                case "selectrow":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var gridElement = await this.automationService.FindElementAsync(step.Element);
                    if (gridElement != null)
                    {
                        var index = step.Parameters.ContainsKey("index") ? Convert.ToInt32(step.Parameters["index"]) : 0;
                        Console.WriteLine($"📊 Selecting row {index} in: {step.Element}");
                        return await this.automationService.SelectDataGridRowAsync(gridElement, index);
                    }

                    return false;

                case "selectcell":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var cellGridElement = await this.automationService.FindElementAsync(step.Element);
                    if (cellGridElement != null)
                    {
                        var row = step.Parameters.ContainsKey("row") ? Convert.ToInt32(step.Parameters["row"]) : step.Row;
                        var column = step.Parameters.ContainsKey("column") ? Convert.ToInt32(step.Parameters["column"]) : step.Column;
                        Console.WriteLine($"📊 Selecting cell ({row}, {column}) in: {step.Element}");
                        return await this.automationService.SelectDataGridCellAsync(cellGridElement, row, column);
                    }

                    return false;

                case "expand":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var expandElement = await this.automationService.FindElementAsync(step.Element);
                    if (expandElement != null)
                    {
                        Console.WriteLine($"📂 Expanding: {step.Element}");
                        return await this.automationService.ToggleExpanderAsync(expandElement);
                    }

                    return false;

                case "collapse":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var collapseElement = await this.automationService.FindElementAsync(step.Element);
                    if (collapseElement != null)
                    {
                        return !await this.automationService.GetExpanderStateAsync(collapseElement)
                            || await this.automationService.ToggleExpanderAsync(collapseElement);
                    }

                    return false;

                case "getvalue":
                case "getselection":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var valueReadElement = await this.automationService.FindElementAsync(step.Element);
                    if (valueReadElement != null)
                    {
                        var currentValue = await this.automationService.GetValueAsync(valueReadElement);
                        Console.WriteLine($"ℹ️  Current value for {step.Element}: {currentValue}");
                        await this.captureService.LogInteractionAsync($"Current value for {step.Element}: {currentValue}");
                        return true;
                    }

                    return false;

                case "getstate":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var stateReadElement = await this.automationService.FindElementAsync(step.Element);
                    if (stateReadElement != null)
                    {
                        var currentState = await this.automationService.GetValueAsync(stateReadElement);
                        Console.WriteLine($"ℹ️  Current state for {step.Element}: {currentState}");
                        await this.captureService.LogInteractionAsync($"Current state for {step.Element}: {currentState}");
                        return true;
                    }

                    return false;

                case "validate":
                    if (string.IsNullOrEmpty(step.Element))
                    {
                        return false;
                    }

                    var validateElement = await this.automationService.FindElementAsync(step.Element);
                    if (validateElement != null)
                    {
                        var expectedValue = step.Parameters.ContainsKey("expectedValue") ?
                            step.Parameters["expectedValue"].ToString() : step.Value;
                        Console.WriteLine($"✅ Validating '{expectedValue}' in: {step.Element}");

                        // UIA value updates can lag briefly behind a preceding keyboard input.
                        // Poll for a bounded interval so a script validates the settled value
                        // instead of racing the WPF dispatcher.
                        var actualValue = string.Empty;
                        var isValid = false;
                        for (var attempt = 0; attempt < 10; attempt++)
                        {
                            actualValue = await this.automationService.GetValueAsync(validateElement);
                            isValid = actualValue == expectedValue;
                            if (isValid)
                            {
                                break;
                            }

                            await Task.Delay(100);
                        }

                        await this.captureService.LogInteractionAsync($"Validation {(isValid ? "PASSED" : "FAILED")}: Expected '{expectedValue}', got '{actualValue}'");
                        return isValid;
                    }

                    return false;

                default:
                    Console.WriteLine($"⚠️  Unknown action: {step.Action}");
                    await this.captureService.LogInteractionAsync($"ERROR: Unknown action: {step.Action}", LogLevel.Error);
                    return false;
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error executing step: {step.Action}");
            Console.WriteLine($"❌ Error executing {step.Action}: {ex.Message}");
            await this.captureService.LogInteractionAsync($"ERROR executing {step.Action}: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateScriptAsync(string scriptPath)
    {
        try
        {
            var script = await this.LoadScriptAsync(scriptPath);
            return script != null;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error validating script: {scriptPath}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Script?> LoadScriptAsync(string scriptPath)
    {
        try
        {
            if (!File.Exists(scriptPath))
            {
                return null;
            }

            var content = await File.ReadAllTextAsync(scriptPath);
            return JsonSerializer.Deserialize<Script>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error loading script: {scriptPath}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task SaveScriptAsync(Script script, string scriptPath)
    {
        try
        {
            var json = JsonSerializer.Serialize(script, new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            await File.WriteAllTextAsync(scriptPath, json);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error saving script: {scriptPath}");
            throw;
        }
    }
}

/// <summary>
/// Runs an interactive command session for desktop automation.
/// </summary>
public class InteractiveService : IInteractiveService
{
    private readonly ILogger<InteractiveService> logger;
    private readonly IAutomationService automationService;
    private readonly IElementTreeService elementTreeService;
    private readonly ISessionCaptureService captureService;

    /// <inheritdoc />
    public bool IsSessionActive { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveService"/> class.
    /// Initializes the interactive service.
    /// </summary>
    /// <param name="logger">Logger used for command diagnostics.</param>
    /// <param name="automationService">Service used to perform UI automation.</param>
    /// <param name="elementTreeService">Service used to inspect and search UI trees.</param>
    /// <param name="captureService">Service used to record interactions.</param>
    public InteractiveService(
        ILogger<InteractiveService> logger,
        IAutomationService automationService,
        IElementTreeService elementTreeService,
        ISessionCaptureService captureService)
    {
        this.logger = logger;
        this.automationService = automationService;
        this.elementTreeService = elementTreeService;
        this.captureService = captureService;
    }

    /// <summary>
    /// Starts the interactive command mode.
    /// </summary>
    /// <param name="processId">Optional process to attach to initially.</param>
    public async Task StartInteractiveModeAsync(int? processId = null)
    {
        await this.StartInteractiveSessionAsync(processId);
    }

    /// <inheritdoc />
    public async Task StartInteractiveSessionAsync(int? processId = null)
    {
        this.IsSessionActive = true;

        Console.WriteLine("=== CLIF Interactive Mode ===");
        Console.WriteLine("Type 'help' for available commands or 'exit' to quit.");

        if (processId.HasValue)
        {
            var attached = await this.automationService.AttachToProcessAsync(processId.Value);
            if (attached)
            {
                Console.WriteLine($"✓ Attached to process: {processId}");
            }
            else
            {
                Console.WriteLine($"⚠ Failed to attach to process: {processId}");
            }
        }

        while (this.IsSessionActive)
        {
            var prompt = await this.GetPromptAsync();
            Console.Write(prompt);

            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Trim().ToLowerInvariant() == "exit")
            {
                this.IsSessionActive = false;
                break;
            }

            await this.ExecuteCommandAsync(input.Trim());
        }

        if (this.automationService.IsAttached)
        {
            await this.automationService.DetachAsync();
        }

        Console.WriteLine("Interactive session ended.");
    }

    /// <inheritdoc />
    public async Task<bool> ExecuteCommandAsync(string command)
    {
        try
        {
            var parts = this.ParseCommand(command);
            if (parts.Length == 0)
            {
                return true;
            }

            var cmd = parts[0].ToLowerInvariant();

            switch (cmd)
            {
                case "help":
                    await this.ShowHelpAsync();
                    return true;

                case "click":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Usage: click <selector>");
                        return false;
                    }

                    return await this.ExecuteClickAsync(parts[1]);

                case "type":
                    if (parts.Length < 3)
                    {
                        Console.WriteLine("Usage: type <selector> <text>");
                        return false;
                    }

                    var text = string.Join(" ", parts.Skip(2));
                    return await this.ExecuteTypeAsync(parts[1], text);

                case "get-text":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Usage: get-text <selector>");
                        return false;
                    }

                    return await this.ExecuteGetTextAsync(parts[1]);

                case "get-value":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Usage: get-value <selector>");
                        return false;
                    }

                    return await this.ExecuteGetValueAsync(parts[1]);

                case "tree":
                    var depth = 5;
                    if (parts.Length > 1 && int.TryParse(parts[1], out var d))
                    {
                        depth = d;
                    }

                    return await this.ExecuteTreeAsync(depth);

                case "search":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Usage: search <criteria>");
                        return false;
                    }

                    return await this.ExecuteSearchAsync(parts[1]);

                case "screenshot":
                    var filename = parts.Length > 1 ? parts[1] : null;
                    return await this.ExecuteScreenshotAsync(filename);

                case "attach":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Usage: attach <process-id>");
                        return false;
                    }

                    if (int.TryParse(parts[1], out var pid))
                    {
                        return await this.ExecuteAttachAsync(pid);
                    }

                    Console.WriteLine("Invalid process ID");
                    return false;

                default:
                    Console.WriteLine($"Unknown command: {cmd}. Type 'help' for available commands.");
                    return false;
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, $"Error executing interactive command: {command}");
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    private string[] ParseCommand(string command)
    {
        // Simple parsing - splits by space but respects quoted strings
        var parts = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < command.Length; i++)
        {
            var c = command[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts.ToArray();
    }

    private async Task<bool> ExecuteClickAsync(string selector)
    {
        if (!this.automationService.IsAttached)
        {
            Console.WriteLine("Not attached to any process. Use 'attach <process-id>' first.");
            return false;
        }

        var element = await this.automationService.FindElementAsync(selector);
        if (element == null)
        {
            Console.WriteLine($"Element not found: {selector}");
            return false;
        }

        var success = await this.automationService.ClickAsync(element);
        Console.WriteLine(success ? $"✓ Clicked: {selector}" : $"✗ Failed to click: {selector}");
        return success;
    }

    private async Task<bool> ExecuteTypeAsync(string selector, string text)
    {
        if (!this.automationService.IsAttached)
        {
            Console.WriteLine("Not attached to any process. Use 'attach <process-id>' first.");
            return false;
        }

        var element = await this.automationService.FindElementAsync(selector);
        if (element == null)
        {
            Console.WriteLine($"Element not found: {selector}");
            return false;
        }

        var success = await this.automationService.TypeTextAsync(element, text);
        Console.WriteLine(success ? $"✓ Typed text into: {selector}" : $"✗ Failed to type into: {selector}");
        return success;
    }

    private async Task<bool> ExecuteGetTextAsync(string selector)
    {
        if (!this.automationService.IsAttached)
        {
            Console.WriteLine("Not attached to any process. Use 'attach <process-id>' first.");
            return false;
        }

        var element = await this.automationService.FindElementAsync(selector);
        if (element == null)
        {
            Console.WriteLine($"Element not found: {selector}");
            return false;
        }

        var text = await this.automationService.GetTextAsync(element);
        Console.WriteLine($"Text: {text ?? "(empty)"}");
        return true;
    }

    private async Task<bool> ExecuteGetValueAsync(string selector)
    {
        if (!this.automationService.IsAttached)
        {
            Console.WriteLine("Not attached to any process. Use 'attach <process-id>' first.");
            return false;
        }

        var element = await this.automationService.FindElementAsync(selector);
        if (element == null)
        {
            Console.WriteLine($"Element not found: {selector}");
            return false;
        }

        var value = await this.automationService.GetValueAsync(element);
        Console.WriteLine($"Value: {value ?? "(empty)"}");
        return true;
    }

    private async Task<bool> ExecuteTreeAsync(int depth)
    {
        if (!this.automationService.IsAttached)
        {
            Console.WriteLine("Not attached to any process. Use 'attach <process-id>' first.");
            return false;
        }

        var window = await this.automationService.GetMainWindowAsync();
        if (window == null)
        {
            Console.WriteLine("Could not get main window");
            return false;
        }

        var tree = await this.elementTreeService.BuildTreeAsync(window, includeChildren: true, maxDepth: depth);
        var output = await this.elementTreeService.PrintTreeAsync(tree);
        Console.WriteLine(output);
        return true;
    }

    private async Task<bool> ExecuteSearchAsync(string criteria)
    {
        if (!this.automationService.IsAttached)
        {
            Console.WriteLine("Not attached to any process. Use 'attach <process-id>' first.");
            return false;
        }

        var window = await this.automationService.GetMainWindowAsync();
        if (window == null)
        {
            Console.WriteLine("Could not get main window");
            return false;
        }

        var tree = await this.elementTreeService.BuildTreeAsync(window, includeChildren: true, maxDepth: 10);

        // Parse criteria (e.g., "name:Button" or "id:TestButton")
        var searchCriteria = new ElementSearchCriteria();
        if (criteria.Contains(':'))
        {
            var parts = criteria.Split(':', 2);
            switch (parts[0].ToLowerInvariant())
            {
                case "name":
                    searchCriteria.Name = parts[1];
                    break;
                case "id":
                    searchCriteria.AutomationId = parts[1];
                    break;
                case "type":
                    searchCriteria.ControlType = parts[1];
                    break;
                case "class":
                    searchCriteria.ClassName = parts[1];
                    break;
            }
        }
        else
        {
            searchCriteria.Name = criteria;
        }

        var results = await this.elementTreeService.SearchTreeAsync(tree, searchCriteria);
        Console.WriteLine($"Found {results.Count} element(s):");
        foreach (var result in results)
        {
            Console.WriteLine($"  - {result.Name} ({result.ControlType}) [{result.Selector}]");
        }

        return true;
    }

    private async Task<bool> ExecuteScreenshotAsync(string? filename)
    {
        filename ??= $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        await this.captureService.CaptureAfterInteractionAsync("SCREENSHOT", filename, success: true);
        Console.WriteLine($"✓ Screenshot saved: {filename}");
        return true;
    }

    private async Task<bool> ExecuteAttachAsync(int processId)
    {
        if (this.automationService.IsAttached)
        {
            await this.automationService.DetachAsync();
        }

        var success = await this.automationService.AttachToProcessAsync(processId);
        if (success)
        {
            Console.WriteLine($"✓ Attached to process: {processId}");
        }
        else
        {
            Console.WriteLine($"✗ Failed to attach to process: {processId}");
        }

        return success;
    }

    /// <inheritdoc />
    public async Task ShowHelpAsync()
    {
        await Task.Run(() =>
        {
            Console.WriteLine();
            Console.WriteLine("Available Commands:");
            Console.WriteLine("  click <selector>          - Click an element");
            Console.WriteLine("  type <selector> <text>    - Type text into an element");
            Console.WriteLine("  get-text <selector>       - Get text from an element");
            Console.WriteLine("  get-value <selector>      - Get value from an element");
            Console.WriteLine("  tree [depth]              - Show element tree");
            Console.WriteLine("  search <criteria>         - Search for elements");
            Console.WriteLine("  screenshot [file]         - Take screenshot");
            Console.WriteLine("  attach <process>          - Attach to different process");
            Console.WriteLine("  help                      - Show this help");
            Console.WriteLine("  exit                      - Exit interactive mode");
            Console.WriteLine();
        });
    }

    /// <inheritdoc />
    public async Task<string> GetPromptAsync()
    {
        return await Task.FromResult("CLIF> ");
    }
}

// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
