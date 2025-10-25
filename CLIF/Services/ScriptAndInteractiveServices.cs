using Microsoft.Extensions.Logging;
using CLIF.Core;
using Newtonsoft.Json;

namespace CLIF.Services;

public class ScriptService : IScriptService
{
    private readonly ILogger<ScriptService> _logger;
    private readonly IProcessService _processService;
    private readonly IAutomationService _automationService;
    private readonly ISessionCaptureService _captureService;
    private int? _attachedProcessId;

    public ScriptService(ILogger<ScriptService> logger, IProcessService processService, IAutomationService automationService, ISessionCaptureService captureService)
    {
        _logger = logger;
        _processService = processService;
        _automationService = automationService;
        _captureService = captureService;
    }

    public async Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptPath, int? processIdOverride = null)
    {
        // Start capture session
        var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
        var sessionId = await _captureService.StartSessionAsync($"{scriptName}_{DateTime.Now:HHmmss}");
        
        try
        {
            await _captureService.LogInteractionAsync($"Starting script execution: {scriptPath}");
            
            if (!File.Exists(scriptPath))
            {
                await _captureService.LogInteractionAsync($"ERROR: Script file not found: {scriptPath}", LogLevel.Error);
                await _captureService.EndSessionAsync();
                return new ScriptExecutionResult
                {
                    Success = false,
                    Message = $"Script file not found: {scriptPath}"
                };
            }

            var content = await File.ReadAllTextAsync(scriptPath);
            return await ExecuteScriptContentAsync(content, processIdOverride);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing script file: {scriptPath}");
            await _captureService.LogInteractionAsync($"ERROR: Error reading script file: {ex.Message}", LogLevel.Error);
            await _captureService.EndSessionAsync();
            return new ScriptExecutionResult
            {
                Success = false,
                Message = $"Error reading script file: {ex.Message}"
            };
        }
    }



    public async Task<ScriptExecutionResult> ExecuteScriptContentAsync(string jsonContent, int? processIdOverride = null)
    {
        var result = new ScriptExecutionResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var script = JsonConvert.DeserializeObject<Script>(jsonContent);
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
            if (!await AttachToProcessAsync(script, processIdOverride))
            {
                result.Success = false;
                result.Message = "Failed to attach to target process";
                return result;
            }
            
            foreach (var step in script.Steps)
            {
                try
                {
                    Console.WriteLine($"⚡ Step {script.Steps.IndexOf(step) + 1}: {step.Action} - {step.Description}");
                    
                    // Add realistic delay between actions
                    if (step.DelayMs > 0)
                    {
                        await Task.Delay(step.DelayMs);
                    }
                    else
                    {
                        await Task.Delay(100); // Default small delay
                    }

                    // Execute the actual automation step with session capture
                    var success = await ExecuteAutomationStepAsync(step);
                    
                    if (success)
                    {
                        result.ExecutionLog.Add($"Executed: {step.Action} on {step.Element ?? "N/A"}");
                    }
                    else
                    {
                        result.ExecutionLog.Add($"Failed: {step.Action} on {step.Element ?? "N/A"}");
                        if (script.Options?.StopOnError == true)
                        {
                            throw new InvalidOperationException($"Step failed: {step.Action} on {step.Element}");
                        }
                    }
                    
                    result.StepsExecuted++;
                }
                catch (Exception stepEx)
                {
                    Console.WriteLine($"❌ Step failed: {stepEx.Message}");
                    result.ExecutionLog.Add($"Step failed: {stepEx.Message}");
                    
                    if (script.Options?.StopOnError == true)
                    {
                        throw;
                    }
                }
            }

            result.Success = true;
            result.Message = "Script execution completed successfully";
            await _captureService.LogInteractionAsync($"Script completed successfully! Executed {result.StepsExecuted} steps.");
            Console.WriteLine($"✅ Script completed successfully! Executed {result.StepsExecuted} steps.");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing script content");
            await _captureService.LogInteractionAsync($"ERROR: Script execution failed: {ex.Message}", LogLevel.Error);
            result.Success = false;
            result.Message = $"Script execution failed: {ex.Message}";
            Console.WriteLine($"❌ Script execution failed: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.ExecutionTime = stopwatch.Elapsed;
            
            // End capture session
            await _captureService.LogInteractionAsync($"Script execution completed in {result.ExecutionTime.TotalSeconds:F2} seconds");
            await _captureService.EndSessionAsync();
        }

        return result;
    }

    private async Task<bool> AttachToProcessAsync(Script script, int? processIdOverride = null)
    {
        try
        {
            var processes = await _processService.GetWpfProcessesAsync();
            var targetProcess = processes.FirstOrDefault(p => 
                (processIdOverride.HasValue && p.Id == processIdOverride.Value) ||
                (!processIdOverride.HasValue && (
                    p.Name.Equals(script.Target.ProcessName, StringComparison.OrdinalIgnoreCase) ||
                    p.WindowTitle.Contains(script.Target.WindowTitle, StringComparison.OrdinalIgnoreCase) ||
                    p.Id == script.Target.ProcessId)));

            if (targetProcess != null)
            {
                _attachedProcessId = targetProcess.Id;
                await _automationService.AttachToProcessAsync(targetProcess.Id);
                Console.WriteLine($"🔗 Attached to process: {targetProcess.Name} (PID: {targetProcess.Id})");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Could not find target process: {script.Target.ProcessName}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attaching to process");
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
                    await _captureService.LogInteractionAsync($"Log: {step.Description}");
                    return true;

                case "wait":
                    if (step.DelayMs > 0)
                    {
                        Console.WriteLine($"⏱️  Waiting {step.DelayMs}ms...");
                        await _captureService.LogInteractionAsync($"Waiting {step.DelayMs}ms");
                        await Task.Delay(step.DelayMs);
                    }
                    return true;

                case "screenshot":
                    Console.WriteLine($"📸 Taking screenshot");
                    await _captureService.CaptureAfterInteractionAsync("SCREENSHOT", step.Description ?? "Manual screenshot capture", true);
                    return true;

                case "clear":
                    if (string.IsNullOrEmpty(step.Element)) return false;
                    var clearElement = await _automationService.FindElementAsync(step.Element);
                    if (clearElement != null)
                    {
                        Console.WriteLine($"🧹 Clearing element: {step.Element}");
                        return await _automationService.SetValueAsync(clearElement, "");
                    }
                    return false;

                case "type":
                    if (string.IsNullOrEmpty(step.Element)) return false;
                    var typeElement = await _automationService.FindElementAsync(step.Element);
                    if (typeElement != null)
                    {
                        Console.WriteLine($"⌨️  Typing '{step.Value}' into: {step.Element}");
                        return await _automationService.TypeTextAsync(typeElement, step.Value);
                    }
                    return false;

                case "click":
                    if (string.IsNullOrEmpty(step.Element)) return false;
                    var clickElement = await _automationService.FindElementAsync(step.Element);
                    if (clickElement != null)
                    {
                        Console.WriteLine($"🖱️  Clicking: {step.Element}");
                        return await _automationService.ClickAsync(clickElement);
                    }
                    return false;

                case "select":
                    if (string.IsNullOrEmpty(step.Element)) return false;
                    var selectElement = await _automationService.FindElementAsync(step.Element);
                    if (selectElement != null)
                    {
                        Console.WriteLine($"📋 Selecting '{step.Value}' in: {step.Element}");
                        return await _automationService.SelectComboBoxItemAsync(selectElement, step.Value);
                    }
                    return false;

                case "setvalue":
                case "setValue":
                    if (string.IsNullOrEmpty(step.Element)) return false;
                    var valueElement = await _automationService.FindElementAsync(step.Element);
                    if (valueElement != null)
                    {
                        Console.WriteLine($"🎚️  Setting value '{step.Value}' on: {step.Element}");
                        if (double.TryParse(step.Value, out double numValue))
                        {
                            return await _automationService.SetSliderValueAsync(valueElement, numValue);
                        }
                        else
                        {
                            return await _automationService.SetValueAsync(valueElement, step.Value);
                        }
                    }
                    return false;

                case "selecttab":
                    if (string.IsNullOrEmpty(step.Element)) return false;
                    var tabElement = await _automationService.FindElementAsync(step.Element);
                    if (tabElement != null)
                    {
                        Console.WriteLine($"📑 Switching to tab '{step.Value}' in: {step.Element}");
                        return await _automationService.SelectTabAsync(tabElement, step.Value);
                    }
                    return false;

                case "selectrow":
                    if (string.IsNullOrEmpty(step.Element)) return false;
                    var gridElement = await _automationService.FindElementAsync(step.Element);
                    if (gridElement != null)
                    {
                        var index = step.Parameters.ContainsKey("index") ? Convert.ToInt32(step.Parameters["index"]) : 0;
                        Console.WriteLine($"📊 Selecting row {index} in: {step.Element}");
                        return await _automationService.SelectDataGridRowAsync(gridElement, index);
                    }
                    return false;

                case "expand":
                    if (string.IsNullOrEmpty(step.Element)) return false;
                    var expandElement = await _automationService.FindElementAsync(step.Element);
                    if (expandElement != null)
                    {
                        Console.WriteLine($"📂 Expanding: {step.Element}");
                        return await _automationService.ToggleExpanderAsync(expandElement);
                    }
                    return false;

                case "validate":
                    if (string.IsNullOrEmpty(step.Element)) return false;
                    var validateElement = await _automationService.FindElementAsync(step.Element);
                    if (validateElement != null)
                    {
                        var expectedValue = step.Parameters.ContainsKey("expectedValue") ? 
                            step.Parameters["expectedValue"].ToString() : step.Value;
                        Console.WriteLine($"✅ Validating '{expectedValue}' in: {step.Element}");
                        var actualValue = await _automationService.GetTextAsync(validateElement);
                        var isValid = actualValue == expectedValue;
                        await _captureService.LogInteractionAsync($"Validation {(isValid ? "PASSED" : "FAILED")}: Expected '{expectedValue}', got '{actualValue}'");
                        return isValid;
                    }
                    return false;

                default:
                    Console.WriteLine($"⚠️  Unknown action: {step.Action}");
                    await _captureService.LogInteractionAsync($"Unknown action: {step.Action}");
                    return true; // Don't fail on unknown actions
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing step: {step.Action}");
            Console.WriteLine($"❌ Error executing {step.Action}: {ex.Message}");
            await _captureService.LogInteractionAsync($"ERROR executing {step.Action}: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    public async Task<bool> ValidateScriptAsync(string scriptPath)
    {
        try
        {
            var script = await LoadScriptAsync(scriptPath);
            return script != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error validating script: {scriptPath}");
            return false;
        }
    }

    public async Task<Script?> LoadScriptAsync(string scriptPath)
    {
        try
        {
            if (!File.Exists(scriptPath))
                return null;

            var content = await File.ReadAllTextAsync(scriptPath);
            return JsonConvert.DeserializeObject<Script>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error loading script: {scriptPath}");
            return null;
        }
    }

    public async Task SaveScriptAsync(Script script, string scriptPath)
    {
        try
        {
            var json = JsonConvert.SerializeObject(script, Formatting.Indented);
            await File.WriteAllTextAsync(scriptPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving script: {scriptPath}");
            throw;
        }
    }
}

public class InteractiveService : IInteractiveService
{
    private readonly ILogger<InteractiveService> _logger;
    
    public bool IsSessionActive { get; private set; }

    public InteractiveService(ILogger<InteractiveService> logger)
    {
        _logger = logger;
    }

    public async Task StartInteractiveModeAsync(int? processId = null)
    {
        await StartInteractiveSessionAsync(processId);
    }

    public async Task StartInteractiveSessionAsync(int? processId = null)
    {
        IsSessionActive = true;
        Console.WriteLine("=== CLIF Interactive Mode ===");
        Console.WriteLine("Type 'help' for available commands or 'exit' to quit.");
        
        if (processId.HasValue)
        {
            Console.WriteLine($"Attached to process: {processId}");
        }

        while (IsSessionActive)
        {
            var prompt = await GetPromptAsync();
            Console.Write(prompt);
            
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Trim().ToLowerInvariant() == "exit")
            {
                IsSessionActive = false;
                break;
            }

            await ExecuteCommandAsync(input.Trim());
        }

        Console.WriteLine("Interactive session ended.");
    }

    public async Task<bool> ExecuteCommandAsync(string command)
    {
        try
        {
            if (command.ToLowerInvariant() == "help")
            {
                await ShowHelpAsync();
                return true;
            }

            // TODO: Implement command parsing and execution
            Console.WriteLine($"Command '{command}' - implementation coming soon!");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing interactive command: {command}");
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

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

    public async Task<string> GetPromptAsync()
    {
        return await Task.FromResult("CLIF> ");
    }
}