using System;
using System.Text;
using System.Threading.Tasks;

namespace InspiredCodes.WPF.BarcodeScanDetector;

using static DetectorConfig;

internal sealed class GenericScanDetector
{
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    private readonly object _syncRoot = new object();

    private void OnBarcodeScannedEvent(BarcodeScannedEventArgs args)
    {
        BarcodeScanned?.Invoke(null, args);
    }

    private DetectorData Data = new DetectorData();

    /// <summary>
    /// for many UI components, the calling thead must be STA
    /// </summary>
    /// <param name="sender"></param>
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
            ProcessInput(textInput[i].ToString());

        // Removed the Task.Delay here to prevent Windows timer jitter from artificially pushing 
        // the newline event past the 32ms ThresholdTicks, which was discarding the entire scan.
        ProcessInput(NewLineN);
    }

    public void ProcessInput(string text)
    {
        lock (_syncRoot)
        {
            long nowTicks = DateTime.Now.Ticks;
            long delta = nowTicks - Data.PreviousInput.TimestampTicks;
            TextInputEventArgs textInputArgs = new TextInputEventArgs(text, nowTicks, delta);

            if (nowTicks < Data.CooldownEndTicks)
            {
                // Special case: if we just processed '\r' and started a cooldown,
                // ignore the subsequent '\n' from a "\r\n" CRLF pair without extending the cooldown.
                if (text == "\n" && Data.PreviousInput.Text == "\r")
                {
                    Data.PreviousInput = new TextInputEventArgs(string.Empty, nowTicks, delta);
                    return;
                }

                // If it's a fast input during an active cooldown, extend the cooldown
                if (delta <= ThresholdTicks)
                {
                    Data.CooldownEndTicks = nowTicks + (300 * TimeSpan.TicksPerMillisecond);
                }

                // Discard input and keep the real timestamp so we can accurately measure the next delta
                Data.PreviousInput = new TextInputEventArgs(string.Empty, nowTicks, delta);
                Data.ClearQueue();
                return;
            }

            // slow
            if (ThresholdTicks < delta)
            {
                Data.PreviousInput = textInputArgs;
                Data.ClearQueue();
                return;
            }

            // fast
            HandleFastInput(textInputArgs);
        }
    }

    private void HandleFastInput(TextInputEventArgs textInputArgs)
    {
        if (IsLineFeedOrCarriageReturn(textInputArgs.Text))
        {
            HandleReturnInput(new ReturnInputArgs(textInputArgs.Text, textInputArgs.TimestampTicks, textInputArgs.DeltaToPreviousTicks));
            return;
        }

        Data.Enqueue(Data.PreviousInput);
        Data.PreviousInput = textInputArgs;
    }

    private void HandleReturnInput(ReturnInputArgs textInputArgs)
    {
        if (IsLineFeedOrCarriageReturn(Data.PreviousInput.Text))
        {
            Data.PreviousInput = textInputArgs;
            return;
        }

        Data.Enqueue(Data.PreviousInput);
        Data.PreviousInput = textInputArgs;
        var arr = Data.GetAllTextAndClearQueue();

        var sb = new StringBuilder();
        for (int i = 0; i < arr.Length; i++)
            sb.Append(arr[i].Text);

        OnBarcodeScannedEvent(new BarcodeScannedEventArgs(sb.ToString()));
        Data.CooldownEndTicks = DateTime.Now.Ticks + (300 * TimeSpan.TicksPerMillisecond);
    }

    public void Reset()
    {
        lock (_syncRoot)
        {
            Data.ClearQueue();
            Data.CooldownEndTicks = 0;
            Data.PreviousInput = new TextInputEventArgs(string.Empty, DateTime.Now.Ticks, 0);
        }
    }
}
