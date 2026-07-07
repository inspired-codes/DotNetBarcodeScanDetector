using System;
using System.Collections.Generic;
using System.Linq;

namespace InspiredCodes.BarcodeScanDetector.Tests.Mock;

public class SomeBarcodes : List<string>
{
    public SomeBarcodes() : base()
    {
        AddRange(new string[]
        {
            "000000000000702675",
            "000000000000703111",
            "000000000000703115",
            "1_C04441_R042",
            "1_C04441_R043",
            "1_C04441_R066",
            "1_C04441_R90",
            "1_C04441_RF0",
            "1_SP5358020_120X20",
            "100004_43",
            "1023918001PR",
            "1025167",
            "1027426",
            "1027849",
            "WPVTZSCH0",
            "WPVTZSDZ0",
            "WPVTZTSS0",
            "XCPT78048450",
            "Y141827",
            "Z-PROF. LI STIF. PLV",
            "Z-PROF. RE STIF. PLV",
            "ZENTRALABDECKUNG PLV",
            "ZUGENTLAST-KK-VIO",
            "ZUS-SCHAUMROHR 40/20",
            "ZUS-SCHAUMROHR 54/13",
            "ZUSCHNITT FLEXIPACK",
            "ZW.STEG 1110653 PLV",
            "ZWISCHENSTEG PLV",
            "A000130963212000010004",
            "00013096321300001",
            "00013096321300002",
            "A000130963214000010001",
        });
    }
}