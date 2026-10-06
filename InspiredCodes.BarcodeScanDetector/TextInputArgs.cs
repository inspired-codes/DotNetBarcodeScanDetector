using System;

namespace InspiredCodes.WPF.BarcodeScanDetector;

public class TextInputEventArgs : EventArgs
{
    /// <summary>
    /// stamps the current time; <paramref name="deltaToPreviousTicks"/> is the time since the
    /// previous input, NOT a timestamp
    /// </summary>
    public TextInputEventArgs(string text, long deltaToPreviousTicks)
        : this(text, DateTime.Now.Ticks, deltaToPreviousTicks)
    {
    }
    public TextInputEventArgs(string text, long timestampTicks, long deltaToPreviousTicks)
    {
        Text = text;
        TimestampTicks = timestampTicks;
        DeltaToPreviousTicks = deltaToPreviousTicks;
    }
    public string Text { get; }
    public long TimestampTicks { get; }
    public long DeltaToPreviousTicks { get; }
}

public class ReturnInputArgs : TextInputEventArgs
{
    public ReturnInputArgs(string text, long deltaToPreviousTicks)
        : this(text, DateTime.Now.Ticks, deltaToPreviousTicks)
    {
    }
    public ReturnInputArgs(string text, long timestampTicks, long deltaToPreviousTicks)
        : base(text, timestampTicks, deltaToPreviousTicks)
    {
        if (!DetectorConfig.IsLineFeedOrCarriageReturn(text))
            throw new ArgumentException("value must be Feed- or CarriageReturn", nameof(text));
    }
}
