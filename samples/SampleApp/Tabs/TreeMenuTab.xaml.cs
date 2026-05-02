using System.Windows.Automation;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class TreeMenuTab : UserControl
{
    public TreeMenuTab()
    {
        InitializeComponent();
        InitializeTree();
    }

    private void InitializeTree()
    {
        var root = new TreeViewItem { Header = "Root", IsExpanded = true };
        AutomationProperties.SetAutomationId(root, "TreeMenu_TreeItem_Root");
        AutomationProperties.SetName(root, "Root");

        for (int i = 1; i <= 3; i++)
        {
            var category = new TreeViewItem { Header = $"Category {i}", IsExpanded = true };
            AutomationProperties.SetAutomationId(category, $"TreeMenu_TreeItem_Category{i}");
            AutomationProperties.SetName(category, $"Category {i}");
            for (int j = 1; j <= 3; j++)
            {
                var item = new TreeViewItem { Header = $"Item {i}-{j}" };
                AutomationProperties.SetAutomationId(item, $"TreeMenu_TreeItem_Category{i}_Item{j}");
                AutomationProperties.SetName(item, $"Item {i}-{j}");
                category.Items.Add(item);
            }
            root.Items.Add(category);
        }

        MainTreeView.Items.Add(root);
    }
}
