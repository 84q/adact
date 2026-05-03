using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;

namespace SampleApp.Tabs;

public partial class AdvancedCustomPatternsTab : UserControl
{
    public AdvancedCustomPatternsTab()
    {
        InitializeComponent();
        SpreadsheetLikeGrid.ItemsSource = new List<SpreadsheetRow>
        {
            new("A1", "42", "=SUM(B1:B3)", "Formula cell"),
            new("B1", "10", string.Empty, "Input value"),
            new("B2", "12", string.Empty, "Input value"),
            new("B3", "20", string.Empty, "Input value")
        };
    }

    private sealed record SpreadsheetRow(string Cell, string Value, string Formula, string Annotation);
}

public abstract class AdvancedPatternSampleControl : Control
{
    public abstract string DisplayText { get; }

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(Brushes.AliceBlue, new Pen(Brushes.SteelBlue, 1), new Rect(RenderSize));
        var text = new FormattedText(DisplayText, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawingContext.DrawText(text, new Point(8, 22));
    }
}

public sealed class MultipleViewSampleControl : AdvancedPatternSampleControl
{
    public override string DisplayText => "MultipleViewPattern\nList / Tiles / Details";
    protected override AutomationPeer OnCreateAutomationPeer() => new MultipleViewSampleAutomationPeer(this);
}

public sealed class StylesSampleControl : AdvancedPatternSampleControl
{
    public override string DisplayText => "Styles placeholder\nNot implemented";
    protected override AutomationPeer OnCreateAutomationPeer() => new StylesSampleAutomationPeer(this);
}

public sealed class DockSampleControl : AdvancedPatternSampleControl
{
    public override string DisplayText => "DockPattern\nDockPosition.Left";
    protected override AutomationPeer OnCreateAutomationPeer() => new DockSampleAutomationPeer(this);
}

public sealed class Transform2SampleControl : AdvancedPatternSampleControl
{
    public override string DisplayText => "Transform baseline\nTransform2 not implemented";
    protected override AutomationPeer OnCreateAutomationPeer() => new Transform2SampleAutomationPeer(this);
}

internal sealed class MultipleViewSampleAutomationPeer(MultipleViewSampleControl owner) : FrameworkElementAutomationPeer(owner), IMultipleViewProvider
{
    private int _currentView = 1;
    public override object? GetPattern(PatternInterface patternInterface) => patternInterface == PatternInterface.MultipleView ? this : base.GetPattern(patternInterface);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    protected override string GetClassNameCore() => "MultipleViewSampleControl";
    public int CurrentView => _currentView;
    public int[] GetSupportedViews() => [1, 2, 3];
    public string GetViewName(int viewId) => viewId switch { 1 => "List", 2 => "Tiles", 3 => "Details", _ => "Unknown" };
    public void SetCurrentView(int viewId) => _currentView = viewId;
}

internal sealed class StylesSampleAutomationPeer(StylesSampleControl owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    protected override string GetClassNameCore() => "StylesSampleControl";
    protected override string GetHelpTextCore() => "StylesPattern placeholder; WPF target framework does not expose IStylesProvider.";
}

internal sealed class DockSampleAutomationPeer(DockSampleControl owner) : FrameworkElementAutomationPeer(owner), IDockProvider
{
    private DockPosition _dockPosition = DockPosition.Left;
    public override object? GetPattern(PatternInterface patternInterface) => patternInterface == PatternInterface.Dock ? this : base.GetPattern(patternInterface);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    protected override string GetClassNameCore() => "DockSampleControl";
    public DockPosition DockPosition => _dockPosition;
    public void SetDockPosition(DockPosition dockPosition) => _dockPosition = dockPosition;
}

internal sealed class Transform2SampleAutomationPeer(Transform2SampleControl owner) : FrameworkElementAutomationPeer(owner), ITransformProvider
{
    public override object? GetPattern(PatternInterface patternInterface) => patternInterface == PatternInterface.Transform ? this : base.GetPattern(patternInterface);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    protected override string GetClassNameCore() => "Transform2SampleControl";
    public bool CanMove => false;
    public bool CanResize => false;
    public bool CanRotate => false;
    public void Move(double x, double y) { }
    public void Resize(double width, double height) { }
    public void Rotate(double degrees) { }
    protected override string GetHelpTextCore() => "TransformPattern baseline placeholder for Transform2 zoom diagnostics.";
}
