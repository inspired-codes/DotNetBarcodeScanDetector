using InspiredCodes.BarcodeScanDetector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace InspiredCodes.Blazor.BarcodeScanDetector.Tests;

public class BlazorBarcodeScanServiceTests
{
    private readonly FakeJsRuntime _js = new FakeJsRuntime();
    private readonly List<BarcodeScannedEventArgs> _scans = new List<BarcodeScannedEventArgs>();
    private readonly List<object?> _senders = new List<object?>();

    private BlazorBarcodeScanService CreateService(ScanDetectorOptions? options = null)
    {
        var service = new BlazorBarcodeScanService(_js, options);
        service.BarcodeScanned += (sender, e) =>
        {
            _senders.Add(sender);
            _scans.Add(e);
        };
        return service;
    }

    /// <summary>what the JavaScript listener sends: one key per character, stepMs apart, from atMs</summary>
    private static (string[] Texts, double[] Timestamps) Keys(string keys, double atMs, double stepMs = 1)
    {
        var texts = keys.Select(c => c.ToString()).ToArray();
        var timestamps = Enumerable.Range(0, keys.Length).Select(i => atMs + i * stepMs).ToArray();
        return (texts, timestamps);
    }

    private static TimeSpan Ms(double ms) => TimeSpan.FromTicks((long)(ms * TimeSpan.TicksPerMillisecond));

    [Fact]
    public async Task StartAsync_ImportsTheModuleAndStartsOneListener()
    {
        var service = CreateService();

        await service.StartAsync();

        Assert.Equal(new[] { "./_content/InspiredCodes.Blazor.BarcodeScanDetector/barcodeScanListener.js" }, _js.Imports);
        var start = Assert.Single(_js.Module.Calls);
        Assert.Equal("start", start.Identifier);
        Assert.IsType<DotNetObjectReference<BlazorBarcodeScanService>>(Assert.Single(start.Args!));
    }

    [Fact]
    public async Task StartAsync_IsCounted_TheListenerStopsAfterTheLastStopAsync()
    {
        var service = CreateService();

        await service.StartAsync();
        await service.StartAsync();
        Assert.Equal(new[] { "start" }, _js.Module.CallNames);

        await service.StopAsync();
        Assert.Equal(new[] { "start" }, _js.Module.CallNames);

        await service.StopAsync();
        Assert.Equal(new[] { "start", "stop" }, _js.Module.CallNames);
        Assert.Equal(new object?[] { 1 }, _js.Module.Calls[1].Args);
    }

    [Fact]
    public async Task StopAsync_WithoutStartAsync_DoesNothing()
    {
        var service = CreateService();

        await service.StopAsync();
        await service.StartAsync();

        Assert.Equal(new[] { "start" }, _js.Module.CallNames);
    }

    [Fact]
    public async Task StopAsync_WhileTheListenerIsStarting_StopsItOnceStarted()
    {
        var service = CreateService();
        _js.Module.PendingStart = new TaskCompletionSource<int>();

        Task starting = service.StartAsync();
        await service.StopAsync();
        _js.Module.PendingStart.SetResult(7);
        await starting;

        Assert.Equal(new[] { "start", "stop" }, _js.Module.CallNames);
        Assert.Equal(new object?[] { 7 }, _js.Module.Calls[1].Args);
    }

    [Fact]
    public async Task StartAsync_AfterAFailedImport_CanBeRetried()
    {
        var service = CreateService();
        _js.FailNextImport = new JSException("network error");

        await Assert.ThrowsAsync<JSException>(() => service.StartAsync());
        await service.StartAsync();

        Assert.Equal(new[] { "start" }, _js.Module.CallNames);
    }

    [Fact]
    public void ProcessKeys_FeedsTheEngineWithTheBrowserTimestamps()
    {
        var service = CreateService();
        var (texts, timestamps) = Keys("ABC\r", 1000.25);

        service.ProcessKeys(texts, timestamps);

        var scan = Assert.Single(_scans);
        Assert.Equal("ABC", scan.InputText);
        Assert.Equal(Ms(1003.25), scan.Timestamp);
        Assert.Same(service.Engine, Assert.Single(_senders));
    }

