namespace CLIF.Core;

public static class AutomationConstants
{
    public const int DefaultDelayMs = 300;
    public const int ShortDelayMs = 100;
    public const int ValidationDelayMs = 200;

    public const string DataItemControlType = "DataItem";
    public const string CustomControlType = "Custom";
    public const string CheckBoxControlType = "CheckBox";

    public const string ClickAction = "CLICK";
    public const string TypeAction = "TYPE";
    public const string SetValueAction = "SET_VALUE";
    public const string ClearAction = "CLEAR";

    // Selectors
    public const string NameSelector = "name=";
    public const string IdSelector = "id=";
    public const string ClassSelector = "class=";
    public const string TypeSelector = "type=";
}
