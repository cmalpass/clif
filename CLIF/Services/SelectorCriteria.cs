// <copyright file="SelectorCriteria.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using CLIF.Core;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;

namespace CLIF.Services;

internal sealed class SelectorCriteria
{
    internal string? AutomationId { get; private set; }
    internal string? Name { get; private set; }
    internal string? ClassName { get; private set; }
    internal string? ControlType { get; private set; }

    internal bool HasCriteria => this.AutomationId is not null || this.Name is not null || this.ClassName is not null || this.ControlType is not null;

    internal bool TrySet(string key, string value)
    {
        switch (key)
        {
            case "id" when this.AutomationId is null:
                this.AutomationId = value;
                return true;
            case "name" when this.Name is null:
                this.Name = value;
                return true;
            case "class" when this.ClassName is null:
                this.ClassName = value;
                return true;
            case "type" when this.ControlType is null:
                this.ControlType = value;
                return true;
            default:
                return false;
        }
    }
}
