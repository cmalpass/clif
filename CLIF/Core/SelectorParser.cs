using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace CLIF.Core;

public class SelectorParser
{
    public static AutomationElement? FindElement(AutomationElement root, string selector)
    {
        var (selectorType, selectorValue) = ParseSelectorType(selector);
        
        switch (selectorType)
        {
            case "name":
                return root.FindFirstDescendant(cf => cf.ByName(selectorValue));
            case "id":
                return root.FindFirstDescendant(cf => cf.ByAutomationId(selectorValue));
            case "class":
                return root.FindFirstDescendant(cf => cf.ByClassName(selectorValue));
            case "type":
                if (Enum.TryParse<ControlType>(selectorValue, true, out var ct))
                {
                    return root.FindFirstDescendant(cf => cf.ByControlType(ct));
                }
                break;
        }
        
        // Default to name search
        return root.FindFirstDescendant(cf => cf.ByName(selector));
    }

    public static AutomationElement[] FindElements(AutomationElement root, string selector)
    {
        var (selectorType, selectorValue) = ParseSelectorType(selector);
        
        switch (selectorType)
        {
            case "name":
                return root.FindAllDescendants(cf => cf.ByName(selectorValue));
            case "id":
                return root.FindAllDescendants(cf => cf.ByAutomationId(selectorValue));
            case "class":
                return root.FindAllDescendants(cf => cf.ByClassName(selectorValue));
            case "type":
                if (Enum.TryParse<ControlType>(selectorValue, true, out var ct))
                {
                    return root.FindAllDescendants(cf => cf.ByControlType(ct));
                }
                break;
        }
        
        return root.FindAllDescendants(cf => cf.ByName(selector));
    }

    private static (string selectorType, string selectorValue) ParseSelectorType(string selector)
    {
        // StartsWith check ensures selector is long enough for range operator
        if (selector.StartsWith(AutomationConstants.NameSelector))
        {
            return ("name", selector[AutomationConstants.NameSelector.Length..]);
        }
        else if (selector.StartsWith(AutomationConstants.IdSelector))
        {
            return ("id", selector[AutomationConstants.IdSelector.Length..]);
        }
        else if (selector.StartsWith(AutomationConstants.ClassSelector))
        {
            return ("class", selector[AutomationConstants.ClassSelector.Length..]);
        }
        else if (selector.StartsWith(AutomationConstants.TypeSelector))
        {
            return ("type", selector[AutomationConstants.TypeSelector.Length..]);
        }
        
        // Default to name search
        return ("name", selector);
    }
}
