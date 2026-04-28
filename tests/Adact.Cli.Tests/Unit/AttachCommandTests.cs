using Adact.Cli.Commands;
using Adact.Cli.Output;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class AttachCommandTests
{
  [Fact]
  public void ValidateAttachArgs_AllNull_ReturnsInvalidArgument()
  {
    var args = new AttachCommand.AttachArgs(null, null, null, null, null);
    var (code, message) = AttachCommand.ValidateAttachArgs(args);

    Assert.Equal(ErrorCodes.InvalidArgument, code);
    Assert.NotNull(message);
  }

  [Fact]
  public void ValidateAttachArgs_RefAndFlag_ReturnsInvalidArgument()
  {
    var args = new AttachCommand.AttachArgs(Ref: "w3", ProcessName: "calc", Title: null, ProcessId: null, ClassName: null);
    var (code, message) = AttachCommand.ValidateAttachArgs(args);

    Assert.Equal(ErrorCodes.InvalidArgument, code);
    Assert.Contains("mutually exclusive", message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void ValidateAttachArgs_InvalidRefFormat_ReturnsInvalidArgument()
  {
    var args = new AttachCommand.AttachArgs(Ref: "foo", ProcessName: null, Title: null, ProcessId: null, ClassName: null);
    var (code, message) = AttachCommand.ValidateAttachArgs(args);

    Assert.Equal(ErrorCodes.InvalidArgument, code);
    Assert.Contains("w<n>", message);
  }

  [Fact]
  public void ValidateAttachArgs_RefOnly_Succeeds()
  {
    var args = new AttachCommand.AttachArgs(Ref: "w3", ProcessName: null, Title: null, ProcessId: null, ClassName: null);
    var (code, message) = AttachCommand.ValidateAttachArgs(args);

    Assert.Null(code);
    Assert.Null(message);
  }

  [Fact]
  public void ValidateAttachArgs_ProcessNameOnly_Succeeds()
  {
    var args = new AttachCommand.AttachArgs(Ref: null, ProcessName: "calc", Title: null, ProcessId: null, ClassName: null);
    var (code, message) = AttachCommand.ValidateAttachArgs(args);

    Assert.Null(code);
    Assert.Null(message);
  }

  [Fact]
  public void ValidateAttachArgs_ProcessNameAndTitle_Succeeds()
  {
    // 複数フラグ指定は AND 条件 (排他ではない)。
    var args = new AttachCommand.AttachArgs(Ref: null, ProcessName: "calc", Title: "電卓", ProcessId: null, ClassName: null);
    var (code, message) = AttachCommand.ValidateAttachArgs(args);

    Assert.Null(code);
    Assert.Null(message);
  }
}
