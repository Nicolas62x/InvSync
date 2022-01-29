
using System;
using System.Collections.Generic;

namespace InvSync.Packets;
interface SyncPacket
{
    static Dictionary<PacketID, SyncPacket> Packets = new Dictionary<PacketID, SyncPacket>();
    
    static SyncPacket()
    {
        Packets.Add(PacketID.InvRequest, new InvRequest());
        Packets.Add(PacketID.InvUpdate, new InvUpdate());
        Packets.Add(PacketID.InvDelete, new InvDelete());
    }

    public static byte[] HandlePacket(PacketID id ,byte[] data, int len)
    {
        if (Packets.TryGetValue(id, out SyncPacket handler))
            return handler.HandlePacket(data, len);
        throw new Exception($"Packet {id} can't be handled");
    }

    public byte[] HandlePacket(byte[] data, int len);
}