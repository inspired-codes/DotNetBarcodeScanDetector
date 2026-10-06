using InspiredCodes.BarcodeScanDetector;
using Microsoft.JSInterop;

namespace InspiredCodes.Blazor.BarcodeScanDetector;

/// <summary>
/// Detects barcode scans in a Blazor WebAssembly app. A JavaScript listener records every keystroke
/// on the page with the browser's high-resolution <c>event.timeStamp</c> and hands the keys to
/// <see cref="Engine"/> in batches, so the measured gaps between keys are exact however late a batch
/// arrives. Register it with <see cref="BarcodeScanServiceCollectionExtensions.AddBarcodeScanDetector"/>
/// and use the <c>BarcodeScanListener</c> component, or call <see cref="StartAsync"/> / <see cref="StopAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// The engine is fed the browser's timestamps, so its own-clock methods (<c>ProcessInput(text)</c>,
/// <c>Simulate(barcode)</c>) throw; use <see cref="SimulateAsync"/>. It only observes: keys still
/// reach the focused element.
/// </para>
/// <para>
/// Detection is only as precise as the browser's timestamps: privacy settings that coarsen timers
/// (e.g. Firefox's <c>privacy.resistFingerprinting</c>, 100 ms) make fast keys indistinguishable from typing.
/// </para>
/// </remarks>
public sealed class BlazorBarcodeScanService : IAsyncDisposable
{
    internal const string ModulePath = "./_content/InspiredCodes.Blazor.BarcodeScanDetector/barcodeScanListener.js";

    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<BlazorBarcodeScanService>? _self;
    private int? _listenerId;
    private int _starts;

    public BlazorBarcodeScanService(IJSRuntime js, ScanDetectorOptions? options = null)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        Engine = new ScanDetectorEngine(options);
    }

    /// <summary>the engine the page's keys are fed to</summary>
    public ScanDetectorEngine Engine { get; }

    /// <summary>raised for each scan; the sender is <see cref="Engine"/></summary>
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned
    {
        add { Engine.BarcodeScanned += value; }
        remove { Engine.BarcodeScanned -= value; }
    }

    /// <summary>
    /// Starts listening to the page's keystrokes. Calls are counted: the listener runs until every
    /// <see cref="StartAsync"/> has been matched by a <see cref="StopAsync"/>.
    /// </summary>
    public async Task StartAsync()
    {
        if (_starts++ > 0)
            return;

        int id;
        try
        {
            IJSObjectReference module = await GetModuleAsync();
            _self ??= DotNetObjectReference.Create(this);
            id = await module.InvokeAsync<int>("start", _self);
        }
        catch
        {
            _starts--;
            throw;
        }

        if (_starts == 0)
            await _module!.InvokeVoidAsync("stop", id);   // stopped again while the listener was starting
        else
            _listenerId = id;
    }

    public async Task StopAsync()
    {
        if (_starts == 0 || --_starts > 0)
            return;

        if (_listenerId is int id)
        {
            _listenerId = null;
            await _module!.InvokeVoidAsync("stop", id);
        }
    }

    /// <summary>
    /// Simulates a scan of <paramref name="barcode"/> at the browser's current time (for example from
    /// a demo button). It is subject to the cooldown like a real scan.
    /// </summary>
    /// <exception cref="ArgumentException">when the barcode is empty or contains a terminator character</exception>
    public async Task SimulateAsync(string barcode)
    {
        if (barcode == null)
            throw new ArgumentNullException(nameof(barcode));

        IJSObjectReference module = await GetModuleAsync();
        double now = await module.InvokeAsync<double>("now");
        Engine.Simulate(barcode, FromMilliseconds(now));
    }

    /// <summary>
    /// Called by the JavaScript listener: each text with the <c>event.timeStamp</c> (milliseconds) of its key.
    /// </summary>
    [JSInvokable]
    public void ProcessKeys(string[] texts, double[] timestamps)
    {
        if (texts == null)
            throw new ArgumentNullException(nameof(texts));
        if (timestamps == null)
            throw new ArgumentNullException(nameof(timestamps));
        if (texts.Length != timestamps.Length)
            throw new ArgumentException("needs one timestamp per text", nameof(timestamps));

        var inputs = new KeyInput[texts.Length];
        for (int i = 0; i < texts.Length; i++)
            inputs[i] = new KeyInput(texts[i], FromMilliseconds(timestamps[i]));
        Engine.ProcessBatch(inputs);
    }

    public async ValueTask DisposeAsync()
    {
        _starts = 0;
        try
        {
            if (_listenerId is int id && _module != null)
            {
                _listenerId = null;
                await _module.InvokeVoidAsync("stop", id);
            }
            if (_module != null)
                await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // the page is gone, and the listener with it
        }
        _module = null;
        _self?.Dispose();
        _self = null;
    }

    private async Task<IJSObjectReference> GetModuleAsync()
    {
        return _module ??= await _js.InvokeAsync<IJSObjectReference>("import", ModulePath);
    }

    // exact on every target framework
    private static TimeSpan FromMilliseconds(double ms) => TimeSpan.FromTicks((long)(ms * TimeSpan.TicksPerMillisecond));
}
