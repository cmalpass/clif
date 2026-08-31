// <copyright file="BasicCommands.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace CLIF.Commands;

/// <summary>Provides the command-line entry point for element operations.</summary>
public class ElementCommand : Command
{
    /// <summary>Initializes a new instance of the <see cref="ElementCommand"/> class. Creates the element command and registers its arguments.</summary>
    /// <param name="serviceProvider">Provider reserved for resolving element services.</param>
    public ElementCommand(IServiceProvider serviceProvider)
        : base("element", "Element operations")
    {
        var processArgument = new Argument<string>("process") { Description = "Process name or ID" };
        var selectorArgument = new Argument<string>("selector") { Description = "Element selector" };
        var actionOption = new Option<string>("--action") { Description = "Action to perform" };
        actionOption.SetDefaultValue("info");

        this.Add(processArgument);
        this.Add(selectorArgument);
        this.Add(actionOption);

        this.SetHandler(
            async (
                string process,
                string selector,
                string action) =>
        {
            Console.WriteLine($"Element operations - implementation coming soon! Process: {process}, Selector: {selector}, Action: {action}");
            await Task.CompletedTask;
        },
            processArgument,
            selectorArgument,
            actionOption);
    }
}

// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
