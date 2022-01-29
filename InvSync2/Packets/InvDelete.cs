
using System;
using System.Collections.Generic;
using System.Text;

namespace InvSync.Packets;
class InvDelete : SyncPacket
{
    static readonly byte[] OK = { 1, 0, 0, 0, 2 };
    public byte[] HandlePacket(byte[] data, int len)
    {
        if (len < 2)
            throw new Exception("Request was not long enough");

        byte NameLen = data[0];
        string Name = Encoding.UTF8.GetString(data, 1, NameLen);

        Logger.Log($"Delete request of {Name}");

        FileManager.DeleteFile(Name);

        return OK;
    }
}