using Adact.Engine.Exceptions;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>Contains tests for the Exception behavior.</summary>
[Trait("Layer", "Unit")]
public class ExceptionTests
{
    /// <summary>Performs the Window Not Found Exception Given Hwnd Message Contains Hwnd Hex operation.</summary>
    [Fact]
    public void WindowNotFoundException_GivenHwnd_MessageContainsHwndHex()
    {
        var ex = new WindowNotFoundException(new IntPtr(0xABCD));
        Assert.Equal((nint)0xABCD, ex.Hwnd);
        Assert.Contains("ABCD", ex.Message);
    }

    /// <summary>Performs the Ref Not Found Exception Given Ref Id And Reason Preserves Both In Properties operation.</summary>
    [Fact]
    public void RefNotFoundException_GivenRefIdAndReason_PreservesBothInProperties()
    {
        var ex = new RefNotFoundException("s1e7", "not found in current snapshot");
        Assert.Equal("s1e7", ex.RefId);
        Assert.Equal("not found in current snapshot", ex.Reason);
        Assert.Contains("s1e7", ex.Message);
        Assert.Contains("not found in current snapshot", ex.Message);
    }

    /// <summary>Performs the Element Interaction Exception Given Operation Name Preserves Operation Property operation.</summary>
    [Fact]
    public void ElementInteractionException_GivenOperationName_PreservesOperationProperty()
    {
        var ex = new ElementInteractionException("s1e2", "click", "boom");
        Assert.Equal("click", ex.Operation);
        Assert.Contains("click", ex.Message);
        Assert.Contains("boom", ex.Message);
    }
}
