using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using CLIF.Core;
using CLIF.Services;
using CLIF.Commands;

namespace CLIF;

class Program
{
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
