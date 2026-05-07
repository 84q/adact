using Adact.Engine;
using Adact.Engine.Elements;

using Xunit;

namespace Adact.Engine.Tests.Unit;

[Trait("Layer", "Unit")]
public sealed class SelectorSuggesterTests
{
    [Fact]
    public void Suggest_AutomationIdUniqueGlobally_ReturnsHighByAutomationId()
    {
        var target = new FakeElement { ControlType = "Button", Name = "OK", AutomationId = "btnOK" };
        var other = new FakeElement { ControlType = "Edit", AutomationId = "txtInput" };
        var allElements = new IElement[] { target, other };

        var result = SelectorSuggester.Suggest(target, allElements, []);

        Assert.NotNull(result);
        Assert.Equal("High", result.Stability);
        Assert.Equal("cf.ByAutomationId(\"btnOK\")", result.Code);
    }

    [Fact]
    public void Suggest_AutomationIdDuplicate_FallsToNameControlType()
    {
        var target = new FakeElement { ControlType = "Button", Name = "OK", AutomationId = "btn" };
        var duplicate = new FakeElement { ControlType = "Button", Name = "Cancel", AutomationId = "btn" };
        var allElements = new IElement[] { target, duplicate };

        var result = SelectorSuggester.Suggest(target, allElements, []);

        Assert.NotNull(result);
        // AutomationId is duplicated, so should fall to ControlType+Name
        Assert.Equal("High", result.Stability);
        Assert.Equal("cf.ByName(\"OK\").And(cf.ByControlType(ControlType.Button))", result.Code);
    }

    [Fact]
    public void Suggest_ControlTypeAndNameUniqueGlobally_ReturnsHighByNameAndControlType()
    {
        var target = new FakeElement { ControlType = "Button", Name = "Submit" };
        var other = new FakeElement { ControlType = "Edit", Name = "Username" };
        var allElements = new IElement[] { target, other };

        var result = SelectorSuggester.Suggest(target, allElements, []);

        Assert.NotNull(result);
        Assert.Equal("High", result.Stability);
        Assert.Equal("cf.ByName(\"Submit\").And(cf.ByControlType(ControlType.Button))", result.Code);
    }

    [Fact]
    public void Suggest_ControlTypeAndNameDuplicate_FallsToAncestorScope()
    {
        // Target and duplicate have same ControlType+Name and same AutomationId
        var target = new FakeElement { ControlType = "Button", Name = "OK", AutomationId = "innerOK" };
        var duplicate = new FakeElement { ControlType = "Button", Name = "OK", AutomationId = "innerOK" };

        // Ancestor with AutomationId containing only target as descendant
        var ancestor = new FakeElement { ControlType = "Pane", AutomationId = "panel1" };
        ancestor.ChildList.Add(target);

        var allElements = new IElement[] { ancestor, target, duplicate };
        var ancestors = new[] { new AncestorInfo("panel1", null, "Pane") };

        var result = SelectorSuggester.Suggest(target, allElements, ancestors);

        Assert.NotNull(result);
        // AutomationId duplicated globally, Name+ControlType duplicated globally,
        // but in scope of panel1, target's AutomationId "innerOK" is unique
        Assert.Equal("High", result.Stability);
        Assert.Equal("window.FindFirstDescendant(cf.ByAutomationId(\"panel1\")).FindFirstDescendant(cf.ByAutomationId(\"innerOK\"))", result.Code);
    }

    [Fact]
    public void Suggest_AncestorScopeAutomationIdUnique_ReturnsHighChain()
    {
        var target = new FakeElement { ControlType = "Button", Name = "OK", AutomationId = "btn1" };
        var duplicate = new FakeElement { ControlType = "Button", Name = "OK", AutomationId = "btn1" };

        // ancestor contains only target, not duplicate
        var ancestor = new FakeElement { ControlType = "Pane", AutomationId = "panel1" };
        ancestor.ChildList.Add(target);

        var allElements = new IElement[] { ancestor, target, duplicate };
        var ancestors = new[] { new AncestorInfo("panel1", null, "Pane") };

        var result = SelectorSuggester.Suggest(target, allElements, ancestors);

        Assert.NotNull(result);
        Assert.Equal("High", result.Stability);
        Assert.Equal("window.FindFirstDescendant(cf.ByAutomationId(\"panel1\")).FindFirstDescendant(cf.ByAutomationId(\"btn1\"))", result.Code);
    }

