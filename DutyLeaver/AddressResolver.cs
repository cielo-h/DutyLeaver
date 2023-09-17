using System;
using Dalamud.Game;

namespace DutyLeaverPlugin;

internal class AddressResolver : BaseAddressResolver
{
    public IntPtr LeaveDuty { get; private set; }
    public IntPtr DutyFinder { get; private set; }

    protected override void Setup64Bit(SigScanner scanner)
    {
        LeaveDuty = scanner.ScanText("40 53 48 83 ec 20 48 8b 05 ?? ?? ?? ?? 0f b6 d9");
    }
}
