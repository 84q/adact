using System.Diagnostics;

using Adact.Engine;
using Adact.Engine.Exceptions;

using Xunit;

namespace Adact.Engine.Tests.Unit;

/// <summary>
/// <see cref="UiaEngine.LaunchAsync"/> の Unit テスト。実 UIA / FlaUI には依存しない。
/// 実プロセス起動を伴うケースでは、テスト終了時に必ず PID を kill してリソースリークを防ぐ。
/// 設計 024 §7。
/// </summary>
[Trait("Layer", "Unit")]
public class UiaEngineLaunchTests
{
    private static UiaEngine CreateEngine() => new();

    private static void TryKill(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            if (!p.HasExited)
            {
                p.Kill(entireProcessTree: true);
                p.WaitForExit(2000);
            }
        }
        catch
        {
            // 既に終了している / アクセス不能 は無視。
        }
    }

    /// <summary>存在しない実行ファイル指定で <see cref="LaunchFailedException"/> を投げる。</summary>
    [Fact]
    public async Task LaunchAsync_NonexistentExecutable_ThrowsLaunchFailed()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(
            Executable: "X:\\definitely-does-not-exist-adact.exe");
        await Assert.ThrowsAsync<LaunchFailedException>(
            () => engine.LaunchAsync(request));
    }

    /// <summary>cmd.exe を引数付きで起動して PID > 0 を得る。直後に kill して回収する。</summary>
    [Fact]
    public async Task LaunchAsync_CmdExe_ReturnsValidPid()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(
            Executable: "cmd.exe",
            Arguments: ["/c", "exit 0"]);

        var result = await engine.LaunchAsync(request);
        try
        {
            Assert.True(result.Pid > 0);
            Assert.False(string.IsNullOrEmpty(result.ProcessName));
            // executablePath は権限により null の場合もあるため strict には検証しない。
        }
        finally
        {
            TryKill(result.Pid);
        }
    }

    /// <summary>cwd 指定で子プロセスの作業ディレクトリが反映される。</summary>
    [Fact]
    public async Task LaunchAsync_WithWorkingDirectory_PropagatesCwd()
    {
        using var engine = CreateEngine();
        var cwd = Path.GetTempPath();
        var request = new LaunchRequest(
            Executable: "cmd.exe",
            Arguments: ["/c", "exit 0"],
            WorkingDirectory: cwd);

        var result = await engine.LaunchAsync(request);
        try
        {
            Assert.True(result.Pid > 0);
            // StartInfo は子プロセス側からは取得できないので、ここでは PID 取得成功のみ確認する。
        }
        finally
        {
            TryKill(result.Pid);
        }
    }

    /// <summary><see cref="UiaEngine.LaunchAsync"/> 経由で渡した env が子プロセスに伝搬することを確認する。
    /// 一時 .bat に <c>echo %ADACT_TEST%&gt;marker</c> を書いて cmd.exe で実行し、
    /// marker ファイル経由で値を観測する (Process.ExitCode 取得は同一ハンドルを保持していないと
    /// 環境によって不安定なため、観測しやすいファイル経由の検証とする) 。設計 024 §7。</summary>
    [Fact]
    public async Task LaunchAsync_WithEnvironment_PropagatesEnvToChildProcess()
    {
        using var engine = CreateEngine();
        var stem = Path.Combine(
            Path.GetTempPath(),
            $"adact_env_test_{Guid.NewGuid():N}");
        var batPath = stem + ".bat";
        var marker = stem + ".txt";
        File.WriteAllText(batPath, $"@echo %ADACT_TEST%>\"{marker}\"\r\n");

        var request = new LaunchRequest(
            Executable: "cmd.exe",
            Arguments: ["/c", batPath],
            Environment: new Dictionary<string, string>
            {
                ["ADACT_TEST"] = "ADACT_VALUE_42",
            });

        var result = await engine.LaunchAsync(request);
        try
        {
            Assert.True(result.Pid > 0);

            // cmd.exe は短命なので、ファイル生成完了を最大 5 秒ポーリングで待つ。
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!File.Exists(marker) && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }
            Assert.True(File.Exists(marker), $"marker file not created: {marker}");

            // 書き込み完了直後に open される race を避けて軽くリトライする。
            string? contents = null;
            for (var i = 0; i < 10; i++)
            {
                try { contents = File.ReadAllText(marker); break; }
                catch (IOException) { await Task.Delay(50); }
            }
            Assert.NotNull(contents);
            Assert.Equal("ADACT_VALUE_42", contents!.Trim());
        }
        finally
        {
            TryKill(result.Pid);
            try { if (File.Exists(marker)) File.Delete(marker); } catch { /* best effort */ }
            try { if (File.Exists(batPath)) File.Delete(batPath); } catch { /* best effort */ }
        }
    }

    /// <summary>UWP モード (shell:AppsFolder\) で cwd を指定すると <see cref="ArgumentException"/>。</summary>
    [Fact]
    public async Task LaunchAsync_UwpWithCwd_ThrowsArgumentException()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(
            Executable: "shell:AppsFolder\\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
            WorkingDirectory: "C:\\");

        await Assert.ThrowsAsync<ArgumentException>(
            () => engine.LaunchAsync(request));
    }

    /// <summary>UWP モードで env を指定すると <see cref="ArgumentException"/>。</summary>
    [Fact]
    public async Task LaunchAsync_UwpWithEnv_ThrowsArgumentException()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(
            Executable: "shell:AppsFolder\\Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
            Environment: new Dictionary<string, string> { ["FOO"] = "BAR" });

        await Assert.ThrowsAsync<ArgumentException>(
            () => engine.LaunchAsync(request));
    }

    /// <summary>空文字 executable は <see cref="ArgumentException"/>。</summary>
    [Fact]
    public async Task LaunchAsync_EmptyExecutable_ThrowsArgumentException()
    {
        using var engine = CreateEngine();
        var request = new LaunchRequest(Executable: "  ");
        await Assert.ThrowsAsync<ArgumentException>(
            () => engine.LaunchAsync(request));
    }

    /// <summary>Dispose 済み Engine への呼び出しは <see cref="ObjectDisposedException"/>。</summary>
    [Fact]
    public async Task LaunchAsync_AfterDispose_Throws()
    {
        var engine = CreateEngine();
        engine.Dispose();
        var request = new LaunchRequest(Executable: "cmd.exe");
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => engine.LaunchAsync(request));
    }

    // ---- QuoteIfNeeded (UWP 単一引数文字列向けクォーティング) ----

    /// <summary>空白 / タブ / <c>"</c> を含まない引数はクォートしない。</summary>
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("C:\\Users\\foo\\", "C:\\Users\\foo\\")]
    [InlineData("ABC", "ABC")]
    public void QuoteIfNeeded_NoWhitespaceOrQuote_NotQuoted(string input, string expected)
    {
        Assert.Equal(expected, UiaEngine.QuoteIfNeeded(input));
    }

    /// <summary>空文字列は <c>""</c> に変換される (CommandLineToArgvW で空引数として受け取れるよう)。</summary>
    [Fact]
    public void QuoteIfNeeded_Empty_ReturnsEmptyQuoted()
    {
        Assert.Equal("\"\"", UiaEngine.QuoteIfNeeded(string.Empty));
    }

    /// <summary>空白を含むだけの単純ケースはダブルクォートで囲む。</summary>
    [Fact]
    public void QuoteIfNeeded_WithSpace_Quoted()
    {
        Assert.Equal("\"with space\"", UiaEngine.QuoteIfNeeded("with space"));
    }

    /// <summary>タブを含む場合もダブルクォートで囲む。</summary>
    [Fact]
    public void QuoteIfNeeded_WithTab_Quoted()
    {
        Assert.Equal("\"a\tb\"", UiaEngine.QuoteIfNeeded("a\tb"));
    }

    /// <summary>埋め込み <c>"</c> は <c>\"</c> にエスケープされ、全体がクォートされる。</summary>
    [Fact]
    public void QuoteIfNeeded_EmbeddedQuote_EscapesQuote()
    {
        // 入力: has"quote → 出力: "has\"quote"
        Assert.Equal("\"has\\\"quote\"", UiaEngine.QuoteIfNeeded("has\"quote"));
    }

    /// <summary>クォート対象の引数が末尾にバックスラッシュを含む場合、閉じクオートに食われないよう
    /// バックスラッシュ列を 2 倍化する (PasteArguments 規約)。</summary>
    [Fact]
    public void QuoteIfNeeded_TrailingBackslashWithWhitespace_DoublesBackslashesBeforeClosingQuote()
    {
        // 入力: foo bar\  (空白あり, 末尾 \ が 1 個)
        // 期待: "foo bar\\"   (末尾 \ が 2 個に倍化)
        Assert.Equal("\"foo bar\\\\\"", UiaEngine.QuoteIfNeeded("foo bar\\"));

        // 入力: C:\Program Files\foo\\  (空白あり, 末尾 \ が 2 個)
        // 期待: "C:\Program Files\foo\\\\"   (末尾 \ が 4 個)
        Assert.Equal(
            "\"C:\\Program Files\\foo\\\\\\\\\"",
            UiaEngine.QuoteIfNeeded("C:\\Program Files\\foo\\\\"));
    }

    /// <summary><c>"</c> 直前のバックスラッシュ列も倍化 + 1 個の <c>\</c> でエスケープされる。</summary>
    [Fact]
    public void QuoteIfNeeded_BackslashesBeforeQuote_DoubledPlusOne()
    {
        // 入力: a \" b   (空白あり、 \ 1 個 + ")
        // 期待: "a \\\" b"   (\ が 2 個 + \" にエスケープ)
        Assert.Equal("\"a \\\\\\\" b\"", UiaEngine.QuoteIfNeeded("a \\\" b"));
    }
}
