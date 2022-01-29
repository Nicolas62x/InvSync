
using System;
using System.Text;

namespace InvSync.Packets;
class InvUpdate : SyncPacket
{
    static readonly byte[] OK = { 1, 0, 0, 0, 1 };
    public byte[] HandlePacket(byte[] data, int len)
    {
        if (len < 3)
            throw new Exception("Request was not long enough");

        byte NameLen = data[0];

        string Name = Encoding.UTF8.GetString(data, 1, NameLen);

        ArraySegment<byte> Inventory = new ArraySegment<byte>(data, NameLen + 1, len - (NameLen + 1));

        Logger.Log($"Update request of {Name} ({Inventory.Count} bytes)");

        FileManager.SetFile(Name, Inventory.ToArray());

        return OK;
    }
}