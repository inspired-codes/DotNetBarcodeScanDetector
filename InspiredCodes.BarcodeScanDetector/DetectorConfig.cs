using System;

namespace InspiredCodes.WPF.BarcodeScanDetector;

public class DetectorConfig
{

    public static readonly char NewLineCharN = '\n';
    public static readonly char NewLineCharR = '\r';
    public static readonly string NewLineN = "\n";
    public static readonly string NewLineR = "\r";
    public static readonly string NewLineRN = "\r\n";
    public static readonly string NewLineNR = "\n\r";

    private static int _cooldownMillisec = 300;
    private static int _maxBufferLength = 4096;

    /// <summary>
    /// the largest gap between two inputs that still counts as fast, in milliseconds (default 32)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">when set to a negative value</exception>
    public static int ThresholdMillisec
    {
        get => (int)(ThresholdTicks / TimeSpan.TicksPerMillisecond);
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "must not be negative");
            ThresholdTicks = value * TimeSpan.TicksPerMillisecond;
        }
    }
    public static long ThresholdTicks { get; set; } = 32 * TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// how long input is discarded after a scan or a buffer overflow, in milliseconds (default 300);
    /// 0 disables the cooldown
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">when set to a negative value</exception>
    public static int CooldownMillisec
    {
        get => _cooldownMillisec;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "must not be negative");
            _cooldownMillisec = value;
        }
    }
    internal static long CooldownTicks => _cooldownMillisec * TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// the most characters a single scan may have (default 4096); a longer scan is discarded
    /// and starts the cooldown
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">when set to less than 1</exception>
    public static int MaxBufferLength
    {
        get => _maxBufferLength;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, "must be at least 1");
            _maxBufferLength = value;
        }
    }

    /// <summary>
    /// throws ArgumentException when text contains CarriageReturn or LineFeed
    /// </summary>
    /// <param name="textInput"></param>
    public static void CheckNoCrOrLf(string textInput)
    {
        for (int i = 0; i < textInput.Length; i++)
            if (textInput[i] == NewLineCharR || textInput[i] == NewLineCharN)
                throw new ArgumentException($"{nameof(textInput)} must not contain line end or new line character");
    }
    public static bool IsLineFeedOrCarriageReturn(string text)
    {
        return (text == NewLineR || text == NewLineN || text == NewLineRN || text == NewLineNR);
    }

}