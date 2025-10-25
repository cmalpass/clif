using System.CommandLine;
using CLIF.Core;

namespace CLIF.Commands;

public class ScriptCommand : Command
{
    private readonly IScriptService _scriptService;

    public ScriptCommand(IScriptService scriptService) 
        : base("script", "Execute an automation script")
    {
        _scriptService = scriptService;

        var scriptFileArgument = new Argument<string>(
            "script-file",
            "Path to the JSON script file to execute");

        var processIdOption = new Option<int?>(
            "--process-id",
            "The process ID to attach to (if not specified in script)");
        processIdOption.AddAlias("-p");

        AddArgument(scriptFileArgument);
        AddOption(processIdOption);

        this.SetHandler(async (string scriptFile, int? processId) =>
        {
            try
            {
                if (!File.Exists(scriptFile))
                {
                    Console.WriteLine($"Script file not found: {scriptFile}");
                    return;
                }

                Console.WriteLine($"Executing script: {scriptFile}");
                var result = await _scriptService.ExecuteScriptAsync(scriptFile, processId);
                if (result.Success)
                {
                    Console.WriteLine($"Script execution completed successfully in {result.ExecutionTime}");
                    Console.WriteLine($"Steps executed: {result.StepsExecuted}");
                }
                else
                {
                    Console.WriteLine($"Script execution failed: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing script: {ex.Message}");
            }
        }, scriptFileArgument, processIdOption);
    }
}