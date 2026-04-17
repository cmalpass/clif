// Licensed under the MIT License.
// Inspired by FlaUI-MCP (https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

using FlaUI.Core.AutomationElements;

namespace CLIF.Mcp.Core;

/// <summary>
/// Maps semantic element references (e.g. "w1e5") to FlaUI AutomationElements.
/// Refs are scoped to windows and regenerated on each snapshot.
/// </summary>
public class ElementRegistry
{
    private readonly Dictionary<string, AutomationElement> _elements = new();
    private readonly Dictionary<string, int> _windowCounters = new();

    /// <summary>
    /// Clear all elements for a window (called before each new snapshot).
    /// </summary>
    public void ClearWindow(string windowHandle)
    {
        var prefix = windowHandle + "e";
        var keysToRemove = _elements.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
        {
            _elements.Remove(key);
        }

        _windowCounters[windowHandle] = 0;
    }

    /// <summary>
    /// Register an element and return its ref id.
    /// </summary>
    public string Register(string windowHandle, AutomationElement element)
    {
        if (!_windowCounters.ContainsKey(windowHandle))
        {
            _windowCounters[windowHandle] = 0;
        }

        var refId = $"{windowHandle}e{++_windowCounters[windowHandle]}";
        _elements[refId] = element;
        return refId;
    }

    /// <summary>
    /// Get an element by its ref id.
    /// </summary>
    public AutomationElement? GetElement(string refId)
    {
        return _elements.TryGetValue(refId, out var element) ? element : null;
    }

    /// <summary>
    /// Check if a ref exists.
    /// </summary>
    public bool HasElement(string refId)
    {
        return _elements.ContainsKey(refId);
    }
}
