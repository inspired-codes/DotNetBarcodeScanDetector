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

    public static int ThresholdMillisec => (int)(ThresholdTicks / TimeSpan.TicksPerMillisecond);
    public static long ThresholdTicks { get; set; } = 32 * TimeSpan.TicksPerMillisecond;

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