    [Fact]
    public void Suggest_AncestorScopeNameControlTypeUnique_ReturnsMediumChain()
    {
        // Target has no AutomationId, Name+ControlType duplicated globally but unique in ancestor scope
        var target = new FakeElement { ControlType = "Button", Name = "OK" };
        var duplicate = new FakeElement { ControlType = "Button", Name = "OK" };

        var ancestor = new FakeElement { ControlType = "Pane", AutomationId = "panel1" };
        ancestor.ChildList.Add(target);

        var allElements = new IElement[] { ancestor, target, duplicate };
        var ancestors = new[] { new AncestorInfo("panel1", null, "Pane") };

        var result = SelectorSuggester.Suggest(target, allElements, ancestors);

        Assert.NotNull(result);
        Assert.Equal("Medium", result.Stability);
        Assert.Equal("window.FindFirstDescendant(cf.ByAutomationId(\"panel1\")).FindFirstDescendant(cf.ByName(\"OK\").And(cf.ByControlType(ControlType.Button)))", result.Code);
    }

    [Fact]
    public void Suggest_NoAncestorWithAutomationId_ReturnsLowIndex()
    {
        var target = new FakeElement { ControlType = "Button", Name = "OK" };
        var duplicate = new FakeElement { ControlType = "Button", Name = "OK" };
        var allElements = new IElement[] { duplicate, target };

        // No ancestors with AutomationId
        var ancestors = new[] { new AncestorInfo(null, "Window1", "Window") };

        var result = SelectorSuggester.Suggest(target, allElements, ancestors);

        Assert.NotNull(result);
        Assert.Equal("Low", result.Stability);
        Assert.Equal("window.FindAllDescendants(cf.ByControlType(ControlType.Button))[1]", result.Code);
    }

    [Fact]
    public void Suggest_AutomationIdNullOrEmpty_SkipsAutomationIdCandidate()
    {
        // AutomationId is null → should skip step 1 and go to ControlType+Name
        var target = new FakeElement { ControlType = "Button", Name = "Delete", AutomationId = null };
        var other = new FakeElement { ControlType = "Edit", Name = "Input" };
        var allElements = new IElement[] { target, other };

        var result = SelectorSuggester.Suggest(target, allElements, []);

        Assert.NotNull(result);
        Assert.Equal("High", result.Stability);
        Assert.Equal("cf.ByName(\"Delete\").And(cf.ByControlType(ControlType.Button))", result.Code);
    }

    [Fact]
    public void Suggest_SingleElement_ReturnsHigh()
    {
        var target = new FakeElement { ControlType = "Button", Name = "Only", AutomationId = "solo" };
        var allElements = new IElement[] { target };

        var result = SelectorSuggester.Suggest(target, allElements, []);

        Assert.NotNull(result);
        Assert.Equal("High", result.Stability);
        Assert.Equal("cf.ByAutomationId(\"solo\")", result.Code);
    }

    [Fact]
    public void Suggest_AncestorUniqueByNameControlType_UsesScopeWithNameControlType()
    {
        // target: AutomationIdなし、Name+ControlTypeが重複
        var target = new FakeElement { ControlType = "ListItem", Name = "Item1" };
        var duplicate = new FakeElement { ControlType = "ListItem", Name = "Item1" };

        // ancestor: AutomationIdなし、Name+ControlTypeがユニーク
        var ancestor = new FakeElement { ControlType = "Tab", Name = "MainTab" };
        ancestor.ChildList.Add(target);

        var allElements = new IElement[] { ancestor, target, duplicate };
        var ancestors = new[] { new AncestorInfo(null, "MainTab", "Tab") };

        var result = SelectorSuggester.Suggest(target, allElements, ancestors);

        Assert.NotNull(result);
        Assert.Contains("cf.ByName(\"MainTab\").And(cf.ByControlType(ControlType.Tab))", result.Code);
        Assert.Contains("cf.ByName(\"Item1\").And(cf.ByControlType(ControlType.ListItem))", result.Code);
    }

    [Fact]
    public void Suggest_AncestorAutomationIdNotUnique_SkipsToNameControlType()
    {
        // target: Name+ControlType duplicated globally
        var target = new FakeElement { ControlType = "Button", Name = "OK" };
        var duplicate = new FakeElement { ControlType = "Button", Name = "OK" };

        // ancestor has AutomationId but it's NOT unique
        var ancestor1 = new FakeElement { ControlType = "Pane", AutomationId = "panel", Name = "Panel1" };
        ancestor1.ChildList.Add(target);
        var ancestor1dup = new FakeElement { ControlType = "Pane", AutomationId = "panel", Name = "Panel2" };

        var allElements = new IElement[] { ancestor1, ancestor1dup, target, duplicate };
        // ancestor with duplicate AutomationId but unique Name+ControlType
        var ancestors = new[] { new AncestorInfo("panel", "Panel1", "Pane") };

        var result = SelectorSuggester.Suggest(target, allElements, ancestors);

        Assert.NotNull(result);
        // AutomationId "panel" is not unique, but Name "Panel1"+ControlType "Pane" is unique
        Assert.Contains("cf.ByName(\"Panel1\").And(cf.ByControlType(ControlType.Pane))", result.Code);
    }
}
