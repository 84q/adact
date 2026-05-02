using System;
using System.Linq;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

class Program
{
    static void PrintElement(AutomationElement e, int indent = 0)
    {
        var prefix = new string(' ', indent * 2);
        Console.WriteLine($"{prefix}{e.ControlType} | Name='{e.Name}' | AutomationId='{e.AutomationId}' | ClassName='{e.ClassName}'");
    }

    static void Main(string[] args)
    {
        var appPath = @"C:\Users\yuta_\dev\adact\samples\SampleApp\bin\Debug\net10.0-windows\SampleApp.exe";
        using (var automation = new UIA3Automation())
        {
            var app = Application.Launch(appPath);
            try
            {
                var window = app.GetMainWindow(automation);
                System.Threading.Thread.Sleep(1000);
                var tabControl = window.FindFirstDescendant(cf => cf.ByControlType(ControlType.Tab));

                // Basic Controls タブを選択して全子孫を出力
                var basicTab = tabControl.FindFirstDescendant(cf => cf.ByName("Basic Controls"));
                basicTab.AsTabItem().Select();
                System.Threading.Thread.Sleep(800);

                Console.WriteLine("=== All descendants under TabControl (Basic Controls active) ===");
                var all = tabControl.FindAllDescendants();
                foreach (var d in all)
                {
                    var parentName = d.Parent != null ? $"Parent={d.Parent.ControlType}|'{d.Parent.Name}'" : "Parent=null";
                    PrintElement(d);
                    Console.WriteLine($"    {parentName}");
                }

                // Selection タブを選択して全子孫を出力（数が少ない方がいいので Basic Controls のみで十分かも）
                // ただし Selection タブも出力して比較
                var selTab = tabControl.FindFirstDescendant(cf => cf.ByName("Selection"));
                selTab.AsTabItem().Select();
                System.Threading.Thread.Sleep(800);

                Console.WriteLine("\n=== All descendants under TabControl (Selection active) ===");
                var all2 = tabControl.FindAllDescendants();
                foreach (var d in all2)
                {
                    var parentName = d.Parent != null ? $"Parent={d.Parent.ControlType}|'{d.Parent.Name}'" : "Parent=null";
                    PrintElement(d);
                    Console.WriteLine($"    {parentName}");
                }

                Console.WriteLine("\n=== Done ===");
            }
            finally
            {
                app.Close();
            }
        }
    }
}
