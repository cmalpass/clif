using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace CLIF.Commands;

public class ElementCommand : Command
{
    public ElementCommand(IServiceProvider serviceProvider) : base("element", "Element operations")
    {
        var processArgument = new Argument<string>("process") { Description = "Process name or ID" };
        var selectorArgument = new Argument<string>("selector") { Description = "Element selector" };
        var actionOption = new Option<string>("--action") { Description = "Action to perform" };
        actionOption.SetDefaultValue("info");

        this.Add(processArgument);
        this.Add(selectorArgument);
        this.Add(actionOption);

        this.SetHandler(async (string process, string selector, string action) =>
        {
            Console.WriteLine($"Element operations - implementation coming soon! Process: {process}, Selector: {selector}, Action: {action}");
            await Task.CompletedTask;
        }, processArgument, selectorArgument, actionOption);
    }
}