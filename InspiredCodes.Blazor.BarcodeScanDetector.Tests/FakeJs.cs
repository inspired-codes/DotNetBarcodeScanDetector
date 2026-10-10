using Microsoft.JSInterop;

namespace InspiredCodes.Blazor.BarcodeScanDetector.Tests;

/// <summary>
/// Stands in for the browser: <c>import</c> returns a <see cref="FakeJsModule"/>, which records
/// the calls the service makes to barcodeScanListener.js.
/// </summary>
internal sealed class FakeJsRuntime : IJSRuntime
{
    public FakeJsModule Module { get; } = new FakeJsModule();
    public List<string> Imports { get; } = new List<string>();

    /// <summary>the next import fails once with this exception</summary>
    public Exception? FailNextImport { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier != "import")
            throw new InvalidOperationException($"unexpected call to {identifier}");
        if (FailNextImport is Exception failure)
        {
            FailNextImport = null;
            return ValueTask.FromException<TValue>(failure);
        }
        Imports.Add((string)args![0]!);
        return new ValueTask<TValue>((TValue)(object)Module);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);
}

internal sealed class FakeJsModule : IJSObjectReference
{
    private int _nextListenerId = 1;

    public List<(string Identifier, object?[]? Args)> Calls { get; } = new List<(string, object?[]?)>();

    /// <summary>what now() returns, in milliseconds</summary>
    public double Now { get; set; }

    /// <summary>when set, start() only completes when this does</summary>
    public TaskCompletionSource<int>? PendingStart { get; set; }

    public bool Disposed { get; private set; }

    public IEnumerable<string> CallNames => Calls.Select(c => c.Identifier);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        Calls.Add((identifier, args));
        switch (identifier)
        {
            case "start":
                if (PendingStart != null)
                    return new ValueTask<TValue>((Task<TValue>)(object)PendingStart.Task);
                return new ValueTask<TValue>((TValue)(object)_nextListenerId++);
            case "now":
                return new ValueTask<TValue>((TValue)(object)Now);
            case "stop":
                return new ValueTask<TValue>(default(TValue)!);
            default:
                throw new InvalidOperationException($"unexpected call to {identifier}");
        }
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
