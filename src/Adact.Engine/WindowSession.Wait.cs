using System.Diagnostics;

using Adact.Engine.Elements;
using Adact.Engine.Exceptions;
using Adact.Engine.Snapshot;

namespace Adact.Engine;

public sealed partial class WindowSession
{
    private static readonly TimeSpan WaitForPollInterval = TimeSpan.FromMilliseconds(100);
    private const int WaitForTraversalMaxDepth = 64;

    /// <summary>
    /// Waits for a ref to reach a specific state.
    /// </summary>
    public Task<WaitForResult> WaitForRefAsync(
        string refId,
        WaitForState state,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(refId);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be positive.");
        ct.ThrowIfCancellationRequested();

        return PollAsync(
            timeout,
            attempt: c =>
            {
                var hit = FindWaitTargetByRef(refId, c);
                if (hit is null)
                {
                    return state == WaitForState.Detached
                        ? new WaitForResult(refId, WaitForState.Detached)
                        : null;
                }

                return CheckRefState(refId, hit, state);
            },
            timeoutMessage: $"wait-for did not observe state '{WaitForStateParser.ToWireString(state)}' for ref '{refId}' within {(int)timeout.TotalMilliseconds}ms.",
            ct);
    }

    /// <summary>
    /// Waits for an element query to reach a specific state.
    /// </summary>
    public Task<WaitForResult> WaitForQueryAsync(
        WaitForElementQuery query,
        WaitForState state,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(query);
        if (!query.HasAnyCondition)
            throw new ArgumentException("Query must specify at least one condition.", nameof(query));
        if (state == WaitForState.Detached)
            throw new ArgumentException("'detached' state is not supported in query mode (no ref to track).", nameof(state));
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be positive.");
        ct.ThrowIfCancellationRequested();

        return PollAsync(
            timeout,
            attempt: c =>
            {
                return FindWaitTargetByQuery(query, state, c);
            },
            timeoutMessage: $"wait-for did not observe a matching element for state '{WaitForStateParser.ToWireString(state)}' within {(int)timeout.TotalMilliseconds}ms.",
            ct);
    }

    /// <summary>
    /// Compares a ref element against the requested wait state.
    /// </summary>
    private static WaitForResult? CheckRefState(string refId, IElement el, WaitForState state)
    {
        return state switch
        {
            WaitForState.Attached => new WaitForResult(refId, WaitForState.Attached),
            WaitForState.Visible => !el.IsOffscreen ? new WaitForResult(refId, WaitForState.Visible) : null,
            WaitForState.Hidden => el.IsOffscreen ? new WaitForResult(refId, WaitForState.Hidden) : null,
            WaitForState.Enabled => el.IsEnabled ? new WaitForResult(refId, WaitForState.Enabled) : null,
            WaitForState.Disabled => !el.IsEnabled ? new WaitForResult(refId, WaitForState.Disabled) : null,
            WaitForState.Detached => null,
            _ => null,
        };
    }

    private static bool QueryMatches(WaitForElementQuery query, IElement el)
    {
        if (!string.IsNullOrEmpty(query.Name)
            && !string.Equals(query.Name, el.Name, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(query.ControlType)
            && !string.Equals(query.ControlType, el.ControlType, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(query.AutomationId)
            && !string.Equals(query.AutomationId, el.AutomationId, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(query.ClassName)
            && !string.Equals(query.ClassName, el.ClassName, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    /// <summary>
    /// Polls until the attempt returns a result or the timeout expires.
    /// </summary>
    private async Task<T> PollAsync<T>(
        TimeSpan timeout,
        Func<CancellationToken, T?> attempt,
        string timeoutMessage,
        CancellationToken ct)
        where T : class
    {
        var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var result = await RunSerializedAsync(c =>
            {
                c.ThrowIfCancellationRequested();
                return Task.FromResult(attempt(c));
            }, ct).ConfigureAwait(false);

            if (result is not null) return result;

            if (Stopwatch.GetTimestamp() >= deadline)
                throw new WaitTimeoutException(timeoutMessage);

            try { await Task.Delay(WaitForPollInterval, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { throw; }
        }
    }

    private IElement? FindWaitTargetByRef(string refId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _rootElement?.ClearChildrenCache();
        _registry.BeginSnapshot();

        var modals = DetectModalElements();
        var popups = DetectPopupElements(modals);
        var emittedRefs = new HashSet<string>(StringComparer.Ordinal);
        var positionalIndex = 0;

        foreach (var root in EnumerateWaitTraversalRoots(modals, popups))
        {
            var found = TraverseForRef(root, refId, depth: 0, ref positionalIndex, emittedRefs, ct);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private WaitForResult? FindWaitTargetByQuery(WaitForElementQuery query, WaitForState state, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _rootElement?.ClearChildrenCache();
        _registry.BeginSnapshot();

        var modals = DetectModalElements();
        var popups = DetectPopupElements(modals);
        var emittedRefs = new HashSet<string>(StringComparer.Ordinal);
        var positionalIndex = 0;

        foreach (var root in EnumerateWaitTraversalRoots(modals, popups))
        {
            var hit = TraverseForQuery(root, query, state, depth: 0, ref positionalIndex, emittedRefs, ct);
            if (hit is not null)
            {
                return hit;
            }
        }

        return null;
    }

    private IEnumerable<IElement> EnumerateWaitTraversalRoots(
        IReadOnlyList<IElement> modals,
        IReadOnlyList<IElement> popups)
    {
        yield return _rootElement;

        foreach (var modal in modals)
        {
            yield return modal;
        }

        foreach (var popup in popups)
        {
            yield return popup;
        }
    }

    private IElement? TraverseForRef(
        IElement element,
        string targetRef,
        int depth,
        ref int positionalIndex,
        HashSet<string> emittedRefs,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var refId = _registry.Register(element, positionalIndex);
        positionalIndex++;
        if (!emittedRefs.Add(refId))
        {
            return null;
        }

        if (string.Equals(refId, targetRef, StringComparison.Ordinal))
        {
            return element;
        }

        if (depth >= WaitForTraversalMaxDepth)
        {
            return null;
        }

        foreach (var child in element.Children)
        {
            var found = TraverseForRef(child, targetRef, depth + 1, ref positionalIndex, emittedRefs, ct);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private WaitForResult? TraverseForQuery(
        IElement element,
        WaitForElementQuery query,
        WaitForState state,
        int depth,
        ref int positionalIndex,
        HashSet<string> emittedRefs,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var refId = _registry.Register(element, positionalIndex);
        positionalIndex++;
        if (!emittedRefs.Add(refId))
        {
            return null;
        }

        if (QueryMatches(query, element))
        {
            var hit = CheckRefState(refId, element, state);
            if (hit is not null)
            {
                return hit;
            }
        }

        if (depth >= WaitForTraversalMaxDepth)
        {
            return null;
        }

        foreach (var child in element.Children)
        {
            var hit = TraverseForQuery(child, query, state, depth + 1, ref positionalIndex, emittedRefs, ct);
            if (hit is not null)
            {
                return hit;
            }
        }

        return null;
    }
}
