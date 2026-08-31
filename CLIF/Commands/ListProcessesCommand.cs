// <copyright file="ListProcessesCommand.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using CLIF.Core;

namespace CLIF.Commands;

/// <summary>Provides the command-line entry point for listing WPF processes.</summary>
public class ListProcessesCommand : Command
{
    /// <summary>Creates the process-listing command.</summary>
    /// <param name="serviceProvider">Provider used to resolve the process service.</param>
    public ListProcessesCommand(IServiceProvider serviceProvider) : base("list-processes", "List all available WPF processes")
    {
        var detailedOption = new Option<bool>("--detailed") { Description = "Show detailed process information" };
        var formatOption = new Option<string>("--format") { Description = "Output format (table, json, csv)" };
        formatOption.SetDefaultValue("table");

        this.Add(detailedOption);
        this.Add(formatOption);

        this.SetHandler(async (bool detailed, string format) =>
        {
            var processService = serviceProvider.GetRequiredService<IProcessService>();

            try
            {
                var processes = await processService.GetWpfProcessesAsync();

                if (!processes.Any())
                {
                    Console.WriteLine("No WPF processes found.");
                    return;
                }

                switch (format.ToLowerInvariant())
                {
                    case "json":
                        await this.OutputJsonAsync(processes, detailed);
                        break;
                    case "csv":
                        await this.OutputCsvAsync(processes, detailed);
                        break;
                    case "table":
                    default:
                        await this.OutputTableAsync(processes, detailed);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing processes: {ex.Message}");
            }
        }, detailedOption, formatOption);
    }

    private async Task OutputTableAsync(List<ProcessInfo> processes, bool detailed)
    {
        await Task.Run(() =>
        {
            if (detailed)
            {
                Console.WriteLine($"{"PID",-8} {"Process Name",-20} {"Window Title",-30} {"Executable Path",-50} {"Start Time",-20}");
                Console.WriteLine(new string('-', 128));

                foreach (var process in processes)
                {
                    Console.WriteLine($"{process.Id,-8} {this.TruncateString(process.Name, 20),-20} {this.TruncateString(process.WindowTitle, 30),-30} {this.TruncateString(process.ExecutablePath, 50),-50} {process.StartTime:yyyy-MM-dd HH:mm:ss}");
                }
            }
            else
            {
                Console.WriteLine($"{"PID",-8} {"Process Name",-20} {"Window Title",-40}");
                Console.WriteLine(new string('-', 68));

                foreach (var process in processes)
                {
                    Console.WriteLine($"{process.Id,-8} {this.TruncateString(process.Name, 20),-20} {this.TruncateString(process.WindowTitle, 40),-40}");
                }
            }
        });
    }

    private async Task OutputJsonAsync(List<ProcessInfo> processes, bool detailed)
    {
        await Task.Run(() =>
        {
            var json = System.Text.Json.JsonSerializer.Serialize(processes, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
            });
            Console.WriteLine(json);
        });
    }

    private async Task OutputCsvAsync(List<ProcessInfo> processes, bool detailed)
    {
        await Task.Run(() =>
        {
            if (detailed)
            {
                Console.WriteLine("PID,ProcessName,WindowTitle,ExecutablePath,StartTime,HasMainWindow");
                foreach (var process in processes)
                {
                    Console.WriteLine($"{process.Id},\"{this.EscapeCsv(process.Name)}\",\"{this.EscapeCsv(process.WindowTitle)}\",\"{this.EscapeCsv(process.ExecutablePath)}\",\"{process.StartTime:yyyy-MM-dd HH:mm:ss}\",{process.HasMainWindow}");
                }
            }
            else
            {
                Console.WriteLine("PID,ProcessName,WindowTitle");
                foreach (var process in processes)
                {
                    Console.WriteLine($"{process.Id},\"{this.EscapeCsv(process.Name)}\",\"{this.EscapeCsv(process.WindowTitle)}\"");
                }
            }
        });
    }

    private string TruncateString(string input, int maxLength)
    {
        if (input.Length <= maxLength)
        {
            return input;
        }

        return input.Substring(0, maxLength - 3) + "...";
    }

    private string EscapeCsv(string input)
    {
        return input.Replace("\"", "\"\"");
    }
}
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
