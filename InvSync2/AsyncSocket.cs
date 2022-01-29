using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace InvSync;
class AsyncSocket
{
    const int retryAttemps = 10;

    static ArrayPool<byte> pool = ArrayPool<byte>.Shared;

    public delegate void ReceiveCB(byte[] buffer, int len);

    Socket s;
    ReceiveCB cb;

    byte[] RBuffer;
    int RCount;
    int ROffset;

    byte[] SBuffer;
    int SCount;
    int SOffset;

    public AsyncSocket(Socket s, ReceiveCB cb)
    {
        this.s = s;
        this.cb = cb;

        RBuffer = null;
        RCount = 0;
        ROffset = 0;

        SBuffer = null;
        SCount = 0;
        SOffset = 0;
    }

    ~AsyncSocket()
    {
        s.Dispose();
        Utils.ReturnBuf(ref RBuffer, pool);
    }

    public void ReadData(int len)
    {
        RCount = len;
        ROffset = 0;

        Utils.ReturnBuf(ref RBuffer, pool);
        RBuffer = pool.Rent(RCount);

        StartListening();
    }

    public void SendData(byte[] data)
    {
        while (s.Connected && SBuffer is not null)
            Thread.Sleep(1);

        SBuffer = data;
        SCount = data.Length;
        SOffset = 0;

        StartSending();
    }

    public void Kill(byte[] killPacket = null)
    {
        if (s.Connected && killPacket is not null)
        {
            s.Send(killPacket);
        }

        s.Dispose();
        Utils.ReturnBuf(ref RBuffer, pool);
    }

    static void OnReceived(IAsyncResult res)
    {
        if (res.AsyncState is null)
            return;

        AsyncSocket co = (AsyncSocket)res.AsyncState;

        try
        {
            int rcv = co.s.EndReceive(res);

            if (rcv <= 0)
                throw new Exception();

            co.ROffset += rcv;

            if (co.ROffset == co.RCount)
                co.cb(co.RBuffer, co.RCount);
            else
                co.StartListening();
        }
        catch (Exception)
        {
            co.s.Dispose();
        }
    }

    static void OnSent(IAsyncResult res)
    {
        if (res.AsyncState is null)
            return;

        AsyncSocket co = (AsyncSocket)res.AsyncState;

        try
        {
            int snd = co.s.EndSend(res);

            if (snd <= 0)
                throw new Exception();

            co.SOffset += snd;

            if (co.SOffset != co.SCount)
                co.StartSending();

            co.SBuffer = null;
        }
        catch (Exception)
        {
            co.s.Dispose();
        }
    }

    void StartListening()
    {
        for (int i = 1; i <= retryAttemps; i++)
            try
            {
                s.BeginReceive(RBuffer, ROffset, RCount - ROffset, SocketFlags.None, OnReceived, this);
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

    void StartSending()
    {
        for (int i = 1; i <= retryAttemps; i++)
            try
            {
                s.BeginSend(SBuffer, SOffset, SCount - SOffset, SocketFlags.None, OnSent, this);
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
}