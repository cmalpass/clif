using CLIF.Validation.Validators;
using System;

class DebugValidator
{
    static void Main()
    {
        var validator = new TextInputValidator();
        
        Console.WriteLine("Testing null:");
        var result1 = validator.Validate(null);
        Console.WriteLine($"IsValid: {result1.IsValid}, Error: {result1.ErrorMessage}");
        
        Console.WriteLine("\nTesting empty string:");
        var result2 = validator.Validate("");
        Console.WriteLine($"IsValid: {result2.IsValid}, Error: {result2.ErrorMessage}");
        
        Console.WriteLine("\nTesting whitespace:");
        var result3 = validator.Validate("   ");
        Console.WriteLine($"IsValid: {result3.IsValid}, Error: {result3.ErrorMessage}");
        
        Console.WriteLine("\nTesting short text 'a':");
        var result4 = validator.Validate("a");
        Console.WriteLine($"IsValid: {result4.IsValid}, Error: {result4.ErrorMessage}");
        
        Console.WriteLine("\nTesting malicious text:");
        var result5 = validator.Validate("<script>alert('xss')</script>");
        Console.WriteLine($"IsValid: {result5.IsValid}, Error: {result5.ErrorMessage}");
    }
}