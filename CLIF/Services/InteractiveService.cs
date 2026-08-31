// <copyright file="InteractiveService.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using CLIF.Core;

namespace CLIF.Services;

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
