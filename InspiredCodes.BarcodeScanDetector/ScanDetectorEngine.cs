using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace InspiredCodes.BarcodeScanDetector;

/// <summary>
/// Tells a barcode scanner that emulates a keyboard (a fast burst of keystrokes ending with a
/// terminator) apart from human typing, and raises <see cref="BarcodeScanned"/> for each scan.
/// It only observes input; it never stops input from reaching the focused control.
/// </summary>
/// <remarks>
/// <para>
/// Thread-safe: input may arrive on several threads. Every state change happens under one lock, and
/// <see cref="BarcodeScanned"/> is raised after that lock is released, on the thread that completed
/// the scan, so a handler may block or feed input back without deadlocking.
/// </para>
/// <para>
/// Time base: <see cref="ProcessInput(string)"/> and <see cref="Simulate"/> use the engine's own
/// monotonic clock; <see cref="ProcessInput(string, TimeSpan)"/> and <see cref="ProcessBatch"/> use
/// the caller's timestamps (for example JavaScript's <c>event.timeStamp</c>). An engine uses one or
/// the other: mixing them throws <see cref="InvalidOperationException"/> until <see cref="Reset"/>.
/// </para>
/// </remarks>
public sealed class ScanDetectorEngine
{
    private static readonly long s_clockOrigin = Stopwatch.GetTimestamp();
    private static readonly double s_timeSpanTicksPerStopwatchTick = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

    private enum TimeSource { None, Own, Caller }

    // a snapshot of the options this engine was created with
    private readonly TimeSpan _threshold;
    private readonly TimeSpan _cooldown;
    private readonly int _maxLength;
    private readonly ScanTerminators _terminators;

    /// <summary>
    /// Guards every field below. Never held while raising <see cref="BarcodeScanned"/>.
    /// </summary>
    private readonly object _sync = new object();

    /// <summary>confirmed text of the scan in progress</summary>
    private readonly StringBuilder _buffer = new StringBuilder();

    /// <summary>
    /// The last input. It is held back and only added to <see cref="_buffer"/> when the next
    /// fast input confirms that it belongs to a scan.
    /// </summary>
    private string _previousText = string.Empty;

    /// <summary>when <see cref="_previousText"/> arrived; null until the first input after construction or Reset</summary>
    private TimeSpan? _previousTimestamp;

    private TimeSpan? _cooldownEnd;

    /// <summary>
    /// The other half of a CR/LF pair: set when a scan ends with a lone CR or LF, and valid only
    /// for the very next input. That input is discarded during the cooldown without extending it.
    /// </summary>
    private string? _newlineComplement;

    private TimeSource _timeSource;

    public ScanDetectorEngine(ScanDetectorOptions? options = null)
    {
        options ??= new ScanDetectorOptions();
        _threshold = options.InterKeyThreshold;
        _cooldown = options.Cooldown;
        _maxLength = options.MaxLength;
        _terminators = options.Terminators;
    }

    /// <summary>raised for each completed scan; the sender is this engine</summary>
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    /// <summary>input that arrived now, by the engine's own monotonic clock</summary>
    /// <exception cref="InvalidOperationException">when this engine is fed caller timestamps</exception>
    public void ProcessInput(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        BarcodeScannedEventArgs? scan;
        lock (_sync)
        {
            UseTimeSource(TimeSource.Own);
            scan = Step(text, Now());
        }

        if (scan != null)
            OnBarcodeScanned(scan);
    }

    /// <summary>
    /// input that arrived at <paramref name="timestamp"/>, in the caller's time base; a timestamp
    /// earlier than the previous one counts as no time passed
    /// </summary>
    /// <exception cref="InvalidOperationException">when this engine uses its own clock</exception>
    public void ProcessInput(string text, TimeSpan timestamp)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        BarcodeScannedEventArgs? scan;
        lock (_sync)
        {
            UseTimeSource(TimeSource.Caller);
            scan = Step(text, timestamp);
        }

