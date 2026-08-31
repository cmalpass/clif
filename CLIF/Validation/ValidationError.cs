// <copyright file="ValidationError.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CLIF.Validation;

/// <summary>
/// Represents a validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="field">The optional field name that failed validation.</param>
    public ValidationError(string message, string? field = null)
    {
        this.Message = message ?? throw new ArgumentNullException(nameof(message));
        this.Field = field;
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the field name that failed validation (if applicable).
    /// </summary>
    public string? Field { get; }

    /// <summary>
    /// Returns a string representation of the validation error.
    /// </summary>
    /// <returns>A string representation of the error.</returns>
    public override string ToString()
    {
        return this.Field != null ? $"{this.Field}: {this.Message}" : this.Message;
    }
}
