using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace SampleApp.Tabs;

public partial class RichTextTab : UserControl
{
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
