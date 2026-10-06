using System;

namespace InspiredCodes.BarcodeScanDetector;

/// <summary>
/// Settings for a <see cref="ScanDetectorEngine"/>. The engine copies them when it is created,
/// so changing an options object afterwards does not affect engines created from it.
/// </summary>
public sealed class ScanDetectorOptions
{
    private TimeSpan _interKeyThreshold = TimeSpan.FromMilliseconds(32);
    private TimeSpan _cooldown = TimeSpan.FromMilliseconds(300);
    private int _maxLength = 4096;
    private ScanTerminators _terminators = ScanTerminators.Enter;

    /// <summary>
    /// the largest gap between two inputs that still counts as fast (default 32 ms)
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">when set to a negative value</exception>
    public TimeSpan InterKeyThreshold
    {
        get => _interKeyThreshold;
        set => _interKeyThreshold = NotNegative(value);
    }

    /// <summary>
    /// how long input is discarded after a scan or a buffer overflow (default 300 ms); fast input
    /// during the cooldown extends it. Zero disables the cooldown.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">when set to a negative value</exception>
    public TimeSpan Cooldown
    {
        get => _cooldown;
        set => _cooldown = NotNegative(value);
    }

    /// <summary>
    /// the most characters a scan may have (default 4096); a longer scan is discarded and starts the cooldown
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">when set to less than 1</exception>
    public int MaxLength
    {
        get => _maxLength;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, "must be at least 1");
            _maxLength = value;
        }
    }

    /// <summary>
    /// what ends a scan (default <see cref="ScanTerminators.Enter"/>); a terminator that is not
    /// selected here is an ordinary character
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">when set to <see cref="ScanTerminators.None"/> or an undefined value</exception>
    public ScanTerminators Terminators
    {
        get => _terminators;
        set
        {
            if (value == ScanTerminators.None || (value & ~(ScanTerminators.Enter | ScanTerminators.Tab)) != 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "must select Enter, Tab or both");
            _terminators = value;
        }
    }

    private static TimeSpan NotNegative(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(value), value, "must not be negative");
        return value;
    }
}
