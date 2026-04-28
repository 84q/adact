using System.Text.Json;

using Adact.Cli.Snapshots;

using Xunit;

namespace Adact.Cli.Tests.Unit;

[Trait("Layer", "Unit")]
public class SnapshotJsonParserTests
{
  [Fact]
  public void Parse_ExtractsMetaAndTree()
  {
    var json = """
    {
      "_meta": {
        "sessionId": "s1",
        "processName": "notepad",
        "processId": 4321,
        "generatedAt": "2025-01-01T00:00:00Z"
      },
      "tree": {
        "ref": "s1e1",
        "role": "Window",
        "name": "T",
        "isEnabled": true,
        "isOffscreen": false,
        "boundingRect": [0, 0, 100, 200],
        "isKeyboardFocusable": false,
        "hasKeyboardFocus": false,
        "children": [
          {
            "ref": "s1e2",
            "role": "Button",
            "name": "OK",
            "automationId": "btnOk",
            "isEnabled": true,
            "isOffscreen": false,
            "boundingRect": [0, 0, 50, 30],
            "isKeyboardFocusable": true,
            "hasKeyboardFocus": false
          }
        ]
      }
    }
    """;
    var (meta, root) = SnapshotJsonParser.Parse(json);

    Assert.Equal("s1", meta.SessionId);
    Assert.Equal("notepad", meta.ProcessName);
    Assert.Equal(4321, meta.ProcessId);
    Assert.Equal("Window", root.Role);
    Assert.Equal("T", root.Name);
    Assert.Single(root.Children);
    Assert.Equal("Button", root.Children[0].Role);
    Assert.Equal("btnOk", root.Children[0].AutomationId);
    Assert.Equal("s1e2", root.Children[0].Ref);
  }

  [Fact]
  public void Parse_MissingChildrenKey_ReturnsEmptyList()
  {
    var json = """
    {
      "_meta": {"sessionId": "s1", "generatedAt": "x"},
      "tree": {"ref": "s1e1", "role": "Window", "isEnabled": true, "isOffscreen": false}
    }
    """;
    var (_, root) = SnapshotJsonParser.Parse(json);
    Assert.Empty(root.Children);
  }

  [Fact]
  public void Parse_ModalDialogFlag_PropagatesToElement()
  {
    var json = """
    {
      "_meta": {"sessionId": "s1", "generatedAt": "x"},
      "tree": {
        "ref": "s1e1",
        "role": "Window",
        "isEnabled": true, "isOffscreen": false,
        "children": [
          {"ref": "s1e2", "role": "Window", "name": "Modal",
           "isEnabled": true, "isOffscreen": false, "isModalDialog": true}
        ]
      }
    }
    """;
    var (_, root) = SnapshotJsonParser.Parse(json);
    Assert.True(root.Children[0].IsModalDialog);
  }
}
