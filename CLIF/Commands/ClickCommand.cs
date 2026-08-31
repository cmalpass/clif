// <copyright file="ClickCommand.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.CommandLine;
using System.CommandLine.Invocation;
using CLIF.Core;
using CLIF.Services;
using Microsoft.Extensions.Logging;

namespace CLIF.Commands;

/// <summary>Provides the command-line entry point for clicking an automation element.</summary>
public class ClickCommand : Command
{
    private readonly IAutomationService automationService;
    private readonly ISessionCaptureService captureService;

    /// <summary>Initializes a new instance of the <see cref="ClickCommand"/> class. Creates a click command backed by the automation and capture services.</summary>
    /// <param name="automationService">Service used to attach and click the target element.</param>
    /// <param name="captureService">Service used to record the command session.</param>
    public ClickCommand(IAutomationService automationService, ISessionCaptureService captureService)
        : base("click", "Click on a UI element")
    {
        this.automationService = automationService;
        this.captureService = captureService;

        var processIdOption = new Option<int>(
            "--process-id",
            "The process ID to attach to");
        processIdOption.IsRequired = true;
        processIdOption.AddAlias("-p");

        var elementPathOption = new Option<string>(
            "--element",
            "The automation ID or path to the element");
        elementPathOption.IsRequired = true;
        elementPathOption.AddAlias("-e");

        this.AddOption(processIdOption);
        this.AddOption(elementPathOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var processId = context.ParseResult.GetValueForOption(processIdOption);
            var elementPath = context.ParseResult.GetValueForOption(elementPathOption)!;

            // Start a mini-session for individual command
            var sessionId = await this.captureService.StartSessionAsync($"CLICK_Command_{DateTime.Now:HHmmss}");

            try
            {
                await this.captureService.LogInteractionAsync($"CLICK command started: {elementPath} (Process: {processId})");

                Console.WriteLine($"Attaching to process {processId}...");
                var success = await this.automationService.AttachToProcessAsync(processId);

                if (!success)
                {
                    Console.WriteLine("Failed to attach to process.");
                    await this.captureService.LogInteractionAsync("ERROR: Failed to attach to process", LogLevel.Error);
                    context.ExitCode = 1;
                    return;
                }

                Console.WriteLine($"Finding element: {elementPath}");
                var element = await this.automationService.FindElementAsync(elementPath);
                if (element == null)
                {
                    Console.WriteLine($"Element '{elementPath}' not found.");
                    await this.captureService.LogInteractionAsync($"ERROR: Element '{elementPath}' not found", LogLevel.Error);
                    context.ExitCode = 1;
                    return;
                }

                Console.WriteLine($"Clicking element: {elementPath}");
                if (!await this.automationService.ClickAsync(element))
                {
                    Console.WriteLine("Click failed.");
                    await this.captureService.LogInteractionAsync("ERROR: Click failed", LogLevel.Error);
                    context.ExitCode = 1;
                    return;
                }

                Console.WriteLine("Click completed successfully.");
                await this.captureService.LogInteractionAsync("CLICK command completed successfully");
                context.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clicking element: {ex.Message}");
                await this.captureService.LogInteractionAsync($"ERROR: {ex.Message}", LogLevel.Error);
                context.ExitCode = 1;
            }
            finally
            {
                await this.captureService.EndSessionAsync();
            }
        });
    }
}

// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
