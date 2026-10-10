using System;
using System.Collections.Generic;

namespace InspiredCodes.WPF.BarcodeScanDetector;

public class DetectorData 
{

    private readonly object LockQueue = new object();
    private readonly Queue<TextInputEventArgs> TextInputQueue = new Queue<TextInputEventArgs>();

    private int _totalLength;

    public long CooldownEndTicks { get; set; }

    public TextInputEventArgs PreviousInput { get; set; } = new TextInputEventArgs(string.Empty, DateTime.Now.Ticks, 0);

    public void ClearQueue()
    {
        lock (LockQueue)
        {
            TextInputQueue.Clear();
            _totalLength = 0;
        }
    }
    public void Enqueue(TextInputEventArgs args)
    {
        if (DetectorConfig.IsLineFeedOrCarriageReturn(args.Text) || string.IsNullOrEmpty(args.Text))
            return;
        lock (LockQueue)
        {
            if (_totalLength + args.Text.Length > 4096)
            {
                TextInputQueue.Clear();
                _totalLength = 0;
                CooldownEndTicks = DateTime.Now.Ticks + (300 * TimeSpan.TicksPerMillisecond);
                return;
            }
            TextInputQueue.Enqueue(args);
            _totalLength += args.Text.Length;
        }
    }
    public TextInputEventArgs[] GetAllTextAndClearQueue()
    {
        lock (LockQueue)
        {
            TextInputEventArgs[] textInputAll = TextInputQueue.ToArray();
            TextInputQueue.Clear();
            _totalLength = 0;
            return textInputAll;
        }
    }
}