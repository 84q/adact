using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

/// <summary>Contains tests for the Attach Command behavior.</summary>
[Trait("Layer", "Unit")]
public class AttachCommandTests
{
    /// <summary>Performs the Validate Attach Args Ref Null Returns Invalid Argument operation.</summary>
    [Fact]
    public void ValidateAttachArgs_RefNull_ReturnsInvalidArgument()
    {
        var args = new AttachCommand.AttachArgs(null);
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Equal(ErrorCodes.InvalidArgument, code);
        Assert.NotNull(message);
    }

    /// <summary>Performs the Validate Attach Args Invalid Ref Format Returns Invalid Argument operation.</summary>
    [Fact]
    public void ValidateAttachArgs_InvalidRefFormat_ReturnsInvalidArgument()
    {
        var args = new AttachCommand.AttachArgs(Ref: "foo");
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Equal(ErrorCodes.InvalidArgument, code);
        Assert.Contains("w<n>", message);
    }

    /// <summary>Performs the Validate Attach Args Ref Only Succeeds operation.</summary>
    [Fact]
    public void ValidateAttachArgs_RefOnly_Succeeds()
    {
        var args = new AttachCommand.AttachArgs(Ref: "w3");
        var (code, message) = AttachCommand.ValidateAttachArgs(args);

        Assert.Null(code);
        Assert.Null(message);
    }
}
