using Xunit;

namespace Adact.Engine.Tests.Integration;

/// <summary>
/// 同一 PID に複数ウィンドウが属しているとき、ClassName で 1 つに絞り込めることを確認する。
/// 実 automation は使わず、<see cref="WindowInfo"/> リストに対する <see cref="UiaEngine.Matches"/> 適用で再現する。
/// </summary>
[Trait("Layer", "Integration")]
public class AttachQueryClassNameDisambiguationTests
{
  private static WindowInfo[] SamePidTwoClasses() => new[]
  {
        new WindowInfo(1234, "App", "Main", "Window", "AppMainClass", new IntPtr(0x1)),
        new WindowInfo(1234, "App", "Tooltip", "Window", "AppPopupClass", new IntPtr(0x2)),
    };

  [Fact]
  public void Filter_GivenSamePidTwoClasses_NarrowsToSingleByClassName()
  {
    var all = SamePidTwoClasses();
    var q = new AttachQuery(ProcessId: 1234, ClassName: "AppMainClass");

    var matches = all.Where(w => UiaEngine.Matches(w, q)).ToList();

    Assert.Single(matches);
    Assert.Equal(new IntPtr(0x1), matches[0].NativeWindowHandle);
  }

  [Fact]
  public void Filter_GivenSamePidWithoutClassName_YieldsMultipleCandidates()
  {
    var all = SamePidTwoClasses();
    var q = new AttachQuery(ProcessId: 1234);

    var matches = all.Where(w => UiaEngine.Matches(w, q)).ToList();

    // ClassName 指定が無いと 2 件マッチ → AmbiguousAttach 相当の状況
    Assert.Equal(2, matches.Count);
  }

  [Fact]
  public void Filter_GivenClassNameOnly_StillNarrowsCorrectly()
  {
    var all = SamePidTwoClasses();
    var q = new AttachQuery(ClassName: "AppPopupClass");

    var matches = all.Where(w => UiaEngine.Matches(w, q)).ToList();

    Assert.Single(matches);
    Assert.Equal(new IntPtr(0x2), matches[0].NativeWindowHandle);
  }
}
