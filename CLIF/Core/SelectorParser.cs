using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace CLIF.Core;

public class SelectorParser
{
    public static AutomationElement? FindElement(AutomationElement root, string selector)
    {
        // Simple selector parsing - can be enhanced
        if (selector.StartsWith("name="))
        {
            var name = selector.Substring(5);
            return root.FindFirstDescendant(cf => cf.ByName(name));
        }
        else if (selector.StartsWith("id="))
        {
            var id = selector.Substring(3);
            return root.FindFirstDescendant(cf => cf.ByAutomationId(id));
        }
        else if (selector.StartsWith("class="))
        {
            var className = selector.Substring(6);
            return root.FindFirstDescendant(cf => cf.ByClassName(className));
        }
        else if (selector.StartsWith("type="))
        {
            var controlType = selector.Substring(5);
            if (Enum.TryParse<ControlType>(controlType, true, out var ct))
            {
                return root.FindFirstDescendant(cf => cf.ByControlType(ct));
            }
        }

        // Default to name search
        return root.FindFirstDescendant(cf => cf.ByName(selector));
    }

    public static AutomationElement[] FindElements(AutomationElement root, string selector)
    {
        // Similar logic to FindElementBySelector but returning all matches
        if (selector.StartsWith("name="))
        {
            var name = selector.Substring(5);
            return root.FindAllDescendants(cf => cf.ByName(name));
        }
        else if (selector.StartsWith("id="))
        {
            var id = selector.Substring(3);
            return root.FindAllDescendants(cf => cf.ByAutomationId(id));
        }
        else if (selector.StartsWith("class="))
        {
            var className = selector.Substring(6);
            return root.FindAllDescendants(cf => cf.ByClassName(className));
        }
        else if (selector.StartsWith("type="))
        {
            var controlType = selector.Substring(5);
            if (Enum.TryParse<ControlType>(controlType, true, out var ct))
            {
                return root.FindAllDescendants(cf => cf.ByControlType(ct));
            }
        }

        return root.FindAllDescendants(cf => cf.ByName(selector));
    }
}
