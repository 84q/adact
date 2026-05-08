namespace Adact.Tests.Common;

/// <summary>Provides a mutex wrapper for tests.</summary>
public sealed class SampleAppMutex : IDisposable
{
    private const string SemaphoreName = @"Global\AdactSampleAppE2E";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(60);

    private readonly Semaphore _semaphore;
    private bool _owned;

    /// <summary>Initializes a new instance of the Sample App Mutex class.</summary>
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

    /// <summary>Releases resources.</summary>
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
