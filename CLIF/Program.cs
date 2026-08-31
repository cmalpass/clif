// <copyright file="Program.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
using System.CommandLine;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CLIF.Commands;
using CLIF.Core;
using CLIF.Services;

// <copyright file="Program.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

namespace CLIF;

class Program
{
    [SupportedOSPlatform("windows7.0")]
    static async Task<int> Main(string[] args)
    {
        // Setup dependency injection and configuration
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("clif-config.json", optional: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Register services
        services.AddTransient<IProcessService, ProcessService>();
        services.AddTransient<IAutomationService, AutomationService>();
        services.AddTransient<IElementTreeService, ElementTreeService>();
        services.AddTransient<IScriptService, ScriptService>();
        services.AddTransient<IInteractiveService, InteractiveService>();
        services.AddSingleton<ISessionCaptureService, SessionCaptureService>();

        var serviceProvider = services.BuildServiceProvider();

        // Build command structure
        var rootCommand = new RootCommand("CLIF - Comprehensive WPF UI Automation CLI");

        // Add all commands
        rootCommand.Add(new ListProcessesCommand(serviceProvider));
        rootCommand.Add(new AttachCommand(serviceProvider));
        rootCommand.Add(new TreeCommand(serviceProvider));
        rootCommand.Add(new ClickCommand(serviceProvider.GetRequiredService<IAutomationService>(), serviceProvider.GetRequiredService<ISessionCaptureService>()));
        rootCommand.Add(new TypeCommand(serviceProvider.GetRequiredService<IAutomationService>(), serviceProvider.GetRequiredService<ISessionCaptureService>()));
        rootCommand.Add(new InteractCommand(serviceProvider.GetRequiredService<IAutomationService>(), serviceProvider.GetRequiredService<ISessionCaptureService>(), serviceProvider.GetRequiredService<ILogger<InteractCommand>>()));
        rootCommand.Add(new ScriptCommand(serviceProvider.GetRequiredService<IScriptService>()));
        rootCommand.Add(new InteractiveCommand(serviceProvider.GetRequiredService<IInteractiveService>()));

        return await rootCommand.InvokeAsync(args);
    }
}

// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
