using CLIF.Core;
using CLIF.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CLIF.Tests.Utilities;

/// <summary>
/// Factory for creating mock objects consistently across tests
/// </summary>
public class MockFactory
{
    /// <summary>
    /// Creates a mock automation service
    /// </summary>
    /// <returns>Mock automation service</returns>
    public Mock<IAutomationService> CreateAutomationService()
    {
        var mock = new Mock<IAutomationService>();

        // Setup default behaviors
        mock.Setup(x => x.IsAttached).Returns(false);
        mock.Setup(x => x.AttachedProcessId).Returns((int?)null);

        // Setup successful operations by default
        mock.Setup(x => x.AttachToProcessAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        mock.Setup(x => x.ClickAsync(It.IsAny<FlaUI.Core.AutomationElements.AutomationElement>()))
            .ReturnsAsync(true);

        mock.Setup(x => x.TypeTextAsync(It.IsAny<FlaUI.Core.AutomationElements.AutomationElement>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        mock.Setup(x => x.FindElementAsync(It.IsAny<string>()))
            .ReturnsAsync(() => new Mock<FlaUI.Core.AutomationElements.AutomationElement>().Object);

        return mock;
    }

    /// <summary>
    /// Creates a mock process service
    /// </summary>
    /// <returns>Mock process service</returns>
    public Mock<IProcessService> CreateProcessService()
    {
        var mock = new Mock<IProcessService>();

        mock.Setup(x => x.GetWpfProcessesAsync())
            .Returns(Task.FromResult(new List<ProcessInfo>
            {
                new() { Id = 1234, Name = "TestApp", WindowTitle = "Test Window", HasMainWindow = true },
                new() { Id = 5678, Name = "AnotherApp", WindowTitle = "Another Window", HasMainWindow = true }
            }));

        mock.Setup(x => x.FindProcessByIdAsync(It.IsAny<int>()))
            .Returns(Task.FromResult((ProcessInfo?)null));

        return mock;
    }

    /// <summary>
    /// Creates a mock session capture service
    /// </summary>
    /// <returns>Mock session capture service</returns>
    public Mock<ISessionCaptureService> CreateSessionCaptureService()
    {
        var mock = new Mock<ISessionCaptureService>();

        mock.Setup(x => x.StartSessionAsync(It.IsAny<string>(), It.IsAny<FlaUI.Core.AutomationElements.AutomationElement>()))
            .ReturnsAsync("test-session-id");

        mock.Setup(x => x.CurrentSessionId)
            .Returns("test-session-id");

        mock.Setup(x => x.CurrentSessionPath)
            .Returns(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test-session"));

        return mock;
    }

    /// <summary>
    /// Creates a mock script service
    /// </summary>
    /// <returns>Mock script service</returns>
    public Mock<IScriptService> CreateScriptService()
    {
        var mock = new Mock<IScriptService>();

        mock.Setup(x => x.ExecuteScriptAsync(It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(new ScriptExecutionResult
            {
                Success = true,
                Message = "Script executed successfully",
                StepsExecuted = 5,
                ExecutionTime = TimeSpan.FromSeconds(2)
            });

        return mock;
    }

    /// <summary>
    /// Creates a mock element tree service
    /// </summary>
    /// <returns>Mock element tree service</returns>
    public Mock<IElementTreeService> CreateElementTreeService()
    {
        var mock = new Mock<IElementTreeService>();

        mock.Setup(x => x.BuildTreeAsync(It.IsAny<FlaUI.Core.AutomationElements.AutomationElement>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(new ElementTreeNode());

        mock.Setup(x => x.PrintTreeAsync(It.IsAny<ElementTreeNode>(), It.IsAny<TreePrintOptions>()))
            .ReturnsAsync("Test tree output");

        return mock;
    }

    /// <summary>
    /// Creates a mock logger
    /// </summary>
    /// <typeparam name="T">Type to create logger for</typeparam>
    /// <returns>Mock logger</returns>
    public Mock<ILogger<T>> CreateLogger<T>() where T : class
    {
        return TestHelpers.CreateMockLogger<T>();
    }
}