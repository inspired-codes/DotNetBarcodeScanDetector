using System;

namespace InspiredCodes.BarcodeScanDetector;

/// <summary>
/// What ends a scan. Scanners append a configurable suffix; Enter is the most common.
/// </summary>
[Flags]
public enum ScanTerminators
{
    None = 0,

    /// <summary>the Enter key: <c>\r</c>, <c>\n</c>, <c>\r\n</c> or <c>\n\r</c></summary>
    Enter = 1,

    /// <summary>the Tab key: <c>\t</c></summary>
    Tab = 2,
}
