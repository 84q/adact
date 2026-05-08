using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SampleApp.Tabs;

/// <summary>
/// Demonstrates list-box drag and drop as well as custom drag/drop controls.
/// </summary>
public partial class DragDropTab : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DragDropTab"/> class.
    /// </summary>
    public DragDropTab()
    {
        InitializeComponent();
        foreach (var item in new[] { "Drag Source A", "Drag Source B", "Drag Source C" })
        {
            SourceListBox.Items.Add(item);
        }

        foreach (var item in new[] { "Target Existing 1", "Target Existing 2" })
        {
            TargetListBox.Items.Add(item);
        }

        foreach (var item in new[] { "Reorder 1", "Reorder 2", "Reorder 3", "Reorder 4" })
        {
            ReorderListBox.Items.Add(item);
        }
    }

    private void SourceListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        StartDragFromListBox(SourceListBox, e);
    }

    private void ReorderListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        StartDragFromListBox(ReorderListBox, e);
    }

    private void TargetListBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(string)) is string item)
        {
            TargetListBox.Items.Add(item);
            DragDropStatusText.Text = $"Dropped '{item}' into target list";
        }
    }

    private void ReorderListBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(string)) is not string item || !ReorderListBox.Items.Contains(item))
        {
            return;
        }

        ReorderListBox.Items.Remove(item);
        ReorderListBox.Items.Insert(0, item);
        DragDropStatusText.Text = $"Reordered '{item}' to top";
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        DragDropStatusText.Text = e.Data.GetData(typeof(string)) is string item
            ? $"Drop zone accepted '{item}'"
            : "Drop zone received non-list data";
    }

    private static void StartDragFromListBox(ListBox listBox, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || listBox.SelectedItem is not string item)
        {
            return;
        }

        DragDrop.DoDragDrop(listBox, item, DragDropEffects.Copy | DragDropEffects.Move);
    }
}

/// <summary>
/// A custom control that renders a drag source placeholder.
/// </summary>
public sealed class CustomDragSourceControl : Control
{
    /// <summary>
    /// Initializes static members of the <see cref="CustomDragSourceControl"/> class.
    /// </summary>
    static CustomDragSourceControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomDragSourceControl), new FrameworkPropertyMetadata(typeof(CustomDragSourceControl)));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new CustomDragSourceAutomationPeer(this);

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(Brushes.LightGoldenrodYellow, new Pen(Brushes.DarkGoldenrod, 1), new Rect(RenderSize));
        var text = new FormattedText("Placeholder drag source", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 13, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawingContext.DrawText(text, new Point(8, 16));
    }
}

/// <summary>
/// A custom control that renders a drop target placeholder.
/// </summary>
public sealed class CustomDropTargetControl : Control
{
    /// <summary>
    /// Initializes static members of the <see cref="CustomDropTargetControl"/> class.
    /// </summary>
    static CustomDropTargetControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomDropTargetControl), new FrameworkPropertyMetadata(typeof(CustomDropTargetControl)));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new CustomDropTargetAutomationPeer(this);

    protected override void OnRender(DrawingContext drawingContext)
    {
        drawingContext.DrawRectangle(Brushes.Honeydew, new Pen(Brushes.SeaGreen, 1), new Rect(RenderSize));
        var text = new FormattedText("Placeholder drop target", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 13, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        drawingContext.DrawText(text, new Point(8, 16));
    }
}

internal sealed class CustomDragSourceAutomationPeer(CustomDragSourceControl owner) : FrameworkElementAutomationPeer(owner)
{
    protected override string GetClassNameCore() => "CustomDragSourceControl";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    protected override string GetHelpTextCore() => "Custom peer placeholder for DragPattern; WPF target framework does not expose IDragProvider.";
}

internal sealed class CustomDropTargetAutomationPeer(CustomDropTargetControl owner) : FrameworkElementAutomationPeer(owner)
{
    protected override string GetClassNameCore() => "CustomDropTargetControl";
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    protected override string GetHelpTextCore() => "Custom peer placeholder for DropTargetPattern; WPF target framework does not expose IDropTargetProvider.";
}
