using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace SampleApp.Tabs;

public partial class SelectionTab : UserControl
{
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
        }
        ColorsComboBox.SelectedIndex = 0;

        var fruits = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry", "Fig", "Grape", "Honeydew", "Kiwi", "Lemon", "Mango", "Nectarine", "Orange", "Papaya", "Quince", "Raspberry", "Strawberry", "Tangerine", "Ugli", "Watermelon" };
        foreach (var fruit in fruits)
        {
            FruitsListBox.Items.Add(fruit);
        }

        var products = new ObservableCollection<Product>();
        for (int i = 1; i <= 20; i++)
        {
            products.Add(new Product { Name = $"Product {i}", Category = $"Category {(i % 5) + 1}", Price = i * 10.99 });
        }
        ProductsListView.ItemsSource = products;
    }
}

public class Product
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Price { get; set; }
}
