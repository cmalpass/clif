using System.CommandLine;
using CLIF.Core;

namespace CLIF.Commands;

public class InteractiveCommand : Command
{
    private readonly IInteractiveService _interactiveService;

    public InteractiveCommand(IInteractiveService interactiveService) 
        : base("interactive", "Enter interactive mode for UI automation")
    {
        _interactiveService = interactiveService;

        var processIdOption = new Option<int?>(
            "--process-id",
            "The process ID to attach to initially");
        processIdOption.AddAlias("-p");

        AddOption(processIdOption);

        this.SetHandler(async (int? processId) =>
        {
            try
            {
                Console.WriteLine("Entering interactive mode. Type 'help' for available commands or 'exit' to quit.");
                await _interactiveService.StartInteractiveSessionAsync(processId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in interactive mode: {ex.Message}");
            }
        }, processIdOption);
    }
}