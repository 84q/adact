using Adact.Engine.Filters;
using Xunit;

namespace Adact.Engine.Tests.Unit;

[Trait("Layer", "Unit")]
public class FilterStrategyTests
{
  [Fact]
  public void Operable_Decide_OnRoot_ReturnsInclude()
  {
    var s = new OperableFilterStrategy();
    var win = FakeElement.Window("Test");
    Assert.Equal(NodeDecision.Include, s.Decide(win, new FilterContext(0)));
  }

  [Fact]
  public void Operable_Decide_OnButton_ReturnsInclude()
  {
    var s = new OperableFilterStrategy();
    var btn = FakeElement.Button("OK");
    Assert.Equal(NodeDecision.Include, s.Decide(btn, new FilterContext(1)));
  }

  [Fact]
  public void Operable_Decide_OnUnnamedPane_ReturnsFlatten()
  {
    var s = new OperableFilterStrategy();
    var pane = FakeElement.Pane(null);
    Assert.Equal(NodeDecision.Flatten, s.Decide(pane, new FilterContext(1)));
  }

  [Fact]
  public void Operable_Decide_OnNamedPane_ReturnsInclude()
  {
    var s = new OperableFilterStrategy();
    var pane = FakeElement.Pane("Side Panel");
    Assert.Equal(NodeDecision.Include, s.Decide(pane, new FilterContext(1)));
  }

  [Fact]
  public void Operable_Decide_OnPaneWithAutomationId_ReturnsInclude()
  {
    var s = new OperableFilterStrategy();
    var pane = new FakeElement { ControlType = "Pane", AutomationId = "main-pane" };
    Assert.Equal(NodeDecision.Include, s.Decide(pane, new FilterContext(1)));
  }

  [Fact]
  public void Operable_Decide_OnOffscreenButton_ReturnsExclude()
  {
    var s = new OperableFilterStrategy();
    var btn = FakeElement.Button("Hidden");
    btn.IsOffscreen = true;
    Assert.Equal(NodeDecision.Exclude, s.Decide(btn, new FilterContext(1)));
  }

  [Fact]
  public void Operable_ExtractProperties_OmitsEmptyKeys()
  {
    var s = new OperableFilterStrategy();
    var btn = FakeElement.Button("OK", automationId: "okBtn", helpText: "Confirm");
    var props = s.ExtractProperties(btn);

    Assert.Equal("OK", props["name"]);
    Assert.Equal("okBtn", props["automationId"]);
    Assert.Equal("Confirm", props["helpText"]);
    Assert.False(props.ContainsKey("value"));
    Assert.False(props.ContainsKey("className"));
  }

  [Fact]
  public void Operable_ExtractProperties_OmitsDefaultIsEnabledTrue()
  {
    var s = new OperableFilterStrategy();
    var btn = FakeElement.Button("OK");
    var props = s.ExtractProperties(btn);
    Assert.False(props.ContainsKey("isEnabled"));
  }

  [Fact]
  public void Operable_ExtractProperties_EmitsIsEnabledFalse()
  {
    var s = new OperableFilterStrategy();
    var btn = FakeElement.Button("OK");
    btn.IsEnabled = false;
    var props = s.ExtractProperties(btn);
    Assert.False((bool)props["isEnabled"]!);
  }

  [Fact]
  public void Raw_Decide_AlwaysIncludes()
  {
    var s = new RawFilterStrategy();
    Assert.Equal(NodeDecision.Include, s.Decide(FakeElement.Pane(null), new FilterContext(0)));
    Assert.Equal(NodeDecision.Include, s.Decide(FakeElement.Pane(null), new FilterContext(5)));
  }

  [Fact]
  public void Registry_Get_KnownStrategy_Succeeds()
  {
    var reg = new FilterStrategyRegistry();
    Assert.IsType<OperableFilterStrategy>(reg.Get("operable"));
    Assert.IsType<RawFilterStrategy>(reg.Get("raw"));
    Assert.IsType<OperableFilterStrategy>(reg.Get("OPERABLE")); // 大小無要E
  }

  [Fact]
  public void Registry_Get_UnknownStrategy_Throws()
  {
    var reg = new FilterStrategyRegistry();
    var ex = Assert.Throws<Exceptions.FilterStrategyNotFoundException>(() => reg.Get("nonexistent"));
    Assert.Equal("nonexistent", ex.Name);
  }
}
