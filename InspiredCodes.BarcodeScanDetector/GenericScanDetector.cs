using System;
using System.Text;
using System.Threading.Tasks;

namespace InspiredCodes.WPF.BarcodeScanDetector;

using static DetectorConfig;

internal sealed class GenericScanDetector
{
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

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

        Task.Delay(ThresholdMillisec / thresholdDivider).Wait();
        ProcessInput(NewLineN);
    }
    public void ProcessInput(string text)
    {
        long nowTicks = DateTime.Now.Ticks;
        long delta = nowTicks - Data.PreviousInput.TimestampTicks;
        TextInputEventArgs textInputArgs = new TextInputEventArgs(text, delta);

        if (nowTicks < Data.CooldownEndTicks)
        {
            // If it's a fast input, extend the cooldown
            if (delta <= ThresholdTicks)
            {
                Data.CooldownEndTicks = nowTicks + (300 * TimeSpan.TicksPerMillisecond);
            }

            // Discard input and keep the real timestamp so we can accurately measure the next delta
            Data.PreviousInput = new TextInputEventArgs(string.Empty, nowTicks);
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
    private void HandleFastInput(TextInputEventArgs textInputArgs)
    {
        if (IsLineFeedOrCarriageReturn(textInputArgs.Text))
        {
            HandleReturnInput(new ReturnInputArgs(textInputArgs.Text, textInputArgs.TimestampTicks));
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
        Data.ClearQueue();
        Data.CooldownEndTicks = 0;
        Data.PreviousInput = new TextInputEventArgs(string.Empty, DateTime.Now.Ticks);
    }
}