        if (scan != null)
            OnBarcodeScanned(scan);
    }

    /// <summary>
    /// Several inputs with the caller's timestamps, processed as one step: no other input is
    /// interleaved, and the scans the batch completes are raised afterwards, in order.
    /// </summary>
    /// <exception cref="ArgumentException">when an input has no text; nothing is processed then</exception>
    /// <exception cref="InvalidOperationException">when this engine uses its own clock</exception>
    public void ProcessBatch(IList<KeyInput> inputs)
    {
        if (inputs == null)
            throw new ArgumentNullException(nameof(inputs));

        var batch = new KeyInput[inputs.Count];
        inputs.CopyTo(batch, 0);
        for (int i = 0; i < batch.Length; i++)
            if (batch[i].Text == null)
                throw new ArgumentException($"input {i} has no text", nameof(inputs));
        if (batch.Length == 0)
            return;

        List<BarcodeScannedEventArgs>? scans = null;
        lock (_sync)
        {
            UseTimeSource(TimeSource.Caller);
            foreach (KeyInput input in batch)
                Collect(ref scans, Step(input.Text, input.Timestamp));
        }

        RaiseAll(scans);
    }

    /// <summary>
    /// Feeds <paramref name="barcode"/> followed by a terminator as if a scanner had typed it, all
    /// at the current time of the engine's own clock; it does not wait. Trailing terminator
    /// characters in <paramref name="barcode"/> are ignored.
    /// </summary>
    /// <exception cref="ArgumentException">when nothing is left after the trailing terminators, or
    /// when the barcode contains a terminator character</exception>
    /// <exception cref="InvalidOperationException">when this engine is fed caller timestamps</exception>
    public void Simulate(string barcode)
    {
        if (barcode == null)
            throw new ArgumentNullException(nameof(barcode));

        char[] terminatorChars = TerminatorChars();
        string text = barcode.TrimEnd(terminatorChars);
        if (text.Length == 0)
            throw new ArgumentException("must contain characters before the terminator", nameof(barcode));
        if (text.IndexOfAny(terminatorChars) >= 0)
            throw new ArgumentException("must not contain a terminator character", nameof(barcode));
        string terminator = (_terminators & ScanTerminators.Enter) != 0 ? "\n" : "\t";

        List<BarcodeScannedEventArgs>? scans = null;
        lock (_sync)
        {
            UseTimeSource(TimeSource.Own);
            TimeSpan now = Now();
            foreach (char c in text)
                Collect(ref scans, Step(c.ToString(), now));
            Collect(ref scans, Step(terminator, now));
        }

        RaiseAll(scans);
    }

    /// <summary>
    /// Forgets the scan in progress, the cooldown and the time base.
    /// </summary>
    public void Reset()
    {
        lock (_sync)
        {
            _buffer.Clear();
            _previousText = string.Empty;
            _previousTimestamp = null;
            _cooldownEnd = null;
            _newlineComplement = null;
            _timeSource = TimeSource.None;
        }
    }

    /// <returns>the completed scan to report, if this input completed one</returns>
    private BarcodeScannedEventArgs? Step(string text, TimeSpan now)
    {
        // the first input has nothing to be fast relative to; an earlier timestamp counts as no time passed
        TimeSpan delta = _previousTimestamp is TimeSpan previous
            ? (now > previous ? now - previous : TimeSpan.Zero)
            : TimeSpan.MaxValue;

        // only the input right after a scan's newline can complete a CR/LF pair
        string? newlineComplement = _newlineComplement;
        _newlineComplement = null;

        if (_cooldownEnd is TimeSpan cooldownEnd && now < cooldownEnd)
        {
            // Discard. Fast input extends the cooldown, except for the second half of a CR/LF
            // pair, which belongs to the scan that started the cooldown.
            bool completesNewlinePair = newlineComplement != null && text == newlineComplement;
            if (delta <= _threshold && !completesNewlinePair)
                _cooldownEnd = now + _cooldown;

            _previousText = string.Empty;
            _previousTimestamp = now;
            _buffer.Clear();
            return null;
        }

        // slow
        if (delta > _threshold)
        {
            _previousText = text;
            _previousTimestamp = now;
            _buffer.Clear();
            return null;
        }

        // fast
        if (IsTerminator(text))
            return CompleteScan(text, now);

        Append(_previousText, now);
        _previousText = text;
        _previousTimestamp = now;
        return null;
    }

    private BarcodeScannedEventArgs? CompleteScan(string terminator, TimeSpan now)
    {
        if (IsTerminator(_previousText))
        {
            _previousText = terminator;
            _previousTimestamp = now;
            return null;
        }

        Append(_previousText, now);
        _previousText = terminator;
        _previousTimestamp = now;

        // Nothing to report: either the buffer overflowed on the last character (the overflow
        // already started the cooldown), or the terminator came with no text before it.
        // Neither is a scan, so no event and no scan-completed cooldown.
        if (_buffer.Length == 0)
            return null;

        string scannedText = _buffer.ToString();
        _buffer.Clear();

        // the cooldown is in place before the event is raised, so input fed from a handler
        // (or another thread) already runs into it
        _cooldownEnd = now + _cooldown;
        _newlineComplement = NewlineComplement(terminator);
        return new BarcodeScannedEventArgs(scannedText, now);
    }

    private void Append(string text, TimeSpan now)
    {
        if (text.Length == 0 || IsTerminator(text))
            return;

        // a scan longer than MaxLength is discarded and starts the cooldown
        if (_buffer.Length + text.Length > _maxLength)
        {
            _buffer.Clear();
            _cooldownEnd = now + _cooldown;
            return;
        }

        _buffer.Append(text);
    }

    private bool IsTerminator(string text)
    {
        if ((_terminators & ScanTerminators.Enter) != 0
            && (text == "\r" || text == "\n" || text == "\r\n" || text == "\n\r"))
            return true;
        return (_terminators & ScanTerminators.Tab) != 0 && text == "\t";
    }

    private char[] TerminatorChars()
    {
        var chars = new List<char>(3);
        if ((_terminators & ScanTerminators.Enter) != 0)
        {
            chars.Add('\r');
            chars.Add('\n');
        }
        if ((_terminators & ScanTerminators.Tab) != 0)
            chars.Add('\t');
        return chars.ToArray();
    }

    private static string? NewlineComplement(string terminator)
    {
        if (terminator == "\r")
            return "\n";
        if (terminator == "\n")
            return "\r";
        return null;
    }

    private void UseTimeSource(TimeSource source)
    {
        if (_timeSource == TimeSource.None)
            _timeSource = source;
        else if (_timeSource != source)
            throw new InvalidOperationException(source == TimeSource.Own
                ? "This engine is fed caller timestamps (ProcessInput(text, timestamp) or ProcessBatch); it cannot also use its own clock (ProcessInput(text) or Simulate) until Reset()."
                : "This engine uses its own clock (ProcessInput(text) or Simulate); it cannot also take caller timestamps (ProcessInput(text, timestamp) or ProcessBatch) until Reset().");
    }

    private static TimeSpan Now()
    {
        long elapsed = Stopwatch.GetTimestamp() - s_clockOrigin;
        return TimeSpan.FromTicks((long)(elapsed * s_timeSpanTicksPerStopwatchTick));
    }

    private static void Collect(ref List<BarcodeScannedEventArgs>? scans, BarcodeScannedEventArgs? scan)
    {
        if (scan != null)
            (scans ??= new List<BarcodeScannedEventArgs>()).Add(scan);
    }

    // called after the lock is released
    private void RaiseAll(List<BarcodeScannedEventArgs>? scans)
    {
        if (scans == null)
            return;
        foreach (BarcodeScannedEventArgs scan in scans)
            OnBarcodeScanned(scan);
    }

    private void OnBarcodeScanned(BarcodeScannedEventArgs args)
    {
        BarcodeScanned?.Invoke(this, args);
    }
}
