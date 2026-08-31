using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using CLIF.Core;
using CLIF.Services;

namespace CLIF.Commands;

/// <summary>Provides the command-line entry point for typing text into an element.</summary>
public class TypeCommand : Command
{
    private readonly IAutomationService _automationService;
    private readonly ISessionCaptureService _captureService;

    /// <summary>Creates a type command backed by the automation and capture services.</summary>
    /// <param name="automationService">Service used to attach and type into the target.</param>
    /// <param name="captureService">Service used to record the command session.</param>
    public TypeCommand(IAutomationService automationService, ISessionCaptureService captureService)
        : base("type", "Type text into a UI element")
    {
        this._automationService = automationService;
        this._captureService = captureService;

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

        var textOption = new Option<string>(
            "--text",
            "The text to type");
        textOption.IsRequired = true;
        textOption.AddAlias("-t");

        this.AddOption(processIdOption);
        this.AddOption(elementPathOption);
        this.AddOption(textOption);

        this.SetHandler(async (InvocationContext context) =>
        {
            var processId = context.ParseResult.GetValueForOption(processIdOption);
            var elementPath = context.ParseResult.GetValueForOption(elementPathOption)!;
            var text = context.ParseResult.GetValueForOption(textOption)!;

            // Start a mini-session for individual command
            var sessionId = await this._captureService.StartSessionAsync($"TYPE_Command_{DateTime.Now:HHmmss}");

            try
            {
                await this._captureService.LogInteractionAsync($"TYPE command started: '{text}' into {elementPath} (Process: {processId})");

                Console.WriteLine($"Attaching to process {processId}...");
                var success = await this._automationService.AttachToProcessAsync(processId);

                if (!success)
                {
                    Console.WriteLine("Failed to attach to process.");
                    await this._captureService.LogInteractionAsync("ERROR: Failed to attach to process", LogLevel.Error);
                    context.ExitCode = 1;
                    return;
                }

                Console.WriteLine($"Finding element: {elementPath}");
                var element = await this._automationService.FindElementAsync(elementPath);
                if (element == null)
                {
                    Console.WriteLine($"Element '{elementPath}' not found.");
                    await this._captureService.LogInteractionAsync($"ERROR: Element '{elementPath}' not found", LogLevel.Error);
                    context.ExitCode = 1;
                    return;
                }

                Console.WriteLine($"Typing text '{text}' into element: {elementPath}");
                if (!await this._automationService.TypeTextAsync(element, text))
                {
                    Console.WriteLine("Text input failed.");
                    await this._captureService.LogInteractionAsync("ERROR: Text input failed", LogLevel.Error);
                    context.ExitCode = 1;
                    return;
                }

                Console.WriteLine("Text input completed successfully.");
                await this._captureService.LogInteractionAsync("TYPE command completed successfully");
                context.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error typing text: {ex.Message}");
                await this._captureService.LogInteractionAsync($"ERROR: {ex.Message}", LogLevel.Error);
                context.ExitCode = 1;
            }
            finally
            {
                await this._captureService.EndSessionAsync();
            }
        });
    }
}
