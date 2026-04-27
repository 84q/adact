using System;
using System.Threading;

namespace Adact.Mcp.Stdio.Tests;

/// <summary>
/// calc.exe を使う E2E テストを system-wide に直列化するための named semaphore ヘルパー。
/// 異なる VSTest プロセス (アセンブリ間並列) でも有効。
/// async/await でスレッドが切り替わっても解放できるよう Mutex ではなく Semaphore を使用する。
/// </summary>
internal sealed class CalculatorMutex : IDisposable
{
    private const string SemaphoreName = @"Global\AdactCalculatorE2E";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(60);

    private readonly Semaphore _semaphore;
    private bool _owned;

    public CalculatorMutex()
    {
        _semaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: SemaphoreName);
        _owned = _semaphore.WaitOne(WaitTimeout);
        if (!_owned)
        {
            _semaphore.Dispose();
            throw new TimeoutException(
                $"Failed to acquire {SemaphoreName} within {WaitTimeout.TotalSeconds}s.");
        }
    }

    public void Dispose()
    {
        if (_owned)
        {
            try { _semaphore.Release(); } catch { }
            _owned = false;
        }
        _semaphore.Dispose();
    }
}
