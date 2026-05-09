using System.Diagnostics;
using System.Text.RegularExpressions;

using Adact.Cli.Connection;
using Adact.Tests.Common;

using Xunit;

namespace Adact.Cli.Tests.E2E;

/// <summary>Contains tests for the Sample App Cli E2 E behavior.</summary>
[Trait("Layer", "E2E")]
[Collection("AdactCli")]
public class SampleAppCliE2ETests
{
    private readonly AdactDaemonFixture _fixture;

    /// <summary>Initializes a new instance of the Sample App Cli E2 ETests class.</summary>
    public SampleAppCliE2ETests(AdactDaemonFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Performs the List Attach Snapshot Click Close Flow On Sample App Succeeds operation.</summary>
    [Fact]
    public async Task ListAttachSnapshotClickCloseFlow_OnSampleApp_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        try
        {
            var listResult = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var windowRef = ExtractSampleAppWindowRef(listResult.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in list-windows output:\n{listResult.Stdout}");

            // (2) attach <windowRef>
            var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            var sessionId = ExtractKeyValue(attachResult.Stdout, "sessionId");
            Assert.False(string.IsNullOrEmpty(sessionId),
                $"sessionId not found in attach stdout:\n{attachResult.Stdout}");

            Assert.Null(ExtractKeyValue(attachResult.Stdout, "generation"));

            var snapshotPath = ExtractKeyValue(attachResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(snapshotPath),
                $"snapshot path not found in attach stdout:\n{attachResult.Stdout}");

            snapshotPath = StripSnapshotNote(snapshotPath!);

            var resolvedSnapshot = Path.IsPathRooted(snapshotPath!)
                ? snapshotPath!
                : Path.Combine(tempDir, snapshotPath!);
            Assert.True(File.Exists(resolvedSnapshot),
                $"snapshot file not found: {resolvedSnapshot}");

            var buttonRef = FindSubmitButtonRef(resolvedSnapshot);
            Assert.False(string.IsNullOrEmpty(buttonRef),
                $"Submit Button ref not found in snapshot file: {resolvedSnapshot}");

            var clickResult = CliProcess.RunWithServer($"click {buttonRef}", _fixture.BaseUrl, tempDir);
            Assert.True(clickResult.ExitCode == 0,
                $"click exit={clickResult.ExitCode}\nstdout: {clickResult.Stdout}\nstderr: {clickResult.Stderr}");

            var clickSnapshotPath = ExtractKeyValue(clickResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(clickSnapshotPath),
                $"snapshot path not found in click stdout:\n{clickResult.Stdout}");
            clickSnapshotPath = StripSnapshotNote(clickSnapshotPath!);
            var resolvedClickSnapshot = Path.IsPathRooted(clickSnapshotPath!)
                ? clickSnapshotPath!
                : Path.Combine(tempDir, clickSnapshotPath!);
            Assert.True(File.Exists(resolvedClickSnapshot),
                $"click snapshot file not found: {resolvedClickSnapshot}");
            Assert.NotEqual(resolvedSnapshot, resolvedClickSnapshot);

            var (buttonName, buttonAutomationId) = FindNodeIdentity(resolvedSnapshot, buttonRef!);
            var refAfterClick = FindRefByIdentity(resolvedClickSnapshot, buttonName, buttonAutomationId);

            Assert.False(string.IsNullOrEmpty(refAfterClick),
                $"button not found in post-click snapshot: {resolvedClickSnapshot}");
            Assert.Equal(buttonRef, refAfterClick);

            // (4) close-window <sid>
            var closeResult = CliProcess.RunWithServer(
                $"close-window {sessionId}", _fixture.BaseUrl, tempDir);
            Assert.True(closeResult.ExitCode == 0,
                $"close-window exit={closeResult.ExitCode}\nstdout: {closeResult.Stdout}\nstderr: {closeResult.Stderr}");
            Assert.Contains("closed", closeResult.Stdout, StringComparison.Ordinal);
            Assert.Contains("detached", closeResult.Stdout, StringComparison.Ordinal);
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Performs the Serve Pipe List Attach Snapshot And Daemon Stop On Sample App Succeeds operation.</summary>
    [Fact]
    public async Task ServePipeListAttachSnapshotAndDaemonStop_OnSampleApp_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-pipe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var endpoint = NamedPipeEndPoint.FromWorkspacePath(tempDir);
        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        Process? servePipe = null;
        try
        {
            servePipe = Process.Start(new ProcessStartInfo
            {
                FileName = CliProcess.ExePath,
                Arguments = "serve pipe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = tempDir,
            });
            Assert.NotNull(servePipe);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await WaitForNamedPipeServerAsync(endpoint, cts.Token);

            var list = CliProcess.Run("list-windows", tempDir);
            Assert.True(list.ExitCode == 0,
                $"list-windows exit={list.ExitCode}\nstdout: {list.Stdout}\nstderr: {list.Stderr}");
            var windowRef = ExtractSampleAppWindowRef(list.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in list-windows output:\n{list.Stdout}");

            var attach = CliProcess.Run($"attach {windowRef}", tempDir);
            Assert.True(attach.ExitCode == 0,
                $"attach exit={attach.ExitCode}\nstdout: {attach.Stdout}\nstderr: {attach.Stderr}");

            var sessionId = ExtractKeyValue(attach.Stdout, "sessionId");
            Assert.False(string.IsNullOrEmpty(sessionId),
                $"sessionId not found in attach stdout:\n{attach.Stdout}");

            var snapshot = CliProcess.Run("snapshot", tempDir);
            Assert.True(snapshot.ExitCode == 0,
                $"snapshot exit={snapshot.ExitCode}\nstdout: {snapshot.Stdout}\nstderr: {snapshot.Stderr}");

            var snapshotPath = ExtractKeyValue(snapshot.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(snapshotPath),
                $"snapshot path not found in snapshot stdout:\n{snapshot.Stdout}");
            snapshotPath = StripSnapshotNote(snapshotPath!);
            var resolvedSnapshot = Path.IsPathRooted(snapshotPath!)
                ? snapshotPath!
                : Path.Combine(tempDir, snapshotPath!);
            Assert.True(File.Exists(resolvedSnapshot),
                $"snapshot file not found: {resolvedSnapshot}");

            var stop = CliProcess.Run("daemon-stop", tempDir);
            Assert.True(stop.ExitCode == 0,
                $"daemon-stop exit={stop.ExitCode}\nstdout: {stop.Stdout}\nstderr: {stop.Stderr}");
            Assert.Contains("stopped: true", stop.Stdout, StringComparison.Ordinal);

            await Task.Delay(500);
            Assert.False(await NamedPipeMcpClient.IsServerRunningAsync(endpoint, timeoutMs: 1000, CancellationToken.None));
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }

            if (servePipe is not null)
            {
                try
                {
                    if (!servePipe.HasExited)
                    {
                        servePipe.Kill(entireProcessTree: true);
                        servePipe.WaitForExit(3000);
                    }
                }
                catch { }
                try { servePipe.Dispose(); } catch { }
            }

            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Performs the Daemon Stop With Http Server Arg Returns Local Only operation.</summary>
    [Fact]
    public void DaemonStop_WithHttpServerArg_ReturnsLocalOnly()
    {
        var result = CliProcess.Run($"daemon-stop --server {_fixture.BaseUrl}");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains("error: LOCAL_ONLY", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("not supported for HTTP mode", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Performs the Snapshot Inspect Screenshot Focus Hover On Sample App Succeeds operation.</summary>
    [Fact]
    public async Task SnapshotInspectScreenshotFocusHover_OnSampleApp_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-s02-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        try
        {
            var listResult = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var windowRef = ExtractSampleAppWindowRef(listResult.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in list-windows output:\n{listResult.Stdout}");

            var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            var snapshotPath = ExtractKeyValue(attachResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(snapshotPath),
                $"snapshot path not found in attach stdout:\n{attachResult.Stdout}");
            snapshotPath = StripSnapshotNote(snapshotPath!);
            var resolvedSnapshot = Path.IsPathRooted(snapshotPath)
                ? snapshotPath
                : Path.Combine(tempDir, snapshotPath);
            Assert.True(File.Exists(resolvedSnapshot),
                $"snapshot file not found: {resolvedSnapshot}");

            var submitRef = FindSubmitButtonRef(resolvedSnapshot);
            Assert.False(string.IsNullOrEmpty(submitRef),
                $"Submit Button ref not found in snapshot file: {resolvedSnapshot}");

            var inspectResult = CliProcess.RunWithServer($"inspect {submitRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectResult.ExitCode == 0,
                $"inspect exit={inspectResult.ExitCode}\nstdout: {inspectResult.Stdout}\nstderr: {inspectResult.Stderr}");
            Assert.Contains("controlType:", inspectResult.Stdout, StringComparison.Ordinal);
            Assert.Contains("automationId: BasicControls_Button_Submit", inspectResult.Stdout, StringComparison.Ordinal);

            var screenshotOut = Path.Combine(tempDir, "s02-submit.png");
            var screenshotResult = CliProcess.RunWithServer($"screenshot {submitRef} --out \"{screenshotOut}\"", _fixture.BaseUrl, tempDir);
            Assert.True(screenshotResult.ExitCode == 0,
                $"screenshot exit={screenshotResult.ExitCode}\nstdout: {screenshotResult.Stdout}\nstderr: {screenshotResult.Stderr}");
            Assert.True(File.Exists(screenshotOut), $"screenshot file not found: {screenshotOut}");
            Assert.True(IsPngFile(screenshotOut), $"screenshot file is not PNG: {screenshotOut}");

            var focusResult = CliProcess.RunWithServer($"focus {submitRef}", _fixture.BaseUrl, tempDir);
            Assert.True(focusResult.ExitCode == 0,
                $"focus exit={focusResult.ExitCode}\nstdout: {focusResult.Stdout}\nstderr: {focusResult.Stderr}");

            var inspectFocusedResult = CliProcess.RunWithServer($"inspect {submitRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectFocusedResult.ExitCode == 0,
                $"inspect(after focus) exit={inspectFocusedResult.ExitCode}\nstdout: {inspectFocusedResult.Stdout}\nstderr: {inspectFocusedResult.Stderr}");
            var state = ExtractKeyValue(inspectFocusedResult.Stdout, "state");
            Assert.False(string.IsNullOrEmpty(state),
                $"state not found in inspect(after focus) stdout:\n{inspectFocusedResult.Stdout}");
            Assert.Contains("focused", state!.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);

            var hoverResult = CliProcess.RunWithServer($"hover {submitRef}", _fixture.BaseUrl, tempDir);
            Assert.True(hoverResult.ExitCode == 0,
                $"hover exit={hoverResult.ExitCode}\nstdout: {hoverResult.Stdout}\nstderr: {hoverResult.Stderr}");

            var hoverSnapshotPath = ExtractKeyValue(hoverResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(hoverSnapshotPath),
                $"snapshot path not found in hover stdout:\n{hoverResult.Stdout}");
            hoverSnapshotPath = StripSnapshotNote(hoverSnapshotPath!);
            var resolvedHoverSnapshot = Path.IsPathRooted(hoverSnapshotPath)
                ? hoverSnapshotPath
                : Path.Combine(tempDir, hoverSnapshotPath);
            Assert.True(File.Exists(resolvedHoverSnapshot),
                $"hover snapshot file not found: {resolvedHoverSnapshot}");
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Performs the Fill Type Keypress Keydown Keyup On Sample App Succeeds operation.</summary>
    [Fact]
    public async Task FillTypeKeypressKeydownKeyup_OnSampleApp_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-s03-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        try
        {
            var listResult = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var windowRef = ExtractSampleAppWindowRef(listResult.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in list-windows output:\n{listResult.Stdout}");

            var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            var snapshotPath = ExtractKeyValue(attachResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(snapshotPath),
                $"snapshot path not found in attach stdout:\n{attachResult.Stdout}");
            snapshotPath = StripSnapshotNote(snapshotPath!);
            var resolvedSnapshot = Path.IsPathRooted(snapshotPath)
                ? snapshotPath
                : Path.Combine(tempDir, snapshotPath);
            Assert.True(File.Exists(resolvedSnapshot),
                $"snapshot file not found: {resolvedSnapshot}");

            var nameInputRef = FindRefByAutomationId(resolvedSnapshot, "BasicControls_TextBox_NameInput");
            Assert.False(string.IsNullOrEmpty(nameInputRef),
                $"NameInput ref not found in snapshot file: {resolvedSnapshot}");

            var passwordInputRef = FindRefByAutomationId(resolvedSnapshot, "BasicControls_PasswordBox_PasswordInput");
            Assert.False(string.IsNullOrEmpty(passwordInputRef),
                $"PasswordInput ref not found in snapshot file: {resolvedSnapshot}");

            var fillToken = "S03-FILL";
            var typeToken = "-TYPE";

            var fillResult = CliProcess.RunWithServer($"fill {nameInputRef} \"{fillToken}\"", _fixture.BaseUrl, tempDir);
            Assert.True(fillResult.ExitCode == 0,
                $"fill exit={fillResult.ExitCode}\nstdout: {fillResult.Stdout}\nstderr: {fillResult.Stderr}");

            var inspectAfterFill = CliProcess.RunWithServer($"inspect {nameInputRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterFill.ExitCode == 0,
                $"inspect(after fill) exit={inspectAfterFill.ExitCode}\nstdout: {inspectAfterFill.Stdout}\nstderr: {inspectAfterFill.Stderr}");
            var valueAfterFill = ExtractKeyValue(inspectAfterFill.Stdout, "value");
            Assert.False(string.IsNullOrEmpty(valueAfterFill),
                $"value not found in inspect(after fill) stdout:\n{inspectAfterFill.Stdout}");
            Assert.Contains(fillToken, valueAfterFill!, StringComparison.Ordinal);

            var typeResult = CliProcess.RunWithServer($"type {nameInputRef} \"{typeToken}\"", _fixture.BaseUrl, tempDir);
            Assert.True(typeResult.ExitCode == 0,
                $"type exit={typeResult.ExitCode}\nstdout: {typeResult.Stdout}\nstderr: {typeResult.Stderr}");

            var inspectAfterType = CliProcess.RunWithServer($"inspect {nameInputRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterType.ExitCode == 0,
                $"inspect(after type) exit={inspectAfterType.ExitCode}\nstdout: {inspectAfterType.Stdout}\nstderr: {inspectAfterType.Stderr}");
            var valueAfterType = ExtractKeyValue(inspectAfterType.Stdout, "value");
            Assert.False(string.IsNullOrEmpty(valueAfterType),
                $"value not found in inspect(after type) stdout:\n{inspectAfterType.Stdout}");
            Assert.Contains(fillToken, valueAfterType!, StringComparison.Ordinal);
            Assert.Contains(typeToken, valueAfterType!, StringComparison.Ordinal);

            var keypressTabResult = CliProcess.RunWithServer("keypress Tab", _fixture.BaseUrl, tempDir);
            Assert.True(keypressTabResult.ExitCode == 0,
                $"keypress(Tab) exit={keypressTabResult.ExitCode}\nstdout: {keypressTabResult.Stdout}\nstderr: {keypressTabResult.Stderr}");

            var inspectPasswordFocused = CliProcess.RunWithServer($"inspect {passwordInputRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectPasswordFocused.ExitCode == 0,
                $"inspect(password after keypress Tab) exit={inspectPasswordFocused.ExitCode}\nstdout: {inspectPasswordFocused.Stdout}\nstderr: {inspectPasswordFocused.Stderr}");
            var passwordState = ExtractKeyValue(inspectPasswordFocused.Stdout, "state");
            Assert.False(string.IsNullOrEmpty(passwordState),
                $"state not found in inspect(password after keypress Tab) stdout:\n{inspectPasswordFocused.Stdout}");
            Assert.Contains("focused", passwordState!.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);

            var keydownShiftResult = CliProcess.RunWithServer("keydown Shift", _fixture.BaseUrl, tempDir);
            var shiftKeyDown = keydownShiftResult.ExitCode == 0;
            Assert.True(shiftKeyDown,
                $"keydown(Shift) exit={keydownShiftResult.ExitCode}\nstdout: {keydownShiftResult.Stdout}\nstderr: {keydownShiftResult.Stderr}");

            try
            {
                var keypressTabWithShiftResult = CliProcess.RunWithServer("keypress Tab", _fixture.BaseUrl, tempDir);
                Assert.True(keypressTabWithShiftResult.ExitCode == 0,
                    $"keypress(Tab with Shift down) exit={keypressTabWithShiftResult.ExitCode}\nstdout: {keypressTabWithShiftResult.Stdout}\nstderr: {keypressTabWithShiftResult.Stderr}");

                var inspectNameFocused = CliProcess.RunWithServer($"inspect {nameInputRef}", _fixture.BaseUrl, tempDir);
                Assert.True(inspectNameFocused.ExitCode == 0,
                    $"inspect(name after Shift+Tab) exit={inspectNameFocused.ExitCode}\nstdout: {inspectNameFocused.Stdout}\nstderr: {inspectNameFocused.Stderr}");
                var nameState = ExtractKeyValue(inspectNameFocused.Stdout, "state");
                Assert.False(string.IsNullOrEmpty(nameState),
                    $"state not found in inspect(name after Shift+Tab) stdout:\n{inspectNameFocused.Stdout}");
                Assert.Contains("focused", nameState!.Split(' ', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            }
            finally
            {
                if (shiftKeyDown)
                {
                    var keyupShiftResult = CliProcess.RunWithServer("keyup Shift", _fixture.BaseUrl, tempDir);
                    Assert.True(keyupShiftResult.ExitCode == 0,
                        $"keyup(Shift) exit={keyupShiftResult.ExitCode}\nstdout: {keyupShiftResult.Stdout}\nstderr: {keyupShiftResult.Stderr}");
                }
            }
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Performs the Click Check Uncheck Select On Sample App Succeeds operation.</summary>
    [Fact]
    public async Task ClickCheckUncheckSelect_OnSampleApp_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-s04-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        try
        {
            var listResult = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var windowRef = ExtractSampleAppWindowRef(listResult.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in list-windows output:\n{listResult.Stdout}");

            var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            var snapshotPath = ExtractKeyValue(attachResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(snapshotPath),
                $"snapshot path not found in attach stdout:\n{attachResult.Stdout}");
            snapshotPath = StripSnapshotNote(snapshotPath!);
            var resolvedSnapshot = Path.IsPathRooted(snapshotPath)
                ? snapshotPath
                : Path.Combine(tempDir, snapshotPath);
            Assert.True(File.Exists(resolvedSnapshot),
                $"snapshot file not found: {resolvedSnapshot}");

            var enableFeatureRef = FindRefByAutomationId(resolvedSnapshot, "BasicControls_CheckBox_EnableFeature");
            Assert.False(string.IsNullOrEmpty(enableFeatureRef),
                $"EnableFeature CheckBox ref not found in snapshot file: {resolvedSnapshot}");

            var selectionTabRef = FindRefByAutomationId(resolvedSnapshot, "MainWindow_TabItem_Selection");
            Assert.False(string.IsNullOrEmpty(selectionTabRef),
                $"Selection tab ref not found in snapshot file: {resolvedSnapshot}");

            var checkResult = CliProcess.RunWithServer($"check {enableFeatureRef}", _fixture.BaseUrl, tempDir);
            Assert.True(checkResult.ExitCode == 0,
                $"check exit={checkResult.ExitCode}\nstdout: {checkResult.Stdout}\nstderr: {checkResult.Stderr}");

            var inspectAfterCheck = CliProcess.RunWithServer($"inspect {enableFeatureRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterCheck.ExitCode == 0,
                $"inspect(after check) exit={inspectAfterCheck.ExitCode}\nstdout: {inspectAfterCheck.Stdout}\nstderr: {inspectAfterCheck.Stderr}");
            var checkedState = ExtractKeyValue(inspectAfterCheck.Stdout, "state");
            var checkedToggle = ExtractKeyValue(inspectAfterCheck.Stdout, "toggleState")
                ?? ExtractKeyValue(inspectAfterCheck.Stdout, "value");
            Assert.True(
                StateHasToken(checkedState, "checked")
                || IsExactToggleState(checkedToggle, "on", "true")
                || inspectAfterCheck.Stdout.Contains("Toggle: On", StringComparison.Ordinal),
                $"checked state not observed in inspect(after check) stdout:\n{inspectAfterCheck.Stdout}");

            var uncheckResult = CliProcess.RunWithServer($"uncheck {enableFeatureRef}", _fixture.BaseUrl, tempDir);
            Assert.True(uncheckResult.ExitCode == 0,
                $"uncheck exit={uncheckResult.ExitCode}\nstdout: {uncheckResult.Stdout}\nstderr: {uncheckResult.Stderr}");

            var inspectAfterUncheck = CliProcess.RunWithServer($"inspect {enableFeatureRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterUncheck.ExitCode == 0,
                $"inspect(after uncheck) exit={inspectAfterUncheck.ExitCode}\nstdout: {inspectAfterUncheck.Stdout}\nstderr: {inspectAfterUncheck.Stderr}");
            var uncheckedState = ExtractKeyValue(inspectAfterUncheck.Stdout, "state");
            var uncheckedToggle = ExtractKeyValue(inspectAfterUncheck.Stdout, "toggleState")
                ?? ExtractKeyValue(inspectAfterUncheck.Stdout, "value");
            Assert.True(
                StateHasToken(uncheckedState, "unchecked")
                || IsExactToggleState(uncheckedToggle, "off", "false")
                || inspectAfterUncheck.Stdout.Contains("Toggle: Off", StringComparison.Ordinal),
                $"unchecked state not observed in inspect(after uncheck) stdout:\n{inspectAfterUncheck.Stdout}");

            var clickSelectionTabResult = CliProcess.RunWithServer($"click {selectionTabRef}", _fixture.BaseUrl, tempDir);
            Assert.True(clickSelectionTabResult.ExitCode == 0,
                $"click(selection tab) exit={clickSelectionTabResult.ExitCode}\nstdout: {clickSelectionTabResult.Stdout}\nstderr: {clickSelectionTabResult.Stderr}");

            var postTabSnapshotPath = ExtractKeyValue(clickSelectionTabResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(postTabSnapshotPath),
                $"snapshot path not found in click(selection tab) stdout:\n{clickSelectionTabResult.Stdout}");
            postTabSnapshotPath = StripSnapshotNote(postTabSnapshotPath!);
            var resolvedPostTabSnapshot = Path.IsPathRooted(postTabSnapshotPath)
                ? postTabSnapshotPath
                : Path.Combine(tempDir, postTabSnapshotPath);
            Assert.True(File.Exists(resolvedPostTabSnapshot),
                $"post-tab snapshot file not found: {resolvedPostTabSnapshot}");

            var colorsComboRef = FindRefByAutomationId(resolvedPostTabSnapshot, "Selection_ComboBox_Colors");
            Assert.False(string.IsNullOrEmpty(colorsComboRef),
                $"Colors ComboBox ref not found in snapshot file: {resolvedPostTabSnapshot}");

            const string selectedColor = "Blue";
            var selectResult = CliProcess.RunWithServer($"select {colorsComboRef} --name \"{selectedColor}\"", _fixture.BaseUrl, tempDir);
            Assert.True(selectResult.ExitCode == 0,
                $"select exit={selectResult.ExitCode}\nstdout: {selectResult.Stdout}\nstderr: {selectResult.Stderr}");

            var inspectAfterSelect = CliProcess.RunWithServer($"inspect {colorsComboRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterSelect.ExitCode == 0,
                $"inspect(after select) exit={inspectAfterSelect.ExitCode}\nstdout: {inspectAfterSelect.Stdout}\nstderr: {inspectAfterSelect.Stderr}");
            var comboValue = ExtractKeyValue(inspectAfterSelect.Stdout, "value");
            Assert.True(
                (!string.IsNullOrEmpty(comboValue) && comboValue.Contains(selectedColor, StringComparison.Ordinal))
                || inspectAfterSelect.Stdout.Contains($"SelectedItem: \"{selectedColor}\"", StringComparison.Ordinal),
                $"selected color not observed in inspect(after select) stdout:\n{inspectAfterSelect.Stdout}");
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Performs the Doubleclick Mouse Move Down Up Wheel Scroll Into View Scroll On Sample App Succeeds operation.</summary>
    [Fact]
    public async Task DoubleclickMouseMoveDownUpWheelScrollIntoViewScroll_OnSampleApp_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-s05-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        try
        {
            var listResult = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var windowRef = ExtractSampleAppWindowRef(listResult.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in list-windows output:\n{listResult.Stdout}");

            var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            var snapshotPath = ExtractKeyValue(attachResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(snapshotPath),
                $"snapshot path not found in attach stdout:\n{attachResult.Stdout}");
            snapshotPath = StripSnapshotNote(snapshotPath!);
            var resolvedSnapshot = Path.IsPathRooted(snapshotPath)
                ? snapshotPath
                : Path.Combine(tempDir, snapshotPath);
            Assert.True(File.Exists(resolvedSnapshot),
                $"snapshot file not found: {resolvedSnapshot}");

            var dataGridTabRef = FindRefByAutomationId(resolvedSnapshot, "MainWindow_TabItem_DataGrid");
            Assert.False(string.IsNullOrEmpty(dataGridTabRef),
                $"DataGrid tab ref not found in snapshot file: {resolvedSnapshot}");

            var clickDataGridTabResult = CliProcess.RunWithServer($"click {dataGridTabRef}", _fixture.BaseUrl, tempDir);
            Assert.True(clickDataGridTabResult.ExitCode == 0,
                $"click(data-grid tab) exit={clickDataGridTabResult.ExitCode}\nstdout: {clickDataGridTabResult.Stdout}\nstderr: {clickDataGridTabResult.Stderr}");

            var dataGridSnapshotPath = ExtractKeyValue(clickDataGridTabResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(dataGridSnapshotPath),
                $"snapshot path not found in click(data-grid tab) stdout:\n{clickDataGridTabResult.Stdout}");
            dataGridSnapshotPath = StripSnapshotNote(dataGridSnapshotPath!);
            var resolvedDataGridSnapshot = Path.IsPathRooted(dataGridSnapshotPath)
                ? dataGridSnapshotPath
                : Path.Combine(tempDir, dataGridSnapshotPath);
            Assert.True(File.Exists(resolvedDataGridSnapshot),
                $"data-grid snapshot file not found: {resolvedDataGridSnapshot}");

            var (dataGridRef, dataItemRef, resolvedDataGridSnapshotFinal) =
                await FindDataGridRefsWithRetryAsync(tempDir, _fixture.BaseUrl, resolvedDataGridSnapshot, maxAttempts: 5, retryDelayMs: 200);
            Assert.False(string.IsNullOrEmpty(dataGridRef),
                $"DataGrid ref not found in snapshot file after retry: {resolvedDataGridSnapshotFinal}");
            Assert.False(string.IsNullOrEmpty(dataItemRef),
                $"DataGrid DataItem ref not found in snapshot file after retry: {resolvedDataGridSnapshotFinal}");

            var doubleClickResult = CliProcess.RunWithServer($"doubleclick {dataItemRef}", _fixture.BaseUrl, tempDir);
            Assert.True(doubleClickResult.ExitCode == 0,
                $"doubleclick exit={doubleClickResult.ExitCode}\nstdout: {doubleClickResult.Stdout}\nstderr: {doubleClickResult.Stderr}");
            var doubleClickSnapshotPath = ExtractKeyValue(doubleClickResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(doubleClickSnapshotPath),
                $"snapshot path not found in doubleclick stdout:\n{doubleClickResult.Stdout}");
            _ = ResolveSnapshotPathAndAssertExists(tempDir, doubleClickSnapshotPath!, "doubleclick", doubleClickResult.Stdout);

            var inspectAfterDoubleClick = CliProcess.RunWithServer($"inspect {dataItemRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterDoubleClick.ExitCode == 0,
                $"inspect(after doubleclick) exit={inspectAfterDoubleClick.ExitCode}\nstdout: {inspectAfterDoubleClick.Stdout}\nstderr: {inspectAfterDoubleClick.Stderr}");
            var stateAfterDoubleClick = ExtractKeyValue(inspectAfterDoubleClick.Stdout, "state");
            var isSelectedOrFocused = StateHasToken(stateAfterDoubleClick, "selected")
                || StateHasToken(stateAfterDoubleClick, "focused")
                || inspectAfterDoubleClick.Stdout.Contains("SelectionItem: Selected", StringComparison.Ordinal);
            Assert.True(
                isSelectedOrFocused,
                $"expected selected/focused state token after doubleclick, actual state='{stateAfterDoubleClick}'\nstdout:\n{inspectAfterDoubleClick.Stdout}");

            var hoverGridResult = CliProcess.RunWithServer($"hover {dataGridRef}", _fixture.BaseUrl, tempDir);
            Assert.True(hoverGridResult.ExitCode == 0,
                $"hover(data-grid) exit={hoverGridResult.ExitCode}\nstdout: {hoverGridResult.Stdout}\nstderr: {hoverGridResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, hoverGridResult.Stdout, "hover(data-grid)");

            var moveResult = CliProcess.RunWithServer("mousemove 200,200", _fixture.BaseUrl, tempDir);
            Assert.True(moveResult.ExitCode == 0,
                $"mousemove exit={moveResult.ExitCode}\nstdout: {moveResult.Stdout}\nstderr: {moveResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, moveResult.Stdout, "mousemove");

            var downResult = CliProcess.RunWithServer("mousedown --button left", _fixture.BaseUrl, tempDir);
            Assert.True(downResult.ExitCode == 0,
                $"mousedown exit={downResult.ExitCode}\nstdout: {downResult.Stdout}\nstderr: {downResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, downResult.Stdout, "mousedown");

            var upResult = CliProcess.RunWithServer("mouseup --button left", _fixture.BaseUrl, tempDir);
            Assert.True(upResult.ExitCode == 0,
                $"mouseup exit={upResult.ExitCode}\nstdout: {upResult.Stdout}\nstderr: {upResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, upResult.Stdout, "mouseup");

            var wheelResult = CliProcess.RunWithServer("mousewheel --delta-y 3", _fixture.BaseUrl, tempDir);
            Assert.True(wheelResult.ExitCode == 0,
                $"mousewheel exit={wheelResult.ExitCode}\nstdout: {wheelResult.Stdout}\nstderr: {wheelResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, wheelResult.Stdout, "mousewheel");

            var scrollIntoViewResult = CliProcess.RunWithServer($"scroll-into-view {dataItemRef}", _fixture.BaseUrl, tempDir);
            Assert.True(scrollIntoViewResult.ExitCode == 0,
                $"scroll-into-view exit={scrollIntoViewResult.ExitCode}\nstdout: {scrollIntoViewResult.Stdout}\nstderr: {scrollIntoViewResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, scrollIntoViewResult.Stdout, "scroll-into-view");

            var scrollResult = CliProcess.RunWithServer($"scroll {dataGridRef} --small-v 2", _fixture.BaseUrl, tempDir);
            Assert.True(scrollResult.ExitCode == 0,
                $"scroll exit={scrollResult.ExitCode}\nstdout: {scrollResult.Stdout}\nstderr: {scrollResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, scrollResult.Stdout, "scroll");

            var inspectAfterLowLevelOps = CliProcess.RunWithServer($"inspect {dataItemRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterLowLevelOps.ExitCode == 0,
                $"inspect(after low-level ops) exit={inspectAfterLowLevelOps.ExitCode}\nstdout: {inspectAfterLowLevelOps.Stdout}\nstderr: {inspectAfterLowLevelOps.Stderr}");
            Assert.Contains("controlType:", inspectAfterLowLevelOps.Stdout, StringComparison.Ordinal);

            var snapshotAfterScroll = CliProcess.RunWithServer("snapshot", _fixture.BaseUrl, tempDir);
            Assert.True(snapshotAfterScroll.ExitCode == 0,
                $"snapshot(after scroll) exit={snapshotAfterScroll.ExitCode}\nstdout: {snapshotAfterScroll.Stdout}\nstderr: {snapshotAfterScroll.Stderr}");

            var snapshotAfterScrollPath = ExtractKeyValue(snapshotAfterScroll.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(snapshotAfterScrollPath),
                $"snapshot path not found in snapshot(after scroll) stdout:\n{snapshotAfterScroll.Stdout}");
            snapshotAfterScrollPath = StripSnapshotNote(snapshotAfterScrollPath!);
            var resolvedAfterScrollSnapshot = Path.IsPathRooted(snapshotAfterScrollPath)
                ? snapshotAfterScrollPath
                : Path.Combine(tempDir, snapshotAfterScrollPath);
            Assert.True(File.Exists(resolvedAfterScrollSnapshot),
                $"snapshot(after scroll) file not found: {resolvedAfterScrollSnapshot}");

            var afterScrollSnapshotText = File.ReadAllText(resolvedAfterScrollSnapshot);
            Assert.Contains("[ref=", afterScrollSnapshotText, StringComparison.Ordinal);
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Performs the Resize Minimize Maximize Restore Window On Sample App Succeeds operation.</summary>
    [Fact]
    public async Task ResizeMinimizeMaximizeRestoreWindow_OnSampleApp_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-s06-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        try
        {
            var listResult = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var windowRef = ExtractSampleAppWindowRef(listResult.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in list-windows output:\n{listResult.Stdout}");

            var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            var sessionId = ExtractKeyValue(attachResult.Stdout, "sessionId");
            Assert.False(string.IsNullOrEmpty(sessionId),
                $"sessionId not found in attach stdout:\n{attachResult.Stdout}");

            var attachSnapshotPath = ExtractKeyValue(attachResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(attachSnapshotPath),
                $"snapshot path not found in attach stdout:\n{attachResult.Stdout}");
            var resolvedAttachSnapshot = ResolveSnapshotPathAndAssertExists(tempDir, attachSnapshotPath!, "attach", attachResult.Stdout);

            var rootWindowElementRef = FindFirstRefByRole(resolvedAttachSnapshot, "Window");
            Assert.False(string.IsNullOrEmpty(rootWindowElementRef),
                $"Window element ref not found in snapshot file: {resolvedAttachSnapshot}");

            var inspectBeforeResize = CliProcess.RunWithServer($"inspect {rootWindowElementRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectBeforeResize.ExitCode == 0,
                $"inspect(before resize) exit={inspectBeforeResize.ExitCode}\nstdout: {inspectBeforeResize.Stdout}\nstderr: {inspectBeforeResize.Stderr}");

            var beforeSize = TryExtractWindowSizeFromInspect(inspectBeforeResize.Stdout);

            const int targetWidth = 900;
            const int targetHeight = 700;
            var resizeResult = CliProcess.RunWithServer(
                $"resize-window {sessionId} --width {targetWidth} --height {targetHeight}",
                _fixture.BaseUrl,
                tempDir);
            Assert.True(resizeResult.ExitCode == 0,
                $"resize-window exit={resizeResult.ExitCode}\nstdout: {resizeResult.Stdout}\nstderr: {resizeResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, resizeResult.Stdout, "resize-window");

            var inspectAfterResize = CliProcess.RunWithServer($"inspect {rootWindowElementRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterResize.ExitCode == 0,
                $"inspect(after resize) exit={inspectAfterResize.ExitCode}\nstdout: {inspectAfterResize.Stdout}\nstderr: {inspectAfterResize.Stderr}");

            var afterSize = TryExtractWindowSizeFromInspect(inspectAfterResize.Stdout);
            Assert.True(afterSize is not null,
                $"window size could not be extracted from inspect(after resize) stdout:\n{inspectAfterResize.Stdout}");
            Assert.True(afterSize!.Value.width > 0 && afterSize.Value.height > 0,
                $"invalid window size after resize: {afterSize.Value.width}x{afterSize.Value.height}\nstdout:\n{inspectAfterResize.Stdout}");
            if (beforeSize is not null)
            {
                Assert.True(
                    beforeSize.Value.width != afterSize.Value.width || beforeSize.Value.height != afterSize.Value.height,
                    $"window size did not change after resize-window. before={beforeSize.Value.width}x{beforeSize.Value.height}, after={afterSize.Value.width}x{afterSize.Value.height}");
            }

            var maximizeResult = CliProcess.RunWithServer($"maximize-window {sessionId}", _fixture.BaseUrl, tempDir);
            Assert.True(maximizeResult.ExitCode == 0,
                $"maximize-window exit={maximizeResult.ExitCode}\nstdout: {maximizeResult.Stdout}\nstderr: {maximizeResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, maximizeResult.Stdout, "maximize-window");

            var inspectAfterMaximize = CliProcess.RunWithServer($"inspect {rootWindowElementRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterMaximize.ExitCode == 0,
                $"inspect(after maximize) exit={inspectAfterMaximize.ExitCode}\nstdout: {inspectAfterMaximize.Stdout}\nstderr: {inspectAfterMaximize.Stderr}");
            var stateAfterMaximize = ExtractKeyValue(inspectAfterMaximize.Stdout, "state");
            Assert.True(
                StateHasToken(stateAfterMaximize, "maximized")
                || inspectAfterMaximize.Stdout.Contains("WindowVisualState: Maximized", StringComparison.Ordinal)
                || inspectAfterMaximize.Stdout.Contains("VisualState: \"Maximized\"", StringComparison.Ordinal),
                $"maximized state not observed in inspect(after maximize) stdout:\n{inspectAfterMaximize.Stdout}");

            var minimizeResult = CliProcess.RunWithServer($"minimize-window {sessionId}", _fixture.BaseUrl, tempDir);
            Assert.True(minimizeResult.ExitCode == 0,
                $"minimize-window exit={minimizeResult.ExitCode}\nstdout: {minimizeResult.Stdout}\nstderr: {minimizeResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, minimizeResult.Stdout, "minimize-window");

            var inspectAfterMinimize = CliProcess.RunWithServer($"inspect {rootWindowElementRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterMinimize.ExitCode == 0,
                $"inspect(after minimize) exit={inspectAfterMinimize.ExitCode}\nstdout: {inspectAfterMinimize.Stdout}\nstderr: {inspectAfterMinimize.Stderr}");
            var stateAfterMinimize = ExtractKeyValue(inspectAfterMinimize.Stdout, "state");
            Assert.True(
                StateHasToken(stateAfterMinimize, "minimized")
                || inspectAfterMinimize.Stdout.Contains("WindowVisualState: Minimized", StringComparison.Ordinal)
                || inspectAfterMinimize.Stdout.Contains("VisualState: \"Minimized\"", StringComparison.Ordinal),
                $"minimized state not observed in inspect(after minimize) stdout:\n{inspectAfterMinimize.Stdout}");

            var restoreResult = CliProcess.RunWithServer($"restore-window {sessionId}", _fixture.BaseUrl, tempDir);
            Assert.True(restoreResult.ExitCode == 0,
                $"restore-window exit={restoreResult.ExitCode}\nstdout: {restoreResult.Stdout}\nstderr: {restoreResult.Stderr}");
            AssertSnapshotPathIfPresent(tempDir, restoreResult.Stdout, "restore-window");

            var inspectAfterRestore = CliProcess.RunWithServer($"inspect {rootWindowElementRef}", _fixture.BaseUrl, tempDir);
            Assert.True(inspectAfterRestore.ExitCode == 0,
                $"inspect(after restore) exit={inspectAfterRestore.ExitCode}\nstdout: {inspectAfterRestore.Stdout}\nstderr: {inspectAfterRestore.Stderr}");
            var stateAfterRestore = ExtractKeyValue(inspectAfterRestore.Stdout, "state");
            Assert.False(
                StateHasToken(stateAfterRestore, "minimized") || inspectAfterRestore.Stdout.Contains("VisualState: \"Minimized\"", StringComparison.Ordinal),
                $"window still minimized after restore-window:\n{inspectAfterRestore.Stdout}");
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Waits for the Wait For Element And Wait For Window On Sample App Succeeds condition.</summary>
    [Fact]
    public async Task WaitForElementAndWaitForWindow_OnSampleApp_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-s07-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        try
        {
            var listResult = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var windowRef = ExtractSampleAppWindowRef(listResult.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in list-windows output:\n{listResult.Stdout}");

            var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            var sessionId = ExtractKeyValue(attachResult.Stdout, "sessionId");
            Assert.False(string.IsNullOrEmpty(sessionId),
                $"sessionId not found in attach stdout:\n{attachResult.Stdout}");

            var attachSnapshotPath = ExtractKeyValue(attachResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(attachSnapshotPath),
                $"snapshot path not found in attach stdout:\n{attachResult.Stdout}");
            var resolvedAttachSnapshot = ResolveSnapshotPathAndAssertExists(tempDir, attachSnapshotPath!, "attach", attachResult.Stdout);

            var asyncDelayTabRef = FindRefByAutomationId(resolvedAttachSnapshot, "MainWindow_TabItem_AsyncDelay");
            Assert.False(string.IsNullOrEmpty(asyncDelayTabRef),
                $"Async/Delay tab ref not found in snapshot file: {resolvedAttachSnapshot}");

            var clickAsyncTabResult = CliProcess.RunWithServer($"click {asyncDelayTabRef}", _fixture.BaseUrl, tempDir);
            Assert.True(clickAsyncTabResult.ExitCode == 0,
                $"click(async-delay tab) exit={clickAsyncTabResult.ExitCode}\nstdout: {clickAsyncTabResult.Stdout}\nstderr: {clickAsyncTabResult.Stderr}");

            var asyncTabSnapshotPath = ExtractKeyValue(clickAsyncTabResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(asyncTabSnapshotPath),
                $"snapshot path not found in click(async-delay tab) stdout:\n{clickAsyncTabResult.Stdout}");
            var resolvedAsyncTabSnapshot = ResolveSnapshotPathAndAssertExists(tempDir, asyncTabSnapshotPath!, "click(async-delay tab)", clickAsyncTabResult.Stdout);

            var startLongTaskRef = FindRefByAutomationId(resolvedAsyncTabSnapshot, "AsyncDelay_Button_StartLongTask");
            Assert.False(string.IsNullOrEmpty(startLongTaskRef),
                $"Start Long Task button ref not found in snapshot file: {resolvedAsyncTabSnapshot}");

            var clickStartResult = CliProcess.RunWithServer($"click {startLongTaskRef}", _fixture.BaseUrl, tempDir);
            Assert.True(clickStartResult.ExitCode == 0,
                $"click(start long task) exit={clickStartResult.ExitCode}\nstdout: {clickStartResult.Stdout}\nstderr: {clickStartResult.Stderr}");

            var waitForElementResult = CliProcess.RunWithServer(
                $"wait-for-element --automation-id AsyncDelay_DataGrid_CompletedResults --state visible --sid {sessionId} --timeout 7000",
                _fixture.BaseUrl,
                tempDir);
            Assert.True(waitForElementResult.ExitCode == 0,
                $"wait-for-element exit={waitForElementResult.ExitCode}\nstdout: {waitForElementResult.Stdout}\nstderr: {waitForElementResult.Stderr}");
            var waitedElementState = ExtractKeyValue(waitForElementResult.Stdout, "state");
            Assert.Equal("visible", waitedElementState);
            var waitedElementRef = ExtractKeyValue(waitForElementResult.Stdout, "ref");
            Assert.False(string.IsNullOrEmpty(waitedElementRef),
                $"ref not found in wait-for-element stdout:\n{waitForElementResult.Stdout}");

            var snapshotAfterWaitResult = CliProcess.RunWithServer("snapshot", _fixture.BaseUrl, tempDir);
            Assert.True(snapshotAfterWaitResult.ExitCode == 0,
                $"snapshot(after wait-for-element) exit={snapshotAfterWaitResult.ExitCode}\nstdout: {snapshotAfterWaitResult.Stdout}\nstderr: {snapshotAfterWaitResult.Stderr}");
            var snapshotAfterWaitPath = ExtractKeyValue(snapshotAfterWaitResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(snapshotAfterWaitPath),
                $"snapshot path not found in snapshot(after wait-for-element) stdout:\n{snapshotAfterWaitResult.Stdout}");
            var resolvedAfterWaitSnapshot = ResolveSnapshotPathAndAssertExists(tempDir, snapshotAfterWaitPath!, "snapshot(after wait-for-element)", snapshotAfterWaitResult.Stdout);

            var multiWindowTabRef = FindRefByAutomationId(resolvedAfterWaitSnapshot, "MainWindow_TabItem_MultiWindow");
            Assert.False(string.IsNullOrEmpty(multiWindowTabRef),
                $"Multi-Window tab ref not found in snapshot file: {resolvedAfterWaitSnapshot}");

            var clickMultiWindowTabResult = CliProcess.RunWithServer($"click {multiWindowTabRef}", _fixture.BaseUrl, tempDir);
            Assert.True(clickMultiWindowTabResult.ExitCode == 0,
                $"click(multi-window tab) exit={clickMultiWindowTabResult.ExitCode}\nstdout: {clickMultiWindowTabResult.Stdout}\nstderr: {clickMultiWindowTabResult.Stderr}");

            var multiWindowSnapshotPath = ExtractKeyValue(clickMultiWindowTabResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(multiWindowSnapshotPath),
                $"snapshot path not found in click(multi-window tab) stdout:\n{clickMultiWindowTabResult.Stdout}");
            var resolvedMultiWindowSnapshot = ResolveSnapshotPathAndAssertExists(tempDir, multiWindowSnapshotPath!, "click(multi-window tab)", clickMultiWindowTabResult.Stdout);

            var openModelessButtonRef = FindRefByAutomationId(resolvedMultiWindowSnapshot, "MultiWindow_Button_OpenModeless");
            Assert.False(string.IsNullOrEmpty(openModelessButtonRef),
                $"Open Modeless button ref not found in snapshot file: {resolvedMultiWindowSnapshot}");

            var openModelessResult = CliProcess.RunWithServer($"click {openModelessButtonRef}", _fixture.BaseUrl, tempDir);
            Assert.True(openModelessResult.ExitCode == 0,
                $"click(open modeless) exit={openModelessResult.ExitCode}\nstdout: {openModelessResult.Stdout}\nstderr: {openModelessResult.Stderr}");

            var modelessWindow = WaitForWindowInListWindows(
                _fixture.BaseUrl,
                tempDir,
                timeout: TimeSpan.FromSeconds(8),
                pollInterval: TimeSpan.FromMilliseconds(200));
            Assert.NotNull(modelessWindow);

            var waitForWindowResult = CliProcess.RunWithServer(
                $"wait-for-window --title \"{EscapeCliArg(modelessWindow!.WindowTitle)}\" --class-name \"{EscapeCliArg(modelessWindow.ClassName)}\" --process-name \"{EscapeCliArg(modelessWindow.ProcessName)}\" --timeout 10000",
                _fixture.BaseUrl,
                tempDir);
            Assert.True(waitForWindowResult.ExitCode == 0,
                $"wait-for-window exit={waitForWindowResult.ExitCode}\nstdout: {waitForWindowResult.Stdout}\nstderr: {waitForWindowResult.Stderr}");

            var matchedWindowTitle = ExtractKeyValue(waitForWindowResult.Stdout, "windowTitle");
            Assert.Equal(modelessWindow.WindowTitle, matchedWindowTitle);
            var matchedProcessName = ExtractKeyValue(waitForWindowResult.Stdout, "processName");
            Assert.False(string.IsNullOrEmpty(matchedProcessName),
                $"processName not found in wait-for-window stdout:\n{waitForWindowResult.Stdout}");
            Assert.Equal(modelessWindow.ProcessName, matchedProcessName);

            var matchedClassName = ExtractKeyValue(waitForWindowResult.Stdout, "className");
            Assert.False(string.IsNullOrEmpty(matchedClassName),
                $"className not found in wait-for-window stdout:\n{waitForWindowResult.Stdout}");
            Assert.Equal(modelessWindow.ClassName, matchedClassName);
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Performs the Detach And Close Window On Sample App Succeeds operation.</summary>
    [Fact]
    public async Task DetachAndCloseWindow_OnSampleApp_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-s08-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var sampleApp = await SampleAppTestHelper.StartFreshSampleAppAsync(TimeSpan.FromSeconds(10));
        try
        {
            var listResult = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var windowRef = ExtractSampleAppWindowRef(listResult.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRef),
                $"SampleApp windowRef not found in list-windows output:\n{listResult.Stdout}");

            var attachResult = CliProcess.RunWithServer($"attach {windowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            var sessionId = ExtractKeyValue(attachResult.Stdout, "sessionId");
            Assert.False(string.IsNullOrEmpty(sessionId),
                $"sessionId not found in attach stdout:\n{attachResult.Stdout}");

            var detachResult = CliProcess.RunWithServer($"detach {sessionId}", _fixture.BaseUrl, tempDir);
            Assert.True(detachResult.ExitCode == 0,
                $"detach exit={detachResult.ExitCode}\nstdout: {detachResult.Stdout}\nstderr: {detachResult.Stderr}");
            Assert.Contains("detached", detachResult.Stdout, StringComparison.OrdinalIgnoreCase);

            var listAfterDetach = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listAfterDetach.ExitCode == 0,
                $"list-windows(after detach) exit={listAfterDetach.ExitCode}\nstdout: {listAfterDetach.Stdout}\nstderr: {listAfterDetach.Stderr}");

            var windowRefAfterDetach = ExtractSampleAppWindowRef(listAfterDetach.Stdout);
            Assert.False(string.IsNullOrEmpty(windowRefAfterDetach),
                $"SampleApp windowRef not found after detach:\n{listAfterDetach.Stdout}");

            var reattachResult = CliProcess.RunWithServer($"attach {windowRefAfterDetach}", _fixture.BaseUrl, tempDir);
            Assert.True(reattachResult.ExitCode == 0,
                $"reattach exit={reattachResult.ExitCode}\nstdout: {reattachResult.Stdout}\nstderr: {reattachResult.Stderr}");

            var reattachedSessionId = ExtractKeyValue(reattachResult.Stdout, "sessionId");
            Assert.False(string.IsNullOrEmpty(reattachedSessionId),
                $"sessionId not found in reattach stdout:\n{reattachResult.Stdout}");

            var closeResult = CliProcess.RunWithServer($"close-window {reattachedSessionId}", _fixture.BaseUrl, tempDir);
            Assert.True(closeResult.ExitCode == 0,
                $"close-window exit={closeResult.ExitCode}\nstdout: {closeResult.Stdout}\nstderr: {closeResult.Stderr}");
            Assert.Contains("closed", closeResult.Stdout, StringComparison.OrdinalIgnoreCase);

            await SampleAppTestHelper.WaitUntilAsync(
                condition: () => Task.FromResult(Process.GetProcessesByName("SampleApp").Length == 0),
                timeout: TimeSpan.FromSeconds(8),
                failureMessage: "SampleApp process remained after close-window.");

            var listAfterClose = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listAfterClose.ExitCode == 0,
                $"list-windows(after close-window) exit={listAfterClose.ExitCode}\nstdout: {listAfterClose.Stdout}\nstderr: {listAfterClose.Stderr}");
            Assert.DoesNotContain("ADACT SampleApp", listAfterClose.Stdout, StringComparison.Ordinal);

            var attachAfterClose = CliProcess.RunWithServer($"attach {windowRefAfterDetach}", _fixture.BaseUrl, tempDir);
            Assert.NotEqual(0, attachAfterClose.ExitCode);
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { sampleApp?.Dispose(); } catch { }
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Performs the Launch And Kill On Dedicated Sample App Process Succeeds operation.</summary>
    [Fact]
    public async Task LaunchAndKill_OnDedicatedSampleAppProcess_Succeeds()
    {
        using var _appLock = new SampleAppMutex();

        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-s09-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        SampleAppTestHelper.KillSampleAppProcesses();
        await SampleAppTestHelper.WaitUntilAsync(
            condition: () => Task.FromResult(Process.GetProcessesByName("SampleApp").Length == 0),
            timeout: TimeSpan.FromSeconds(5),
            failureMessage: "Existing SampleApp processes did not exit before launch test.");

        var sampleAppExe = FindSampleAppExecutablePathForLaunch();
        try
        {
            var launchResult = CliProcess.RunWithServer($"launch \"{sampleAppExe}\"", _fixture.BaseUrl, tempDir);
            Assert.True(launchResult.ExitCode == 0,
                $"launch exit={launchResult.ExitCode}\nstdout: {launchResult.Stdout}\nstderr: {launchResult.Stderr}");

            var launchedPidText = ExtractKeyValue(launchResult.Stdout, "pid");
            Assert.True(int.TryParse(launchedPidText, out var launchedPid),
                $"pid not found/invalid in launch stdout:\n{launchResult.Stdout}");
            Assert.Contains("processName: SampleApp", launchResult.Stdout, StringComparison.Ordinal);

            await SampleAppTestHelper.WaitUntilAsync(
                condition: () => Task.FromResult(Process.GetProcessesByName("SampleApp").Any(p => p.Id == launchedPid)),
                timeout: TimeSpan.FromSeconds(8),
                failureMessage: $"Launched SampleApp pid={launchedPid} did not appear.");

            var listedWindow = WaitForWindowByProcessIdInListWindows(
                _fixture.BaseUrl,
                tempDir,
                launchedPid,
                timeout: TimeSpan.FromSeconds(8),
                pollInterval: TimeSpan.FromMilliseconds(200));
            Assert.NotNull(listedWindow);
            Assert.Equal("SampleApp", listedWindow!.ProcessName, ignoreCase: true);

            var attachResult = CliProcess.RunWithServer($"attach {listedWindow.WindowRef}", _fixture.BaseUrl, tempDir);
            Assert.True(attachResult.ExitCode == 0,
                $"attach exit={attachResult.ExitCode}\nstdout: {attachResult.Stdout}\nstderr: {attachResult.Stderr}");

            var sessionId = ExtractKeyValue(attachResult.Stdout, "sessionId");
            Assert.False(string.IsNullOrEmpty(sessionId),
                $"sessionId not found in attach stdout:\n{attachResult.Stdout}");

            var killResult = CliProcess.RunWithServer($"kill {sessionId}", _fixture.BaseUrl, tempDir);
            Assert.True(killResult.ExitCode == 0,
                $"kill exit={killResult.ExitCode}\nstdout: {killResult.Stdout}\nstderr: {killResult.Stderr}");
            Assert.Contains("killed: true", killResult.Stdout, StringComparison.Ordinal);
            Assert.Contains("detached: true", killResult.Stdout, StringComparison.Ordinal);

            await SampleAppTestHelper.WaitUntilAsync(
                condition: () => Task.FromResult(!Process.GetProcessesByName("SampleApp").Any(p => p.Id == launchedPid)),
                timeout: TimeSpan.FromSeconds(8),
                failureMessage: $"Launched SampleApp pid={launchedPid} remained after kill.");

            var listAfterKill = CliProcess.RunWithServer("list-windows", _fixture.BaseUrl, tempDir);
            Assert.True(listAfterKill.ExitCode == 0,
                $"list-windows(after kill) exit={listAfterKill.ExitCode}\nstdout: {listAfterKill.Stdout}\nstderr: {listAfterKill.Stderr}");
            var rowsAfterKill = ParseListWindowsRows(listAfterKill.Stdout);
            Assert.DoesNotContain(rowsAfterKill, r => r.ProcessId == launchedPid);
        }
        finally
        {
            SampleAppTestHelper.KillSampleAppProcesses();
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    /// <summary>Performs the Install Skills Creates Expected Skill Directories Under Client Relative Root operation.</summary>
    [Theory]
    [InlineData("copilot", ".github/skills")]
    [InlineData("claude", ".claude/skills")]
    [InlineData("codex", ".agents/skills")]
    public void InstallSkills_CreatesExpectedSkillDirectories_UnderClientRelativeRoot(string client, string expectedSkillsRootRelative)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "adact-cli-e2e-s10-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var installResult = CliProcess.Run($"install --skills {client}", workingDirectory: tempDir);
            Assert.True(installResult.ExitCode == 0,
                $"install exit={installResult.ExitCode}\nstdout: {installResult.Stdout}\nstderr: {installResult.Stderr}");
            Assert.Contains("installed: true", installResult.Stdout, StringComparison.Ordinal);

            var expectedSkillsRoot = Path.GetFullPath(Path.Combine(
                tempDir,
                expectedSkillsRootRelative.Replace('/', Path.DirectorySeparatorChar)));
            var pathOutput = ExtractKeyValue(installResult.Stdout, "path");
            Assert.False(string.IsNullOrEmpty(pathOutput),
                $"path not found in install stdout:\n{installResult.Stdout}");
            Assert.Equal(expectedSkillsRoot, Path.GetFullPath(pathOutput!));

            Assert.True(Directory.Exists(expectedSkillsRoot), $"skills root missing: {expectedSkillsRoot}");

            var adactCliDir = Path.Combine(expectedSkillsRoot, "adact-cli");
            var flauiTestgenDir = Path.Combine(expectedSkillsRoot, "adact-flaui-testgen");
            Assert.True(Directory.Exists(adactCliDir), $"adact-cli directory missing: {adactCliDir}");
            Assert.True(Directory.Exists(flauiTestgenDir), $"adact-flaui-testgen directory missing: {flauiTestgenDir}");

            Assert.True(File.Exists(Path.Combine(adactCliDir, "SKILL.md")), "adact-cli/SKILL.md missing");
            Assert.True(File.Exists(Path.Combine(flauiTestgenDir, "SKILL.md")), "adact-flaui-testgen/SKILL.md missing");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }

        Assert.False(Directory.Exists(tempDir), $"temp directory should be cleaned up: {tempDir}");
    }

    private static async Task WaitForNamedPipeServerAsync(NamedPipeEndPoint endpoint, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (await NamedPipeMcpClient.IsServerRunningAsync(endpoint, timeoutMs: 100, ct))
            {
                return;
            }

            await Task.Delay(100, ct);
        }

        throw new OperationCanceledException("Named Pipe server did not become ready within timeout.");
    }

    private static string? ExtractSampleAppWindowRef(string stdout)
    {
        var inBody = false;
        var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line == "---") { inBody = true; continue; }
            if (!inBody) continue;

            var cols = line.Split('\t');
            if (cols.Length < 6) continue;

            var processName = cols[2];
            var windowTitle = cols[5];

            if (processName.Contains("SampleApp", StringComparison.OrdinalIgnoreCase)
                || windowTitle.Contains("ADACT SampleApp", StringComparison.Ordinal))
            {
                return cols[0];
            }
        }
        return null;
    }

    private sealed record ListWindowsRow(string WindowRef, int? ProcessId, string ProcessName, string ClassName, string WindowTitle);

    private static ListWindowsRow? WaitForWindowByProcessIdInListWindows(
        string baseUrl,
        string tempDir,
        int processId,
        TimeSpan timeout,
        TimeSpan pollInterval)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var listResult = CliProcess.RunWithServer("list-windows", baseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows(poll for launched pid) exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var rows = ParseListWindowsRows(listResult.Stdout);
            var matched = rows.FirstOrDefault(r => r.ProcessId == processId);
            if (matched is not null)
            {
                return matched;
            }

            Thread.Sleep(pollInterval);
        }

        return null;
    }

    private static ListWindowsRow? WaitForWindowInListWindows(
        string baseUrl,
        string tempDir,
        TimeSpan timeout,
        TimeSpan pollInterval)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var listResult = CliProcess.RunWithServer("list-windows", baseUrl, tempDir);
            Assert.True(listResult.ExitCode == 0,
                $"list-windows(poll for modeless window) exit={listResult.ExitCode}\nstdout: {listResult.Stdout}\nstderr: {listResult.Stderr}");

            var rows = ParseListWindowsRows(listResult.Stdout);
            var modelessRow = rows.FirstOrDefault(static r =>
                r.ProcessName.Contains("SampleApp", StringComparison.OrdinalIgnoreCase)
                && r.WindowTitle.Contains("Modeless", StringComparison.OrdinalIgnoreCase));

            if (modelessRow is not null)
            {
                return modelessRow;
            }

            Thread.Sleep(pollInterval);
        }

        return null;
    }

    private static List<ListWindowsRow> ParseListWindowsRows(string stdout)
    {
        var rows = new List<ListWindowsRow>();
        var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var inBody = false;
        foreach (var line in lines)
        {
            if (line == "---") { inBody = true; continue; }
            if (!inBody) continue;

            var cols = line.Split('\t');
            if (cols.Length < 6) continue;

            int? processId = null;
            if (int.TryParse(cols[3], out var pid))
            {
                processId = pid;
            }

            rows.Add(new ListWindowsRow(
                WindowRef: cols[0],
                ProcessId: processId,
                ProcessName: cols[2],
                ClassName: cols[4],
                WindowTitle: cols[5]));
        }

        return rows;
    }

    private static string EscapeCliArg(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string FindSampleAppExecutablePathForLaunch()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && dir.GetFiles("adact.sln*").Length == 0)
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new FileNotFoundException("adact.sln not found. Cannot locate SampleApp.exe.");
        }

        var exePath = Path.Combine(
            dir.FullName,
            "test-apps",
            "SampleApp",
            "bin",
            "Debug",
            "net10.0-windows",
            "SampleApp.exe");

        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"SampleApp.exe not found at expected path: {exePath}. Build the solution first.");
        }

        return exePath;
    }

    private static string? ExtractKeyValue(string stdout, string key)
    {
        var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var idx = line.IndexOf(": ", StringComparison.Ordinal);
            if (idx <= 0) continue;
            if (!string.Equals(line[..idx], key, StringComparison.Ordinal)) continue;
            var value = line[(idx + 2)..];
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1];
            return value;
        }
        return null;
    }

    private static bool StateHasToken(string? state, string expectedToken)
    {
        if (string.IsNullOrWhiteSpace(state)) return false;

        foreach (var token in state.Split([',', ';', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(token, expectedToken, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsExactToggleState(string? value, params string[] expectedValues)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        foreach (var expected in expectedValues)
        {
            if (string.Equals(value, expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static (int width, int height)? TryExtractWindowSizeFromInspect(string stdout)
    {
        var widthText = ExtractKeyValue(stdout, "width");
        var heightText = ExtractKeyValue(stdout, "height");
        if (int.TryParse(widthText, out var width) && int.TryParse(heightText, out var height))
        {
            return (width, height);
        }

        var boundsText = ExtractKeyValue(stdout, "boundingRect")
            ?? ExtractKeyValue(stdout, "bounds");
        if (string.IsNullOrWhiteSpace(boundsText))
        {
            return null;
        }

        var nums = Regex.Matches(boundsText, @"-?\d+")
            .Select(m => int.Parse(m.Value))
            .ToArray();

        if (nums.Length >= 4)
        {
            return (Math.Abs(nums[2]), Math.Abs(nums[3]));
        }

        return null;
    }

    private static string StripSnapshotNote(string path)
    {
        const string changed = " (changed)";
        const string unchanged = " (unchanged)";
        if (path.EndsWith(changed, StringComparison.Ordinal))
            return path[..^changed.Length];
        if (path.EndsWith(unchanged, StringComparison.Ordinal))
            return path[..^unchanged.Length];
        return path;
    }

    private static string ResolveSnapshotPathAndAssertExists(string tempDir, string snapshotPathRaw, string commandName, string stdout)
    {
        var snapshotPath = StripSnapshotNote(snapshotPathRaw);
        var resolvedSnapshot = Path.IsPathRooted(snapshotPath)
            ? snapshotPath
            : Path.Combine(tempDir, snapshotPath);
        Assert.True(File.Exists(resolvedSnapshot),
            $"{commandName} snapshot file not found: {resolvedSnapshot}\nstdout: {stdout}");
        return resolvedSnapshot;
    }

    private static void AssertSnapshotPathIfPresent(string tempDir, string stdout, string commandName)
    {
        var snapshotPath = ExtractKeyValue(stdout, "snapshotPath");
        if (string.IsNullOrEmpty(snapshotPath))
        {
            return;
        }

        _ = ResolveSnapshotPathAndAssertExists(tempDir, snapshotPath, commandName, stdout);
    }

    private sealed record SnapshotLine(string Role, string? Name, string? AutomationId, string? Ref);

    private static string? FindSubmitButtonRef(string snapshotFilePath)
    {
        foreach (var line in ReadSnapshotLines(snapshotFilePath))
        {
            if (line.AutomationId == "BasicControls_Button_Submit") return line.Ref;
        }
        foreach (var line in ReadSnapshotLines(snapshotFilePath))
        {
            if (line.Role == "Button" && !string.IsNullOrEmpty(line.Ref)) return line.Ref;
        }
        return null;
    }

    private static string? FindRefByAutomationId(string snapshotFilePath, string automationId)
    {
        foreach (var line in ReadSnapshotLines(snapshotFilePath))
        {
            if (string.Equals(line.AutomationId, automationId, StringComparison.Ordinal)) return line.Ref;
        }

        return null;
    }

    private static string? FindFirstRefByRole(string snapshotFilePath, string role)
    {
        foreach (var line in ReadSnapshotLines(snapshotFilePath))
        {
            if (string.Equals(line.Role, role, StringComparison.Ordinal) && !string.IsNullOrEmpty(line.Ref))
            {
                return line.Ref;
            }
        }

        return null;
    }

    private static async Task<(string? dataGridRef, string? dataItemRef, string snapshotPath)> FindDataGridRefsWithRetryAsync(
        string tempDir,
        string baseUrl,
        string initialSnapshotPath,
        int maxAttempts,
        int retryDelayMs)
    {
        var currentSnapshotPath = initialSnapshotPath;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var dataGridRef = FindRefByAutomationId(currentSnapshotPath, "DataGrid_Grid_Main")
                ?? FindRefByAutomationId(currentSnapshotPath, "DataGrid_DataGrid_Main")
                ?? FindFirstRefByRole(currentSnapshotPath, "DataGrid");
            var dataItemRef = FindFirstRefByRole(currentSnapshotPath, "DataItem");
            if (!string.IsNullOrEmpty(dataGridRef) && !string.IsNullOrEmpty(dataItemRef))
            {
                return (dataGridRef, dataItemRef, currentSnapshotPath);
            }

            if (attempt == maxAttempts)
            {
                return (dataGridRef, dataItemRef, currentSnapshotPath);
            }

            await Task.Delay(retryDelayMs);
            var snapshotResult = CliProcess.RunWithServer("snapshot", baseUrl, tempDir);
            Assert.True(snapshotResult.ExitCode == 0,
                $"snapshot(retry for data-grid/data-item) exit={snapshotResult.ExitCode}\nstdout: {snapshotResult.Stdout}\nstderr: {snapshotResult.Stderr}");

            var snapshotPath = ExtractKeyValue(snapshotResult.Stdout, "snapshotPath");
            Assert.False(string.IsNullOrEmpty(snapshotPath),
                $"snapshot path not found in snapshot(retry) stdout:\n{snapshotResult.Stdout}");
            currentSnapshotPath = ResolveSnapshotPathAndAssertExists(tempDir, snapshotPath!, "snapshot(retry)", snapshotResult.Stdout);
        }

        return (null, null, currentSnapshotPath);
    }

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

    private static bool IsPngFile(string filePath)
    {
        var expectedHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var actual = File.ReadAllBytes(filePath);
        if (actual.Length < expectedHeader.Length) return false;
        for (int i = 0; i < expectedHeader.Length; i++)
        {
            if (actual[i] != expectedHeader[i]) return false;
        }
        return true;
    }
}