    [Fact]
    public void ProcessKeys_MeasuresTheGapsBetweenKeys_NotWhenTheBatchArrives()
    {
        var service = CreateService();

        // typed 100 ms apart: human typing, even though all keys arrive in one batch
        var (texts, timestamps) = Keys("ABC\r", 0, stepMs: 100);
        service.ProcessKeys(texts, timestamps);

        Assert.Empty(_scans);
    }

    [Fact]
    public void ProcessKeys_AcrossSeveralBatches_IsOneTimeline()
    {
        var service = CreateService();

        service.ProcessKeys(new[] { "A", "B" }, new[] { 0.0, 1.0 });
        service.ProcessKeys(new[] { "C", "\r" }, new[] { 2.0, 3.0 });

        Assert.Equal("ABC", Assert.Single(_scans).InputText);
    }

    [Fact]
    public void ProcessKeys_WithMismatchedArrays_IsRejected()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.ProcessKeys(new[] { "A", "B" }, new[] { 0.0 }));
        Assert.Throws<ArgumentNullException>(() => service.ProcessKeys(null!, new double[0]));
        Assert.Throws<ArgumentNullException>(() => service.ProcessKeys(new string[0], null!));
    }

    [Fact]
    public void Engine_UsesTheBrowserTimeBase_SoItsOwnClockIsRejected()
    {
        var service = CreateService();
        service.ProcessKeys(new[] { "A" }, new[] { 0.0 });

        Assert.Throws<InvalidOperationException>(() => service.Engine.ProcessInput("B"));
    }

    [Fact]
    public async Task SimulateAsync_ScansAtTheBrowsersCurrentTime()
    {
        var service = CreateService();
        _js.Module.Now = 5000.5;

        await service.SimulateAsync("ABC");

        var scan = Assert.Single(_scans);
        Assert.Equal("ABC", scan.InputText);
        Assert.Equal(Ms(5000.5), scan.Timestamp);
        Assert.Contains("now", _js.Module.CallNames);
    }

    [Theory]
    [InlineData(100, false)]      // inside the 300 ms cooldown of the scan at 3 ms
    [InlineData(303, true)]
    public async Task SimulateAsync_SharesTheTimelineWithTypedKeys(double now, bool accepted)
    {
        var service = CreateService();
        var (texts, timestamps) = Keys("ABC\r", 0);
        service.ProcessKeys(texts, timestamps);
        _js.Module.Now = now;

        await service.SimulateAsync("XYZ");

        Assert.Equal(accepted ? new[] { "ABC", "XYZ" } : new[] { "ABC" }, _scans.Select(s => s.InputText));
    }

    [Fact]
    public async Task DisposeAsync_StopsTheListenerAndReleasesTheModule()
    {
        var service = CreateService();
        await service.StartAsync();

        await service.DisposeAsync();

        Assert.Equal(new[] { "start", "stop" }, _js.Module.CallNames);
        Assert.True(_js.Module.Disposed);
    }

    [Fact]
    public async Task AddBarcodeScanDetector_RegistersAScopedService_WithTheGivenOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(_js);
        services.AddBarcodeScanDetector(options => options.Terminators = ScanTerminators.Tab);
        // the service only disposes asynchronously (it talks to JavaScript), as Blazor's scopes do
        await using var provider = services.BuildServiceProvider();

        BlazorBarcodeScanService first, again, other;
        await using (var scope = provider.CreateAsyncScope())
        {
            first = scope.ServiceProvider.GetRequiredService<BlazorBarcodeScanService>();
            again = scope.ServiceProvider.GetRequiredService<BlazorBarcodeScanService>();
        }
        await using (var scope = provider.CreateAsyncScope())
            other = scope.ServiceProvider.GetRequiredService<BlazorBarcodeScanService>();

        Assert.Same(first, again);
        Assert.NotSame(first, other);

        // the options reached the engine: Tab ends a scan, Enter is ordinary text
        var scans = new List<string>();
        first.BarcodeScanned += (sender, e) => scans.Add(e.InputText);
        var (texts, timestamps) = Keys("AB\r\t", 0);
        first.ProcessKeys(texts, timestamps);
        Assert.Equal(new[] { "AB\r" }, scans);
    }
}
