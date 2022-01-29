using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InvSync.Packets;

[StructLayout(LayoutKind.Sequential, Size = 5)]
struct PacketHeader
{
    public int Len;
    public PacketID ID;
}