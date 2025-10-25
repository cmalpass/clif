using AutoFixture;
using Bogus;
using System.Text.Json;

namespace CLIF.Tests.Utilities;

/// <summary>
/// Provides methods for building test data
/// </summary>
public class TestDataBuilder
{
    private readonly Fixture _autoFixture;
    private readonly Faker _faker;

    /// <summary>
    /// Initializes a new instance of TestDataBuilder
    /// </summary>
    public TestDataBuilder()
    {
        _autoFixture = new Fixture();
        _faker = new Faker();
        
        // Configure AutoFixture for better test data generation
        _autoFixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _autoFixture.Behaviors.Remove(b));
        _autoFixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    /// <summary>
    /// Creates an instance of type T with auto-generated data
    /// </summary>
    /// <typeparam name="T">Type to create</typeparam>
    /// <returns>Instance of T</returns>
    public T Create<T>() => _autoFixture.Create<T>();
    
    /// <summary>
    /// Creates multiple instances of type T
    /// </summary>
    /// <typeparam name="T">Type to create</typeparam>
    /// <param name="count">Number of instances to create</param>
    /// <returns>List of instances</returns>
    public List<T> CreateMany<T>(int count = 3) => _autoFixture.CreateMany<T>(count).ToList();

    /// <summary>
    /// Creates random text
    /// </summary>
    /// <param name="length">Maximum length of text</param>
    /// <returns>Random text</returns>
    public string CreateRandomText(int length = 50) => _faker.Lorem.Text().Substring(0, Math.Min(length, 50));
    
    /// <summary>
    /// Creates a random element ID
    /// </summary>
    /// <returns>Element ID</returns>
    public string CreateElementId() => _faker.Internet.DomainWord() + "Element";
    
    /// <summary>
    /// Creates a random process ID
    /// </summary>
    /// <returns>Process ID</returns>
    public int CreateProcessId() => _faker.Random.Int(1000, 9999);

    /// <summary>
    /// Creates a test script file with the given script object
    /// </summary>
    /// <param name="scriptObject">Script object to serialize</param>
    /// <returns>Path to created script file</returns>
    public async Task<string> CreateTestScriptAsync(object scriptObject)
    {
        var json = JsonSerializer.Serialize(scriptObject, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        var filePath = TestHelpers.GetTempFilePath(".json");
        await File.WriteAllTextAsync(filePath, json);
        
        return filePath;
    }

    /// <summary>
    /// Creates a basic script object
    /// </summary>
    /// <param name="processId">Target process ID</param>
    /// <returns>Script object</returns>
    public object CreateBasicScript(int processId)
    {
        return new
        {
            Name = _faker.Lorem.Sentence(3),
            Description = _faker.Lorem.Sentence(10),
            Version = "1.0",
            Target = new
            {
                ProcessId = processId,
                TimeoutMs = 30000
            },
            Steps = new[]
            {
                new
                {
                    Action = "click",
                    Element = $"id={CreateElementId()}",
                    Description = "Test click action"
                },
                new
                {
                    Action = "type",
                    Element = $"id={CreateElementId()}",
                    Value = CreateRandomText(20),
                    Description = "Test type action"
                }
            }
        };
    }

    /// <summary>
    /// Creates a complex script object
    /// </summary>
    /// <param name="processId">Target process ID</param>
    /// <param name="stepCount">Number of steps to create</param>
    /// <returns>Script object</returns>
    public object CreateComplexScript(int processId, int stepCount = 10)
    {
        var actions = new[] { "click", "type", "clear", "wait" };
        var steps = new List<object>();

        for (int i = 0; i < stepCount; i++)
        {
            var action = _faker.PickRandom(actions);
            var step = new Dictionary<string, object>
            {
                ["action"] = action,
                ["description"] = $"Test step {i + 1}: {action}"
            };

            switch (action)
            {
                case "click":
                    step["element"] = $"id={CreateElementId()}";
                    break;
                case "type":
                    step["element"] = $"id={CreateElementId()}";
                    step["value"] = CreateRandomText(30);
                    break;
                case "clear":
                    step["element"] = $"id={CreateElementId()}";
                    break;
                case "wait":
                    step["duration"] = _faker.Random.Int(500, 2000);
                    break;
            }

            steps.Add(step);
        }

        return new
        {
            Name = "Complex Test Script",
            Description = "Complex automation script for testing",
            Version = "1.0",
            Target = new
            {
                ProcessId = processId,
                TimeoutMs = 60000
            },
            Options = new
            {
                TakeScreenshots = true,
                ContinueOnError = false,
                DelayBetweenActions = 100
            },
            Steps = steps
        };
    }
}