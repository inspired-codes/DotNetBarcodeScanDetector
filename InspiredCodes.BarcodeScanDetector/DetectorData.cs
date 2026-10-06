using System;
using System.Collections.Generic;

namespace InspiredCodes.WPF.BarcodeScanDetector;

/// <summary>
/// The detector's state. Only the queue operations are synchronized here;
/// <see cref="CooldownEndTicks"/> and <see cref="PreviousInput"/> are plain properties.
/// An instance is private to one detector, which serializes every access with its own
/// lock (taken before the queue lock below, never after it).
/// </summary>
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
            if (_totalLength + args.Text.Length > DetectorConfig.MaxBufferLength)
            {
                TextInputQueue.Clear();
                _totalLength = 0;
                CooldownEndTicks = DateTime.Now.Ticks + DetectorConfig.CooldownTicks;
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