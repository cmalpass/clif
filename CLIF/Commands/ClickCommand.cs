using System.CommandLine;
using Microsoft.Extensions.Logging;
using CLIF.Core;
using CLIF.Services;

namespace CLIF.Commands;

public class ClickCommand : Command
{
    private readonly IAutomationService _automationService;
    private readonly ISessionCaptureService _captureService;

    public ClickCommand(IAutomationService automationService, ISessionCaptureService captureService) 
        : base("click", "Click on a UI element")
    {
        _automationService = automationService;
        _captureService = captureService;

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

        AddOption(processIdOption);
        AddOption(elementPathOption);

        this.SetHandler(async (int processId, string elementPath) =>
        {
            // Start a mini-session for individual command
            var sessionId = await _captureService.StartSessionAsync($"CLICK_Command_{DateTime.Now:HHmmss}");
            
            try
            {
                await _captureService.LogInteractionAsync($"CLICK command started: {elementPath} (Process: {processId})");
                
                Console.WriteLine($"Attaching to process {processId}...");
                var success = await _automationService.AttachToProcessAsync(processId);
                
                if (!success)
                {
                    Console.WriteLine("Failed to attach to process.");
                    await _captureService.LogInteractionAsync("ERROR: Failed to attach to process", LogLevel.Error);
                    return;
                }

                Console.WriteLine($"Finding element: {elementPath}");
                var element = await _automationService.FindElementAsync(elementPath);
                if (element == null)
                {
                    Console.WriteLine($"Element '{elementPath}' not found.");
                    await _captureService.LogInteractionAsync($"ERROR: Element '{elementPath}' not found", LogLevel.Error);
                    return;
                }
                
                Console.WriteLine($"Clicking element: {elementPath}");
                await _automationService.ClickAsync(element);
                Console.WriteLine("Click completed successfully.");
                await _captureService.LogInteractionAsync("CLICK command completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clicking element: {ex.Message}");
                await _captureService.LogInteractionAsync($"ERROR: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                await _captureService.EndSessionAsync();
            }
        }, processIdOption, elementPathOption);
    }
}