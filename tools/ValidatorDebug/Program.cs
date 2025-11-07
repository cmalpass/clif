using System;
using CLIF.Validation.Validators;

var validator = new TextInputValidator();
string[] longTexts = new[]
{
    "This is a test of the emergency broadcast system. This is only a test. If this were a real emergency, you would be instructed where to tune in your area for news and official information. This is a test of the emergency broadcast system. This concludes this test. This is additional text to make it exceed the length limit for testing purposes and ensure that the validation properly handles extremely long inputs that could potentially cause issues.",
    "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Additional text to exceed the limit.",
    "A very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very very long string that exceeds maximum allowed length"
};

for (int i = 0; i < longTexts.Length; i++)
{
    var t = longTexts[i];
    Console.WriteLine($"Text {i + 1} length: {t.Length}");
    var r = validator.Validate(t);
    Console.WriteLine($"IsValid: {r.IsValid}");
    Console.WriteLine($"ErrorMessage: {r.ErrorMessage}");
    Console.WriteLine();
}
