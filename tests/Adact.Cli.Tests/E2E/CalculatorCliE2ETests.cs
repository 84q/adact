using System.Diagnostics;
using System.Text.RegularExpressions;

using Xunit;

namespace Adact.Cli.Tests.E2E;

/// <summary>
/// L5 E2E: <c>adact.exe</c> サブプロセスを直接起動して daemon に接続し、実 calc.exe を操作する通しフロー。
/// 設計 009 §9.2 (E2E シナリオ): list-apps → attach → snapshot → click → close。
/// </summary>
[Trait("Layer", "E2E")]
[Collection("AdactCli")]
public class CalculatorCliE2ETests
{
    private readonly AdactDaemonFixture _fixture;

    /// <summary>
    /// 共有 daemon フィクスチャを受け取る xUnit コンストラクタ。
    /// </summary>
    /// <param name="fixture">テスト全体で共有される <see cref="AdactDaemonFixture"/>。</param>
    public CalculatorCliE2ETests(AdactDaemonFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// 実 calc.exe に対して list-apps → attach → click → close の一連の CLI コマンドを逓次実行し、
    /// stdout の key/value ・snapshot ファイル・ref 安定性・close 出力まで含めて検証する。
    /// CLI と daemon と UIA を含む E2E テスト (設計 009 §9.2) のテスト。
    /// </summary>
    [Fact]
    public void ListAttachSnapshotClickCloseFlow_OnCalculator_Succeeds()
    {
        using var _calcLock = new CalculatorMutex();

        // snapshot を一時ディレクトリに書き出すため、cwd = 専用 temp dir。
        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var calculator = StartCalculator();
        try
        {
            // (1) list-apps → 電卓行から windowRef を抽出
            var listResult = CliProcess.RunWithServer("list-apps", _fixture.BaseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-apps exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var windowRef = ExtractCalculatorWindowRef(listResult.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"Calculator windowRef not found in list-apps output:\n{listResult.Stdout}");

            // (2) attach <windowRef>
            var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            // 設計 §5.2 (011 §4.5): stdout に sessionId / windowRef / snapshot 行が出る (generation は廃止)。
            var sessionId = ExtractKeyValue(attachResult.Stdout, "sessionId");
            Assert.False(string.IsNullOrEmpty(sessionId),
                $"sessionId not found in attach stdout:\n{attachResult.Stdout}");

            Assert.Null(ExtractKeyValue(attachResult.Stdout, "generation"));

            var snapshotPath = ExtractKeyValue(attachResult.Stdout, "snapshot");
            Assert.False(string.IsNullOrEmpty(snapshotPath),
                $"snapshot path not found in attach stdout:\n{attachResult.Stdout}");

            // snapshot ファイルは cwd (tempDir) の .adact/ 下に出力される (設計 §4.4)。
            var resolvedSnapshot = Path.IsPathRooted(snapshotPath!)
                ? snapshotPath!
                : Path.Combine(tempDir, snapshotPath!);
            Assert.True(File.Exists(resolvedSnapshot),
                $"snapshot file not found: {resolvedSnapshot}");

            // (3) snapshot text から電卓ボタンの ref を抽出 → click
            var buttonRef = FindCalculatorButtonRef(resolvedSnapshot);
            Assert.False(string.IsNullOrEmpty(buttonRef),
                $"Calculator Button ref not found in snapshot file: {resolvedSnapshot}");
            // 後で同一 button の ref 比較に使うため、Name / AutomationId も取得しておく。
            var (buttonName, buttonAutomationId) = FindNodeIdentity(resolvedSnapshot, buttonRef!);

            var clickResult = CliProcess.RunWithServer($"click {buttonRef}", _fixture.BaseUrl, tempDir);
            Assert.True(clickResult.ExitCode == 0,
                $"click exit={clickResult.ExitCode}\nstdout: {clickResult.Stdout}\nstderr: {clickResult.Stderr}");

            // generation 行は廃止された。
            Assert.Null(ExtractKeyValue(clickResult.Stdout, "generation"));

            var clickSnapshotPath = ExtractKeyValue(clickResult.Stdout, "snapshot");
            Assert.False(string.IsNullOrEmpty(clickSnapshotPath),
                $"snapshot path not found in click stdout:\n{clickResult.Stdout}");
            var resolvedClickSnapshot = Path.IsPathRooted(clickSnapshotPath!)
                ? clickSnapshotPath!
                : Path.Combine(tempDir, clickSnapshotPath!);
            Assert.True(File.Exists(resolvedClickSnapshot),
                $"click snapshot file not found: {resolvedClickSnapshot}");
            Assert.NotEqual(resolvedSnapshot, resolvedClickSnapshot);

            // ref 安定化 (011 §4): click 後の自動 snapshot でも、同じボタン
            // (Name / AutomationId 一致) は同じ ref を返す。
            var refAfterClick = FindRefByIdentity(resolvedClickSnapshot, buttonName, buttonAutomationId);
            Assert.False(string.IsNullOrEmpty(refAfterClick),
                $"button not found in post-click snapshot: {resolvedClickSnapshot}");
            Assert.Equal(buttonRef, refAfterClick);

            // (4) close --sid <sid>
            var closeResult = CliProcess.RunWithServer(
                $"close --sid {sessionId}", _fixture.BaseUrl, tempDir);
            Assert.True(closeResult.ExitCode == 0,
                $"close exit={closeResult.ExitCode}\nstdout: {closeResult.Stdout}\nstderr: {closeResult.Stderr}");
            Assert.Contains("closed", closeResult.Stdout, StringComparison.Ordinal);
            Assert.Contains("detached", closeResult.Stdout, StringComparison.Ordinal);
        }
        finally
        {
            foreach (var p in Process.GetProcessesByName("CalculatorApp"))
            {
                try { p.Kill(); p.WaitForExit(2000); } catch { }
            }
            try { calculator?.Dispose(); } catch { }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// UWP 電卓 (calc.exe → CalculatorApp.exe) を起動し、UIA で見える状態になるまで待機する。
    /// 10 秒以内に CalculatorApp プロセスが見つからなければ <see cref="InvalidOperationException"/> で fast-fail する。
    /// </summary>
    private static Process? StartCalculator()
    {
        var p = Process.Start(new ProcessStartInfo
        {
            FileName = "calc.exe",
            UseShellExecute = true,
        });
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(10))
        {
            if (Process.GetProcessesByName("CalculatorApp").Length > 0)
            {
                // UWP 特有の安定化待ち: ウィンドウが UIA ツリーに登録されるまでの猶予。
                Thread.Sleep(1000);
                return p;
            }
            Thread.Sleep(150);
        }
        throw new InvalidOperationException("CalculatorApp did not start within 10s");
    }

    /// <summary>
    /// list-apps の TSV 出力から processName が CalculatorApp あるいは windowTitle が "電卓"
    /// を含む行の windowRef (列 0) を返す。
    /// </summary>
    private static string? ExtractCalculatorWindowRef(string stdout)
    {
        var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        // 0 行目はヘッダ。
        foreach (var line in lines.Skip(1))
        {
            var cols = line.Split('\t');
            if (cols.Length < 6) continue;

            var processName = cols[2];
            var windowTitle = cols[5];

            if (processName.Contains("Calculator", StringComparison.OrdinalIgnoreCase)
                || windowTitle.Contains("電卓", StringComparison.Ordinal)
                || windowTitle.Contains("Calculator", StringComparison.OrdinalIgnoreCase))
            {
                return cols[0];
            }
        }
        return null;
    }

    /// <summary>
    /// key-value 形式 stdout (設計 §5.1) から指定 key の値を抽出する。
    /// 区切りは空白 1 個。値に空白を含む場合 (snapshot path 等) も最初の空白以降全てを返す。
    /// </summary>
    private static string? ExtractKeyValue(string stdout, string key)
    {
        var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var idx = line.IndexOf(' ');
            if (idx <= 0) continue;
            if (!string.Equals(line[..idx], key, StringComparison.Ordinal)) continue;
            return line[(idx + 1)..];
        }
        return null;
    }

    /// <summary>
    /// Phase 7 snapshot text 形式の 1 行分を表す。設計 016 §2.5 (CLI 出力フォーマット)。
    /// 例: <c>  - Button "1" [aid="num1Button"] [ref=s1e7]</c>
    /// </summary>
    private sealed record SnapshotLine(string Role, string? Name, string? AutomationId, string? Ref);

    /// <summary>
    /// snapshot text ファイルから電卓のボタン要素の ref を探す。
    /// 優先度: AutomationId == "num1Button" > role == "Button" の最初。
    /// </summary>
    private static string? FindCalculatorButtonRef(string snapshotFilePath)
    {
        foreach (var line in ReadSnapshotLines(snapshotFilePath))
        {
            if (line.AutomationId == "num1Button") return line.Ref;
        }
        foreach (var line in ReadSnapshotLines(snapshotFilePath))
        {
            if (line.Role == "Button" && !string.IsNullOrEmpty(line.Ref)) return line.Ref;
        }
        return null;
    }

    /// <summary>snapshot から ref に対応するノードの (Name, AutomationId) を抽出する。</summary>
    private static (string? name, string? automationId) FindNodeIdentity(string snapshotFilePath, string targetRef)
    {
        foreach (var line in ReadSnapshotLines(snapshotFilePath))
        {
            if (string.Equals(line.Ref, targetRef, StringComparison.Ordinal))
            {
                return (line.Name, line.AutomationId);
            }
        }
        return (null, null);
    }

    /// <summary>(Name, AutomationId) 一致するノードの ref を snapshot から返す。</summary>
    private static string? FindRefByIdentity(string snapshotFilePath, string? name, string? automationId)
    {
        foreach (var line in ReadSnapshotLines(snapshotFilePath))
        {
            if (string.Equals(line.Name, name, StringComparison.Ordinal)
                && string.Equals(line.AutomationId, automationId, StringComparison.Ordinal))
            {
                return line.Ref;
            }
        }
        return null;
    }

    private static IEnumerable<SnapshotLine> ReadSnapshotLines(string snapshotFilePath)
    {
        var text = File.ReadAllText(snapshotFilePath);
        var inFrontmatter = false;
        var sawFrontmatterStart = false;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line == "---")
            {
                if (!sawFrontmatterStart) { sawFrontmatterStart = true; inFrontmatter = true; continue; }
                if (inFrontmatter) { inFrontmatter = false; continue; }
            }
            if (inFrontmatter || string.IsNullOrEmpty(line)) continue;

            var parsed = ParseLine(line);
            if (parsed is not null) yield return parsed;
        }
    }

