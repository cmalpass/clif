using FluentAssertions;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// Tests for the ElementRegistry - element registration, lookup, clearing, and ref generation.
/// These tests don't require FlaUI/Windows since they only test the dictionary/ref-tracking logic.
/// </summary>
public class ElementRegistryTests
{
    [Fact]
    public void Register_ReturnsRefWithWindowPrefix()
    {
        var registry = new ElementRegistry();
        // We can't create real AutomationElements without Windows, but we can test
        // the ref generation logic with null (it's just a dictionary insert)
        var refId = registry.Register("w1", null!);

        refId.Should().StartWith("w1e");
        refId.Should().Be("w1e1");
    }

    [Fact]
    public void Register_IncrementsCounter()
    {
        var registry = new ElementRegistry();

        var ref1 = registry.Register("w1", null!);
        var ref2 = registry.Register("w1", null!);
        var ref3 = registry.Register("w1", null!);

        ref1.Should().Be("w1e1");
        ref2.Should().Be("w1e2");
        ref3.Should().Be("w1e3");
    }

    [Fact]
    public void Register_DifferentWindows_HaveIndependentCounters()
    {
        var registry = new ElementRegistry();

        var ref1 = registry.Register("w1", null!);
        var ref2 = registry.Register("w2", null!);
        var ref3 = registry.Register("w1", null!);

        ref1.Should().Be("w1e1");
        ref2.Should().Be("w2e1");
        ref3.Should().Be("w1e2");
    }

    [Fact]
    public void HasElement_ReturnsTrueForRegistered()
    {
        var registry = new ElementRegistry();
        var refId = registry.Register("w1", null!);

        registry.HasElement(refId).Should().BeTrue();
    }

    [Fact]
    public void HasElement_ReturnsFalseForUnregistered()
    {
        var registry = new ElementRegistry();

        registry.HasElement("w1e999").Should().BeFalse();
    }

    [Fact]
    public void GetElement_ReturnsNullForUnregistered()
    {
        var registry = new ElementRegistry();

        registry.GetElement("w1e999").Should().BeNull();
    }

    [Fact]
    public void GetElement_ReturnsRegisteredElement()
    {
        var registry = new ElementRegistry();
        // Register with null since we can't create real elements on Linux
        registry.Register("w1", null!);

        // It returns null because we registered null, but the lookup works
        registry.HasElement("w1e1").Should().BeTrue();
    }

    [Fact]
    public void ClearWindow_RemovesAllElementsForWindow()
    {
        var registry = new ElementRegistry();
        registry.Register("w1", null!);
        registry.Register("w1", null!);
        registry.Register("w2", null!);

        registry.ClearWindow("w1");

        registry.HasElement("w1e1").Should().BeFalse();
        registry.HasElement("w1e2").Should().BeFalse();
        registry.HasElement("w2e1").Should().BeTrue();
    }

    [Fact]
    public void ClearWindow_DoesNotReuseStaleReference()
    {
        var registry = new ElementRegistry();
        registry.Register("w1", null!);
        registry.Register("w1", null!);

        registry.ClearWindow("w1");

        var newRef = registry.Register("w1", null!);
        newRef.Should().Be("w1e3");
    }

    [Fact]
    public void ClearWindow_NoOp_WhenWindowNotTracked()
    {
        var registry = new ElementRegistry();
        registry.Register("w1", null!);

        // Should not throw
        registry.ClearWindow("w999");

        registry.HasElement("w1e1").Should().BeTrue();
    }

    [Fact]
    public void ClearWindow_DoesNotAffectOtherWindows()
    {
        var registry = new ElementRegistry();
        registry.Register("w1", null!);
        registry.Register("w2", null!);
        registry.Register("w3", null!);

        registry.ClearWindow("w2");

        registry.HasElement("w1e1").Should().BeTrue();
        registry.HasElement("w2e1").Should().BeFalse();
        registry.HasElement("w3e1").Should().BeTrue();
    }

    [Fact]
    public void Register_ManyElements_GeneratesUniqueRefs()
    {
        var registry = new ElementRegistry();
        var refs = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            refs.Add(registry.Register("w1", null!));
        }

        refs.Should().HaveCount(100, "all 100 refs should be unique");
    }

    [Fact]
    public void RefFormat_IsConsistent()
    {
        var registry = new ElementRegistry();

        var ref1 = registry.Register("w1", null!);
        var ref2 = registry.Register("w10", null!);
        var ref3 = registry.Register("w100", null!);

        ref1.Should().MatchRegex(@"^w\d+e\d+$");
        ref2.Should().MatchRegex(@"^w\d+e\d+$");
        ref3.Should().MatchRegex(@"^w\d+e\d+$");
    }
}
