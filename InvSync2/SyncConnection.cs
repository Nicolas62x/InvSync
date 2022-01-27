using InvSync.Packets;
using System;
using System.Buffers;
using System.Net.Sockets;

namespace InvSync;

class SyncConnection
{
    const int MaxPacketLen = 1_000_000;
    const int retryAttemps = 10;

    static ArrayPool<byte> pool = ArrayPool<byte>.Shared;

    Socket s;
    PacketHeader CurrentPacket;

    byte[] buf;
    int ToRead;
    int Red;

    public SyncConnection(Socket s)
    {
        this.s = s;

        ToRead = 5;
        Red = 0;

        buf = pool.Rent(ToRead);

        CurrentPacket.ID = 0;
        CurrentPacket.Len = 0;
    }

    public static void Listen(Socket s)
    {
        if (!s.Connected)
        {
            s.Dispose();
            return;
        }

        SyncConnection co = new SyncConnection(s);

        co.StartListening();
    }

    void Update()
    {
        //finished reading data
        if (Red == ToRead)
        {
            //need to parse header
            if (CurrentPacket.Len == 0)
            {
                CurrentPacket = Utils.ByteArrayToStructure<PacketHeader>(buf);

                Utils.ReturnBuf(ref buf, pool);

                buf = pool.Rent(CurrentPacket.Len);
            }
            //need to parse packet
            else
            {

            }
        }
        //still data to read
        else
        {

        }
    }

    void StartListening()
    {
        for (int i = 1; i <= retryAttemps; i++)
            try
            {
                s.BeginReceive(buf, Red, ToRead, SocketFlags.None, OnReceived, this);
                return;
            }
            catch (Exception e)
            {
                if (i == retryAttemps)
                {
                    Logger.LogError(e.ToString());
                    throw;
                }
            }
    }

    static void OnReceived(IAsyncResult res)
    {
        if (res.AsyncState is null)
            return;

        SyncConnection co = (SyncConnection)res.AsyncState;

        co.Red += co.s.EndReceive(res);

        co.Update();
    }
}
