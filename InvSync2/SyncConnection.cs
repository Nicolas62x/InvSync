using InvSync.Packets;
using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;

namespace InvSync;

class SyncConnection
{
    const int MaxPacketLen = 1_000_000_000;

    static readonly byte[] InvalidRequest = { 1, 0, 0, 0, 255 };

    AsyncSocket s;
    PacketHeader CurrentPacket;    

    public SyncConnection(Socket s)
    {
        this.s = new AsyncSocket(s, Update);

        CurrentPacket.Len = 0;

        this.s.ReadData(5);
    }

    public static void Listen(Socket s)
    {
        if (!s.Connected)
        {
            s.Dispose();
            return;
        }

        new SyncConnection(s);
    }

    //called when a packet is red
    void Update(byte[] buffer, int len)
    {
        try
        {
            //need to parse header
            if (CurrentPacket.Len == 0)
            {
                CurrentPacket = Utils.ByteArrayToStructure<PacketHeader>(buffer);

                InvSync.AddRequest();

                //handle rejection if packet is too long
                if (CurrentPacket.Len > MaxPacketLen)
                    throw new Exception($"{CurrentPacket.Len} exeeds max packet lenght of {MaxPacketLen} bytes");

                s.ReadData(CurrentPacket.Len - 1);
            }
            //handle packet
            else
            {
                PacketID id = CurrentPacket.ID;

                CurrentPacket.Len = 0;
                s.ReadData(5);

                s.SendData(SyncPacket.HandlePacket(id, buffer, len));
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.ToString());

            s.Kill(InvalidRequest);
        }
    }
}
