// Licensed under the MIT License.
// CLIF MCP Server - Windows desktop automation via Model Context Protocol.
// Inspired by FlaUI-MCP (https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

using CLIF.Mcp;
using CLIF.Mcp.Core;
using CLIF.Mcp.Diagnostics;
using CLIF.Mcp.Security;
using CLIF.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

// Create shared services
var sessionManager = new WindowSessionManager();
var elementRegistry = new ElementRegistry();
sessionManager.WindowRemoved += elementRegistry.RemoveWindow;
var safetyPolicy = McpSafetyPolicy.FromEnvironment();
var diagnostics = new McpDiagnostics();

// Register all MCP tools
var toolRegistry = new ToolRegistry(safetyPolicy, diagnostics);
toolRegistry.RegisterTool(new LaunchTool(sessionManager, safetyPolicy));
toolRegistry.RegisterTool(new SnapshotTool(sessionManager, elementRegistry));
toolRegistry.RegisterTool(new ClickTool(elementRegistry));
toolRegistry.RegisterTool(new TypeTool(elementRegistry));
toolRegistry.RegisterTool(new FillTool(elementRegistry));
toolRegistry.RegisterTool(new GetTextTool(elementRegistry));
toolRegistry.RegisterTool(new ScreenshotTool(sessionManager, elementRegistry, safetyPolicy));
toolRegistry.RegisterTool(new ListWindowsTool(sessionManager, safetyPolicy));
toolRegistry.RegisterTool(new FocusWindowTool(sessionManager));
toolRegistry.RegisterTool(new CloseWindowTool(sessionManager, elementRegistry, safetyPolicy));
toolRegistry.RegisterTool(new BatchTool(sessionManager, elementRegistry));
toolRegistry.RegisterTool(new InteractTool(elementRegistry));
toolRegistry.RegisterTool(new SearchTool(sessionManager, elementRegistry));
toolRegistry.RegisterTool(new ScriptTool());

// Build the official MCP SDK host around the application-owned registry.
var sdkTools = McpSdkToolAdapter.CreateAll(toolRegistry);
var services = new ServiceCollection();
services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "clif-mcp",
            Version = "0.1.0",
        };
    })
    .WithStdioServerTransport()
    .WithTools(sdkTools);
await using var serviceProvider = services.BuildServiceProvider();
var server = serviceProvider.GetRequiredService<ModelContextProtocol.Server.McpServer>();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    await server.RunAsync(cts.Token);
}
finally
{
    sessionManager.Dispose();
}