    private static readonly Regex LineRegex = new(
        @"^\s*-\s+(?<role>\S+)(?:\s+""(?<name>(?:\\.|[^""\\])*)"")?(?<rest>.*)$",
        RegexOptions.Compiled);
    private static readonly Regex AidRegex = new(
        @"\[aid=""(?<aid>(?:\\.|[^""\\])*)""\]", RegexOptions.Compiled);
    private static readonly Regex RefRegex = new(
        @"\[ref=(?<ref>[^\]]+)\]", RegexOptions.Compiled);

    private static SnapshotLine? ParseLine(string line)
    {
        var m = LineRegex.Match(line);
        if (!m.Success) return null;
        var role = m.Groups["role"].Value;
        var name = m.Groups["name"].Success ? Unescape(m.Groups["name"].Value) : null;
        var rest = m.Groups["rest"].Value;
        var aidM = AidRegex.Match(rest);
        var aid = aidM.Success ? Unescape(aidM.Groups["aid"].Value) : null;
        var refM = RefRegex.Match(rest);
        var refId = refM.Success ? refM.Groups["ref"].Value : null;
        return new SnapshotLine(role, name, aid, refId);
    }

    private static string Unescape(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '\\' && i + 1 < s.Length)
            {
                var n = s[++i];
                sb.Append(n switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    't' => '\t',
                    _ => n,
                });
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
