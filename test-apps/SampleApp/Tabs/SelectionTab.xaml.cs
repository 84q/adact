using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace SampleApp.Tabs;

/// <summary>
/// Demonstrates selection behavior across combo boxes, list boxes, and item sources.
/// </summary>
public partial class SelectionTab : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionTab"/> class.
    /// </summary>
    public SelectionTab()
    {
        InitializeComponent();
        InitializeData();
    }

    private void InitializeData()
    {
        var colors = new[] { "Red", "Green", "Blue", "Yellow", "Orange", "Purple", "Cyan", "Magenta", "Black", "White" };
        foreach (var color in colors)
        {
            ColorsComboBox.Items.Add(color);
            EditableColorsComboBox.Items.Add(color);
        }
        ColorsComboBox.SelectedIndex = 0;
        EditableColorsComboBox.Text = "Green";

        var fruits = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew", "Kiwi", "Lemon", "Mango", "Nectarine", "Orange", "Papaya", "Quince", "Raspberry", "Strawberry", "Tangerine", "Ugli", "Watermelon" };
        foreach (var fruit in fruits)
        {
            FruitsListBox.Items.Add(fruit);
            MultiFruitsListBox.Items.Add(fruit);
        }
        FruitsListBox.SelectedIndex = 0;
        MultiFruitsListBox.SelectedItems.Add("Apple");
        MultiFruitsListBox.SelectedItems.Add("Cherry");

        for (int i = 1; i <= 1000; i++)
        {
            VirtualizedItemsListBox.Items.Add($"Virtualized Item {i:0000}");
        }

        var products = new ObservableCollection<Product>();
        for (int i = 1; i <= 20; i++)
        {
            products.Add(new Product { Name = $"Product {i}", Category = $"Category {(i % 5) + 1}", Price = i * 10.99 });
        }
        ProductsListView.ItemsSource = products;
    }
}

/// <summary>
/// Represents a product displayed in the selection sample list view.
/// </summary>
public class Product
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public double Price { get; set; }
}
