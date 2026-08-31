// Licensed under the MIT License.

using System.Text.Json;
using System.Diagnostics;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using CLIF.Mcp.Core;
using CLIF.Mcp.Security;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to execute multiple actions in a single call for better performance.
/// </summary>
public class BatchTool : ToolBase
{
    private readonly WindowSessionManager _sessionManager;
    private readonly ElementRegistry _elementRegistry;

    /// <summary>
    /// Initializes the batch tool.
    /// </summary>
    /// <param name="sessionManager">Manager used to resolve target windows.</param>
    /// <param name="elementRegistry">Registry used to resolve element references.</param>
    public BatchTool(WindowSessionManager sessionManager, ElementRegistry elementRegistry)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
    }

    /// <inheritdoc />
    public override string Name => "clif_batch";

    /// <inheritdoc />
    public override McpCapability RequiredCapability => McpCapability.Input;

    /// <inheritdoc />
    public override string Description =>
        "Execute multiple actions in a single call. Much faster than individual calls. " +
        "Supports click, type, fill, wait, and snapshot actions. Returns results for each action.";

    /// <inheritdoc />
    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            actions = new
            {
                type = "array",
                minItems = 1,
                maxItems = McpSafetyPolicy.MaximumBatchActions,
                description = "List of actions to execute in order",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        action = new
                        {
                            type = "string",
                            @enum = new[] { "click", "type", "fill", "wait", "snapshot" },
                            description = "Action type",
                        },
                        @ref = new
                        {
                            type = "string",
                            description = "Element ref required for click, type, and fill actions",
                        },
                        text = new
                        {
                            type = "string",
                            minLength = 1,
                            description = "Text for type action",
                        },
                        value = new
                        {
                            type = "string",
                            description = "Value for fill action",
                        },
                        ms = new
                        {
                            type = "integer",
                            minimum = 0,
                            maximum = McpSafetyPolicy.MaximumWaitMilliseconds,
                            description = "Milliseconds for wait action (default: 100)",
                        },
                        handle = new
                        {
                            type = "string",
                            description = "Registered window handle required for snapshot actions",
                        },
                        submit = new
                        {
                            type = "boolean",
                            description = "Press Enter after typing (for type action)",
                        },
                    },
                    required = new[] { "action" },
                },
            },
            stopOnError = new
            {
                type = "boolean",
                description = "Stop executing if an action fails (default: true)",
            },
        },
        required = new[] { "actions" },
    };

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        => ExecuteCoreAsync(arguments, CancellationToken.None);

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken)
        => ExecuteCoreAsync(arguments, cancellationToken);

    private async Task<McpToolResult> ExecuteCoreAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken)
    {
        if (arguments == null || arguments.Value.ValueKind != JsonValueKind.Object ||
            !arguments.Value.TryGetProperty("actions", out var actionsElement))
        {
            return ErrorResult("Missing required argument: actions");
        }

        if (actionsElement.ValueKind != JsonValueKind.Array)
        {
            return ErrorResult("Argument 'actions' must be an array.");
        }

        var stopOnError = true;
        if (arguments.Value.TryGetProperty("stopOnError", out var stopProp))
        {
            if (stopProp.ValueKind != JsonValueKind.True && stopProp.ValueKind != JsonValueKind.False)
            {
                return ErrorResult("Argument 'stopOnError' must be a boolean.");
            }

            stopOnError = stopProp.GetBoolean();
        }

        var results = new List<string>();
        var actions = actionsElement.EnumerateArray().ToList();
        if (actions.Count == 0)
        {
            return ErrorResult("At least one batch action is required.");
        }

        if (actions.Count > McpSafetyPolicy.MaximumBatchActions)
        {
            return ErrorResult($"Batch exceeds the maximum of {McpSafetyPolicy.MaximumBatchActions} actions.");
        }

        var failed = false;
        var stopwatch = Stopwatch.StartNew();

        foreach (var (actionObj, index) in actions.Select((a, i) => (a, i)))
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (stopwatch.ElapsedMilliseconds > McpSafetyPolicy.MaximumBatchDurationMilliseconds)
                {
                    return ErrorResult($"Batch exceeds the maximum duration of {McpSafetyPolicy.MaximumBatchDurationMilliseconds}ms.");
                }

                if (actionObj.ValueKind != JsonValueKind.Object || !actionObj.TryGetProperty("action", out var actionProperty) ||
                    actionProperty.ValueKind != JsonValueKind.String)
                {
                    results.Add($"{index + 1}. ERROR: Each action must be an object with a string 'action' property.");
                    failed = true;
                    if (stopOnError)
                    {
                        results.Add($"Stopped at action {index + 1} due to error");
                        break;
                    }

                    continue;
                }

                var actionType = actionProperty.GetString();
                var result = actionType switch
                {
                    "click" => ExecuteClick(actionObj),
                    "type" => await ExecuteTypeAsync(actionObj, cancellationToken),
                    "fill" => await ExecuteFillAsync(actionObj, cancellationToken),
                    "wait" => await ExecuteWaitAsync(actionObj, cancellationToken),
                    "snapshot" => ExecuteSnapshot(actionObj),
                    _ => $"Unknown action: {actionType}",
                };
                results.Add($"{index + 1}. {actionType}: {result}");
                if (IsActionFailure(result))
                {
                    failed = true;
                    if (stopOnError)
                    {
                        results.Add($"Stopped at action {index + 1} due to error");
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failed = true;
                results.Add($"{index + 1}. ERROR: {ex.Message}");
                if (stopOnError)
                {
                    results.Add($"Stopped at action {index + 1} due to error");
                    break;
                }
            }
        }

        var batchResult = TextResult(string.Join("\n", results));
        batchResult.IsError = failed ? true : null;
        return batchResult;
    }

    private string ExecuteClick(JsonElement action)
    {
        var refId = action.TryGetProperty("ref", out var refProp) ? refProp.GetString() : null;
        if (string.IsNullOrEmpty(refId))
        {
            return "Missing ref";
        }

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return $"Element not found: {refId}";
        }

        var elementName = element.Properties.Name.ValueOrDefault ?? refId;

        if (element.Patterns.Invoke.IsSupported)
        {
            element.Patterns.Invoke.Pattern.Invoke();
            return $"Invoked {elementName}";
        }

        if (element.Patterns.Toggle.IsSupported)
        {
            element.Patterns.Toggle.Pattern.Toggle();
            return $"Toggled {elementName}";
        }

        var clickPoint = element.GetClickablePoint();
        Mouse.Click(clickPoint);
        return $"Clicked {elementName}";
    }

    private async Task<string> ExecuteTypeAsync(JsonElement action, CancellationToken cancellationToken)
    {
        var text = action.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
        if (string.IsNullOrEmpty(text))
        {
            return "Missing text";
        }

        var refId = action.TryGetProperty("ref", out var refProp) ? refProp.GetString() : null;
        if (string.IsNullOrEmpty(refId))
        {
            return "Missing ref";
        }

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return $"Element not found: {refId}";
        }

        element.Focus();
        await UiDispatcher.DelayAsync(TimeSpan.FromMilliseconds(30), cancellationToken);

        Keyboard.Type(text);

        var submit = action.TryGetProperty("submit", out var submitProp) && submitProp.GetBoolean();
        if (submit)
        {
            Keyboard.Press(VirtualKeyShort.ENTER);
        }

        return $"Typed \"{text}\"";
    }

    private async Task<string> ExecuteFillAsync(JsonElement action, CancellationToken cancellationToken)
    {
        var refId = action.TryGetProperty("ref", out var refProp) ? refProp.GetString() : null;
        var value = action.TryGetProperty("value", out var valProp) ? valProp.GetString() : null;

        if (string.IsNullOrEmpty(refId) || value == null)
        {
            return "Missing ref or value";
        }

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return $"Element not found: {refId}";
        }

        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(value);
            return $"Filled with \"{value}\"";
        }

        element.Focus();
        await UiDispatcher.DelayAsync(TimeSpan.FromMilliseconds(30), cancellationToken);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        await UiDispatcher.DelayAsync(TimeSpan.FromMilliseconds(30), cancellationToken);
        Keyboard.Type(value);
        return $"Filled with \"{value}\"";
    }

    private static async Task<string> ExecuteWaitAsync(JsonElement action, CancellationToken cancellationToken)
    {
        var ms = action.TryGetProperty("ms", out var msProp) ? msProp.GetInt32() : 100;
        if (ms < 0 || ms > McpSafetyPolicy.MaximumWaitMilliseconds)
        {
            return $"Wait must be between 0 and {McpSafetyPolicy.MaximumWaitMilliseconds}ms";
        }

        await UiDispatcher.DelayAsync(TimeSpan.FromMilliseconds(ms), cancellationToken);
        return $"Waited {ms}ms";
    }

    private static bool IsActionFailure(string result) =>
        result.StartsWith("Missing", StringComparison.Ordinal) ||
        result.StartsWith("Unknown", StringComparison.Ordinal) ||
        result.StartsWith("Element not found", StringComparison.Ordinal) ||
        result.StartsWith("Window not found", StringComparison.Ordinal) ||
        result.StartsWith("No window", StringComparison.Ordinal) ||
        result.StartsWith("Wait must", StringComparison.Ordinal);

    private string ExecuteSnapshot(JsonElement action)
    {
        var handle = action.TryGetProperty("handle", out var handleProp) ? handleProp.GetString() : null;
        if (string.IsNullOrEmpty(handle))
        {
            return "Missing handle";
        }

        var window = _sessionManager.GetWindow(handle);
        if (window == null)
        {
            return $"Window not found: {handle}";
        }

        var snapshotBuilder = new SnapshotBuilder(_elementRegistry);
        var snapshot = snapshotBuilder.BuildSnapshot(handle, window);
        return $"\n{snapshot}";
    }
}
