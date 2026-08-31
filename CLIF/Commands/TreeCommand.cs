// <copyright file="TreeCommand.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using CLIF.Core;

namespace CLIF.Commands;

/// <summary>Displays or searches an attached application's automation tree.</summary>
public class TreeCommand : Command
{
    /// <summary>Initializes a new instance of the <see cref="TreeCommand"/> class. Creates the tree command and registers its display options.</summary>
    /// <param name="serviceProvider">Provider used to resolve process and tree services.</param>
    public TreeCommand(IServiceProvider serviceProvider) : base("tree", "Display or search the automation element tree")
    {
        var processArgument = new Argument<string>("process") { Description = "Process name, window title, or process ID" };
        var depthOption = new Option<int>("--depth") { Description = "Maximum tree depth to display" };
        depthOption.SetDefaultValue(10);
        var enabledOnlyOption = new Option<bool>("--enabled-only") { Description = "Show only enabled elements" };
        var visibleOnlyOption = new Option<bool>("--visible-only") { Description = "Show only visible elements" };
        var showPropertiesOption = new Option<bool>("--show-properties") { Description = "Show element properties" };
        showPropertiesOption.SetDefaultValue(true);
        var showSelectorsOption = new Option<bool>("--show-selectors") { Description = "Show element selectors" };
        showSelectorsOption.SetDefaultValue(true);
        var searchOption = new Option<string?>("--search") { Description = "Search for elements matching criteria" };
        var controlTypeOption = new Option<string?>("--control-type") { Description = "Filter by control type" };
        var outputFileOption = new Option<string?>("--output") { Description = "Save tree to file" };

        this.Add(processArgument);
        this.Add(depthOption);
        this.Add(enabledOnlyOption);
        this.Add(visibleOnlyOption);
        this.Add(showPropertiesOption);
        this.Add(showSelectorsOption);
        this.Add(searchOption);
        this.Add(controlTypeOption);
        this.Add(outputFileOption);

        this.SetHandler(async (string process, int depth, bool enabledOnly, bool visibleOnly,
            bool showProperties) =>
        {
            var processService = serviceProvider.GetRequiredService<IProcessService>();
            var automationService = serviceProvider.GetRequiredService<IAutomationService>();
            var treeService = serviceProvider.GetRequiredService<IElementTreeService>();

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

                var attached = await automationService.AttachToProcessAsync(targetProcess.Id);
                if (!attached)
                {
                    Console.WriteLine("Failed to attach to process.");
                    return;
                }

                var rootElement = await automationService.GetRootElementAsync();
                if (rootElement == null)
                {
                    Console.WriteLine("Failed to get root element.");
                    return;
                }

                Console.WriteLine($"Building automation tree for: {targetProcess.Name} (PID: {targetProcess.Id})");

                var tree = await treeService.BuildTreeAsync(rootElement, true, depth);

                var printOptions = new TreePrintOptions
                {
                    ShowProperties = showProperties,
                    ShowOnlyEnabled = enabledOnly,
                    ShowOnlyVisible = visibleOnly,
                    ShowSelector = true,
                    MaxDepth = depth,
                };

                string treeOutput = await treeService.PrintTreeAsync(tree, printOptions);
                Console.WriteLine(treeOutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying tree: {ex.Message}");
            }
        }, processArgument, depthOption, enabledOnlyOption, visibleOnlyOption, showPropertiesOption);
    }

    private ElementSearchCriteria ParseSearchCriteria(string search)
    {
        var criteria = new ElementSearchCriteria();

        // Simple parsing - can be enhanced later
        if (search.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
        {
            criteria.Name = search.Substring(5);
        }
        else if (search.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
        {
            criteria.AutomationId = search.Substring(3);
        }
        else if (search.StartsWith("class:", StringComparison.OrdinalIgnoreCase))
        {
            criteria.ClassName = search.Substring(6);
        }
        else if (search.StartsWith("type:", StringComparison.OrdinalIgnoreCase))
        {
            criteria.ControlType = search.Substring(5);
        }
        else
        {
            // Default to name search
            criteria.Name = search;
        }

        return criteria;
    }
}
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
