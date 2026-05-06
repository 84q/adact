namespace Adact.Tests.Common;

/// <summary>
/// SampleApp を使うテストを system-wide に直列化するための named semaphore ヘルパー。
/// 異なる VSTest プロセス (アセンブリ間並列) でも有効。
/// async/await でスレッドが切り替わっても解放できるよう Mutex ではなく Semaphore を使用する。
/// </summary>
public sealed class SampleAppMutex : IDisposable
{
    private const string SemaphoreName = @"Global\AdactSampleAppE2E";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(60);

    private readonly Semaphore _semaphore;
    private bool _owned;

    /// <summary>
    /// named semaphore を取得する。
    /// </summary>
    /// <exception cref="TimeoutException">タイムアウトした場合。</exception>
    public SampleAppMutex()
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

    /// <summary>
    /// semaphore を解放する。
    /// </summary>
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
