// <copyright file="AttachCommand.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.CommandLine;
using CLIF.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CLIF.Commands;

/// <summary>Attaches to a WPF process and optionally executes one action.</summary>
public class AttachCommand : Command
{
    /// <summary>Initializes a new instance of the <see cref="AttachCommand"/> class. Creates the attach command and registers its command-line options.</summary>
    /// <param name="serviceProvider">Provider used to resolve command services.</param>
    public AttachCommand(IServiceProvider serviceProvider) : base("attach", "Attach to a WPF process and execute actions")
    {
        var processArgument = new Argument<string>("process") { Description = "Process name, window title, or process ID" };
        var actionOption = new Option<string?>("--action") { Description = "Action to perform (click, type, get-text, etc.)" };
        var elementOption = new Option<string?>("--element") { Description = "Element selector" };
        var valueOption = new Option<string?>("--value") { Description = "Value for the action" };
        var interactiveOption = new Option<bool>("--interactive") { Description = "Start interactive mode after attaching" };

        this.Add(processArgument);
        this.Add(actionOption);
        this.Add(elementOption);
        this.Add(valueOption);
        this.Add(interactiveOption);

        this.SetHandler(async (string process, string? action, string? element, string? value, bool interactive) =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<AttachCommand>>();
            var processService = serviceProvider.GetRequiredService<IProcessService>();
            var automationService = serviceProvider.GetRequiredService<IAutomationService>();
            var interactiveService = serviceProvider.GetRequiredService<IInteractiveService>();

            try
            {
                // Find and attach to process
                ProcessInfo? targetProcess = null;

                if (int.TryParse(process, out int processId))
                {
                    targetProcess = await processService.FindProcessByIdAsync(processId);
                }
                else
                {
                    targetProcess = await processService.FindProcessByNameAsync(process) ??
                                  await processService.FindProcessByWindowTitleAsync(process);
                }

                if (targetProcess == null)
                {
                    Console.WriteLine($"Process '{process}' not found or is not a WPF application.");
                    return;
                }

                Console.WriteLine($"Attaching to process: {targetProcess.Name} (PID: {targetProcess.Id})");

                var attached = await automationService.AttachToProcessAsync(targetProcess.Id);
                if (!attached)
                {
                    Console.WriteLine("Failed to attach to process.");
                    return;
                }

                Console.WriteLine("Successfully attached to process.");

                // Execute single action if provided
                if (!string.IsNullOrEmpty(action) && !string.IsNullOrEmpty(element))
                {
                    await this.ExecuteAction(automationService, action, element, value);
                }

                // Start interactive mode if requested
                if (interactive)
                {
                    await interactiveService.StartInteractiveSessionAsync(targetProcess.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in attach command");
                Console.WriteLine($"Error: {ex.Message}");
            }
        },
            processArgument,
            actionOption,
            elementOption,
            valueOption,
            interactiveOption);
    }

    private async Task ExecuteAction(IAutomationService automation, string action, string elementSelector, string? value)
    {
        try
        {
            var element = await automation.FindElementAsync(elementSelector);
            if (element == null)
            {
                Console.WriteLine($"Element '{elementSelector}' not found.");
                return;
            }

            bool result = false;
            string output = string.Empty;

            switch (action.ToLowerInvariant())
            {
                case "click":
                    result = await automation.ClickAsync(element);
                    Console.WriteLine(result ? "Click successful" : "Click failed");
                    break;

                case "double-click":
                    result = await automation.DoubleClickAsync(element);
                    Console.WriteLine(result ? "Double-click successful" : "Double-click failed");
                    break;

                case "right-click":
                    result = await automation.RightClickAsync(element);
                    Console.WriteLine(result ? "Right-click successful" : "Right-click failed");
                    break;

                case "type":
                    if (string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine("Value is required for type action");
                        return;
                    }

                    result = await automation.TypeTextAsync(element, value);
                    Console.WriteLine(result ? "Type successful" : "Type failed");
                    break;

                case "set-value":
                    if (string.IsNullOrEmpty(value))
                    {
                        Console.WriteLine("Value is required for set-value action");
                        return;
                    }

                    result = await automation.SetValueAsync(element, value);
                    Console.WriteLine(result ? "Set value successful" : "Set value failed");
                    break;

                case "get-text":
                    output = await automation.GetTextAsync(element);
                    Console.WriteLine($"Text: {output}");
                    break;

                case "get-value":
                    output = await automation.GetValueAsync(element);
                    Console.WriteLine($"Value: {output}");
                    break;

                case "focus":
                    result = await automation.FocusAsync(element);
                    Console.WriteLine(result ? "Focus successful" : "Focus failed");
                    break;

                default:
                    Console.WriteLine($"Unknown action: {action}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing action: {ex.Message}");
        }
    }
}

// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
