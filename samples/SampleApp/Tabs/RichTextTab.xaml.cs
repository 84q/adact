using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SampleApp.Tabs;

/// <summary>
/// Demonstrates rich-text document selection and focus handling.
/// </summary>
public partial class RichTextTab : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RichTextTab"/> class.
    /// </summary>
    public RichTextTab()
    {
        InitializeComponent();
    }

    private void SelectHeadingButton_Click(object sender, RoutedEventArgs e)
    {
        var paragraph = RichDocumentBox.Document.Blocks.FirstBlock as Paragraph;
        if (paragraph is null)
        {
            return;
        }

        RichDocumentBox.Selection.Select(paragraph.ContentStart, paragraph.ContentEnd);
        SelectionStatusText.Text = "Selected text status: heading paragraph selected";
        RichDocumentBox.Focus();
    }
}
