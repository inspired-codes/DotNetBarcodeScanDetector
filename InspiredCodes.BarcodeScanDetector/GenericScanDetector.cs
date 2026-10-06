using System;
using System.Text;
using System.Threading.Tasks;

namespace InspiredCodes.WPF.BarcodeScanDetector;

using static DetectorConfig;

internal sealed class GenericScanDetector
{
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    private void OnBarcodeScannedEvent(object? sender, BarcodeScannedEventArgs args)
    {
        BarcodeScanned?.Invoke(sender, args);
    }

    /// <summary>
    /// Guards <see cref="Data"/> and <see cref="_newlineComplement"/>: input may arrive from
    /// several threads, and every state transition must see and leave consistent state.
    /// Never held while raising <see cref="BarcodeScanned"/>, so a handler may block or call
    /// back into the detector without deadlocking. Lock order: this lock, then the queue
    /// lock inside <see cref="DetectorData"/>; the latter never takes this one.
    /// </summary>
    private readonly object _sync = new object();

    private readonly DetectorData Data = new DetectorData();

    /// <summary>
    /// The other half of a CRLF/LFCR pair: set when a scan ends with a lone CR or LF,
    /// and valid only for the very next input. That input is discarded during the
    /// cooldown without extending it. Guarded by <see cref="_sync"/>.
    /// </summary>
    private string? _newlineComplement;

    /// <summary>
    /// for many UI components, the calling thead must be STA
    /// </summary>
    /// <param name="sender">reported as the sender of the BarcodeScanned event this simulation raises</param>
    /// <param name="textInput"></param>
    /// <param name="thresholdDivider"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void SimulateFastInput(object? sender, string textInput, int thresholdDivider = 3)
    {
        Task.Delay(ThresholdMillisec / thresholdDivider).Wait();
        if (string.IsNullOrEmpty(textInput))
            throw new ArgumentNullException(nameof(textInput));

        textInput = textInput.TrimEnd(new char[] { NewLineCharN, NewLineCharR });

        CheckNoCrOrLf(textInput);

        for (int i = 0; i < textInput.Length; i++)
            ProcessInput(textInput[i].ToString(), sender);

        Task.Delay(ThresholdMillisec / thresholdDivider).Wait();
        ProcessInput(NewLineN, sender);
    }
    /// <summary>real input: the raised event has no sender</summary>
    public void ProcessInput(string text)
    {
        ProcessInput(text, null);
    }
    /// <param name="sender">reported as the sender of the BarcodeScanned event, if this input completes a scan</param>
    private void ProcessInput(string text, object? sender)
    {
        BarcodeScannedEventArgs? scanned;
        lock (_sync)
        {
            scanned = ProcessInputLocked(text);
        }

        // raised after the lock is released; the cooldown it started is already in place
        if (scanned != null)
            OnBarcodeScannedEvent(sender, scanned);
    }
    /// <returns>the completed scan to report, if this input completed one</returns>
    private BarcodeScannedEventArgs? ProcessInputLocked(string text)
    {
        // read the clock inside the lock so timestamps follow the order inputs are processed in
        long nowTicks = DateTime.Now.Ticks;
        long delta = nowTicks - Data.PreviousInput.TimestampTicks;
        TextInputEventArgs textInputArgs = new TextInputEventArgs(text, nowTicks, delta);

        // only the input right after a scan's newline can complete a CRLF/LFCR pair
        string? newlineComplement = _newlineComplement;
        _newlineComplement = null;

        if (nowTicks < Data.CooldownEndTicks)
        {
            // If it's a fast input, extend the cooldown, except for the second half
            // of a CRLF/LFCR pair, which belongs to the scan that started the cooldown
            bool completesNewlinePair = newlineComplement != null && text == newlineComplement;
            if (delta <= ThresholdTicks && !completesNewlinePair)
            {
                Data.CooldownEndTicks = nowTicks + CooldownTicks;
            }

            // Discard input and keep the real timestamp so we can accurately measure the next delta
            Data.PreviousInput = new TextInputEventArgs(string.Empty, nowTicks, delta);
            Data.ClearQueue();
            return null;
        }

        // slow
        if (ThresholdTicks < delta)
        {
            Data.PreviousInput = textInputArgs;
            Data.ClearQueue();
            return null;
        }

        // fast
        return HandleFastInput(textInputArgs);
    }
    private BarcodeScannedEventArgs? HandleFastInput(TextInputEventArgs textInputArgs)
    {
        if (IsLineFeedOrCarriageReturn(textInputArgs.Text))
            return HandleReturnInput(new ReturnInputArgs(textInputArgs.Text, textInputArgs.TimestampTicks, textInputArgs.DeltaToPreviousTicks));

        Data.Enqueue(Data.PreviousInput);
        Data.PreviousInput = textInputArgs;
        return null;
    }
    private BarcodeScannedEventArgs? HandleReturnInput(ReturnInputArgs textInputArgs)
    {
        if (IsLineFeedOrCarriageReturn(Data.PreviousInput.Text))
        {
            Data.PreviousInput = textInputArgs;
            return null;
        }

        Data.Enqueue(Data.PreviousInput);
        Data.PreviousInput = textInputArgs;
        var arr = Data.GetAllTextAndClearQueue();

        var sb = new StringBuilder();
        for (int i = 0; i < arr.Length; i++)
            sb.Append(arr[i].Text);

        // Nothing to report: either the buffer overflowed on the last character (the
        // overflow already started the cooldown), or the newline came with no text
        // before it. Neither is a scan, so no event and no scan-completed cooldown.
        if (sb.Length == 0)
            return null;

        // start the cooldown before the event is raised, so input fed from a handler
        // (or another thread) already runs into it
        Data.CooldownEndTicks = DateTime.Now.Ticks + CooldownTicks;
        _newlineComplement = NewlineComplement(textInputArgs.Text);
        return new BarcodeScannedEventArgs(sb.ToString());
    }
    private static string? NewlineComplement(string newline)
    {
        if (newline == NewLineR)
            return NewLineN;
        if (newline == NewLineN)
            return NewLineR;
        return null;
    }
    //private void TextInputHandler(object _, string textInput)
    //{
    //    long delta = DateTime.Now.Ticks - Data.PreviousInput.TimestampTicks;
    //    TextInputEventArgs textInputArgs = new TextInputEventArgs(textInput, delta);

    //    // slow
    //    if (ThresholdTicks < delta)
    //    {
    //        Data.PreviousInput = textInputArgs;
    //        Data.ClearQueue();
    //        return;
    //    }

    //    // fast
    //    HandleFastInput(textInputArgs);

    //}
    public void Reset()
    {
        lock (_sync)
        {
            Data.ClearQueue();
            Data.CooldownEndTicks = 0;
            _newlineComplement = null;
            Data.PreviousInput = new TextInputEventArgs(string.Empty, DateTime.Now.Ticks, 0);
        }
    }
}
