using CLIF.Core;
using CLIF.Services;
using CLIF.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CLIF.Tests.Integration;

/// <summary>
/// Base class for integration tests that require full service setup
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IProcessService ProcessService;
    protected readonly IAutomationService AutomationService;
    protected readonly IElementTreeService ElementTreeService;
    protected readonly IScriptService ScriptService;
    protected readonly IInteractiveService InteractiveService;
    protected readonly ISessionCaptureService SessionCaptureService;
    
    protected IntegrationTestBase()
    {
        var services = new ServiceCollection();
        
        // Register logging
        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Debug));
        
        // Register services with real implementations
        services.AddTransient<IProcessService, ProcessService>();
        services.AddTransient<IAutomationService, AutomationService>();
        services.AddTransient<IElementTreeService, ElementTreeService>();
        services.AddSingleton<ISessionCaptureService, SessionCaptureService>();
        
        // Build service provider
        ServiceProvider = services.BuildServiceProvider();
        
        // Get service instances
        ProcessService = ServiceProvider.GetRequiredService<IProcessService>();
        AutomationService = ServiceProvider.GetRequiredService<IAutomationService>();
        ElementTreeService = ServiceProvider.GetRequiredService<IElementTreeService>();
        SessionCaptureService = ServiceProvider.GetRequiredService<ISessionCaptureService>();
        
        // Create ScriptService with dependencies
        ScriptService = new ScriptService(
            ServiceProvider.GetRequiredService<ILogger<ScriptService>>(),
            ProcessService,
            AutomationService,
            SessionCaptureService);
        
        // Create InteractiveService with dependencies
        InteractiveService = new InteractiveService(
            ServiceProvider.GetRequiredService<ILogger<InteractiveService>>(),
            AutomationService,
            ElementTreeService,
            SessionCaptureService);
    }

    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
}
