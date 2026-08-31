// Licensed under the MIT License.

namespace CLIF.Mcp.Security;

/// <summary>
/// Describes the permission required to execute an MCP tool.
/// </summary>
public enum McpCapability
{
    /// <summary>Read-only inspection of registered UI state.</summary>
    ReadOnly,

    /// <summary>Keyboard, mouse, or control mutations.</summary>
    Input,

    /// <summary>Launching an approved executable.</summary>
    Launch,

    /// <summary>Enumerating desktop windows.</summary>
    WindowEnumeration,

    /// <summary>Focusing a registered window.</summary>
    WindowFocus,

    /// <summary>Closing a registered window.</summary>
    WindowClose,

    /// <summary>Capturing the entire desktop.</summary>
    FullScreenCapture,
}
