using System;

namespace InspiredCodes.WPF.BarcodeScanDetector;

public class TextInputEventArgs : EventArgs
{
    public TextInputEventArgs(string text, long deltaToPreviousTicks)
    {
        Text = text;
        DeltaToPreviousTicks = deltaToPreviousTicks;
    }
    public string Text { get; }
    public long TimestampTicks { get; } = DateTime.Now.Ticks;
    public long DeltaToPreviousTicks { get; }
}

public class ReturnInputArgs : TextInputEventArgs
{
    public ReturnInputArgs(string text, long deltaToPreviousTicks) : base(text,deltaToPreviousTicks)
    {
        if (!DetectorConfig.IsLineFeedOrCarriageReturn(text))
            throw new ArgumentException("value must be Feed- or CarriageReturn", nameof(text));
    }
}
