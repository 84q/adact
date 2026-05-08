using FlaUI.Core.WindowsAPI;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Modifier Keys behavior.</summary>
[Trait("Layer", "Unit")]
public class ModifierKeysTests
{
    /// <summary>Resolves the Resolve Null Returns Empty value.</summary>
    [Fact]
    public void Resolve_Null_ReturnsEmpty()
    {
        Assert.Empty(ModifierKeys.Resolve(null));
        Assert.Empty(ModifierKeys.Resolve(System.Array.Empty<string>()));
    }

    /// <summary>Resolves the Resolve Known Names Map To Vk value.</summary>
    [Theory]
    [InlineData("Shift", VirtualKeyShort.SHIFT)]
    [InlineData("Control", VirtualKeyShort.CONTROL)]
    [InlineData("Ctrl", VirtualKeyShort.CONTROL)]
    [InlineData("Alt", VirtualKeyShort.ALT)]
    [InlineData("Meta", VirtualKeyShort.LWIN)]
    [InlineData("Win", VirtualKeyShort.LWIN)]
    [InlineData("Windows", VirtualKeyShort.LWIN)]
    public void Resolve_KnownNames_MapToVk(string name, VirtualKeyShort expected)
    {
        var result = ModifierKeys.Resolve(new[] { name });
        Assert.Single(result, expected);
    }

    /// <summary>Resolves the Resolve Unknown Throws value.</summary>
    [Fact]
    public void Resolve_Unknown_Throws()
    {
        Assert.Throws<System.ArgumentException>(() => ModifierKeys.Resolve(new[] { "Hyper" }));
    }
}
