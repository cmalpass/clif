// <copyright file="InteractiveCommand.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.CommandLine;
using CLIF.Core;

namespace CLIF.Commands;

/// <summary>Provides the command-line entry point for an interactive session.</summary>
public class InteractiveCommand : Command
{
    private readonly IInteractiveService interactiveService;

    /// <summary>Creates an interactive command backed by the interactive service.</summary>
    /// <param name="interactiveService">Service that owns the interactive session.</param>
    public InteractiveCommand(IInteractiveService interactiveService)
        : base("interactive", "Enter interactive mode for UI automation")
    {
        this.interactiveService = interactiveService;

        var processIdOption = new Option<int?>(
            "--process-id",
            "The process ID to attach to initially");
        processIdOption.AddAlias("-p");

        this.AddOption(processIdOption);

        this.SetHandler(async (int? processId) =>
        {
            try
            {
                Console.WriteLine("Entering interactive mode. Type 'help' for available commands or 'exit' to quit.");
                await this.interactiveService.StartInteractiveSessionAsync(processId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in interactive mode: {ex.Message}");
            }
        }, processIdOption);
    }
}
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
