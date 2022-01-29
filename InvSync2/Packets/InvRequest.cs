
using System;
using System.Collections.Generic;
using System.Text;

namespace InvSync.Packets;
class InvRequest : SyncPacket
{
    static readonly byte[] NotFound = { 1, 0, 0, 0, 254 };
    public byte[] HandlePacket(byte[] data, int len)
    {
        if (len < 2)
            throw new Exception("Request was not long enough");

        byte NameLen = data[0];
        string Name = Encoding.UTF8.GetString(data, 1, NameLen);

        Logger.Log($"Inv request of {Name}");

        byte[] Inventory = FileManager.GetFile(Name);
        if (Inventory is null)
            return NotFound;

        List<byte> resp = new List<byte>();
        resp.AddRange(BitConverter.GetBytes(Inventory.Length + 1));
        resp.Add(0);
        resp.AddRange(Inventory);
        return resp.ToArray();
    }
}