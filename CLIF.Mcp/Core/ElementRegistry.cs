// Licensed under the MIT License.
// Inspired by FlaUI-MCP (https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

using FlaUI.Core.AutomationElements;

namespace CLIF.Mcp.Core;

/// <summary>
/// Maps semantic element references (e.g. "w1e5") to FlaUI AutomationElements.
/// Refs are scoped to windows and invalidated on each snapshot. A ref is never reused
/// during a session, preventing a stale ref from resolving to a different element.
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

        // Do not reset the counter. Reusing w1e1 for a new snapshot could make a
        // stale model-issued reference target a different, potentially destructive control.
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

    /// <summary>
    /// Remove all refs and the sequence state for a closed window.
    /// </summary>
    public void RemoveWindow(string windowHandle)
    {
        var prefix = windowHandle + "e";
        var keysToRemove = _elements.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var key in keysToRemove)
        {
            _elements.Remove(key);
        }

        _windowCounters.Remove(windowHandle);
    }
}
