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

        for (int i = 1; i <= 12; i++)
        {
            var category = new TreeViewItem { Header = $"Category {i}", IsExpanded = i <= 3 };
            AutomationProperties.SetAutomationId(category, $"TreeMenu_TreeItem_Category{i}");
            AutomationProperties.SetName(category, $"Category {i}");
            for (int j = 1; j <= 10; j++)
            {
                var item = new TreeViewItem { Header = $"Item {i}-{j}" };
                AutomationProperties.SetAutomationId(item, $"TreeMenu_TreeItem_Category{i}_Item{j}");
                AutomationProperties.SetName(item, $"Item {i}-{j}");
                if (i <= 3 && j <= 2)
                {
                    item.IsExpanded = j == 1;
                    for (int k = 1; k <= 3; k++)
                    {
                        var leaf = new TreeViewItem { Header = $"Deep Node {i}-{j}-{k}" };
                        AutomationProperties.SetAutomationId(leaf, $"TreeMenu_TreeItem_Category{i}_Item{j}_Deep{k}");
                        AutomationProperties.SetName(leaf, $"Deep Node {i}-{j}-{k}");
                        item.Items.Add(leaf);
                    }
                }
                category.Items.Add(item);
            }
            root.Items.Add(category);
        }

        MainTreeView.Items.Add(root);
    }
}
