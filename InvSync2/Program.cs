using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace InvSync;
static class Progam
{
    public static void Main(string[] args)
    {
        InvSync.Listen();

        //Test();

        while (true)
        {
            Logger.TitleWrite($"{InvSync.RequestPerS:0.00} R/s");
            Thread.Sleep(250);
        }
    }

    //------------------This Section is only use for testing------------------------

    static void Test()
    {
        for (byte i = 0; i < 8; i++)
        {
            byte tmp = i;

            Task.Run(() =>
            {
                Thread.Sleep(2000);

                using Socket s = new Socket(SocketType.Stream, ProtocolType.Tcp);

                s.Connect(new IPEndPoint(IPAddress.IPv6Loopback, 7342));
                s.NoDelay = true;
                s.LingerState.Enabled = false;

                while (true)
                {
                    try
                    {

                        Check(tmp, s);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e.ToString());
                        return;
                    }
                }
            });
        }
    }

    static void Check(byte id, Socket s)
    {
        byte[] buf = new byte[10001];

        new Random().NextBytes(buf);

        buf[0] = 1;
        buf[1] = 1;
        buf[2] = (byte)(((byte)'0') + id);

        SendToSocket(s, BitConverter.GetBytes(buf.Length));
        SendToSocket(s, buf);

        byte[] data = RcvFromSocket(s, 4);

        int size = BitConverter.ToInt32(data);

        data = RcvFromSocket(s, size);

        if (data[0] > 128)
            Logger.LogError($"Responded with {data[0]}");

        byte[] buf2 = new byte[3];

        buf2[0] = 0;
        buf2[1] = 1;
        buf2[2] = (byte)(((byte)'0') + id);

        SendToSocket(s, BitConverter.GetBytes(buf2.Length));
        SendToSocket(s, buf2);

        data = RcvFromSocket(s, 4);

        size = BitConverter.ToInt32(data);

        data = RcvFromSocket(s, size);

        if (data[0] > 128)
            Logger.LogError($"Responded with {data[0]}");

        for (int i = 1; i < data.Length; i++)
        {
            if (data[i] != buf[i + 2])
            {
                Logger.LogError("Invalid Data");
                break;
            }
        }

        byte[] buf3 = new byte[3];

        buf3[0] = 2;
        buf3[1] = 1;
        buf3[2] = (byte)(((byte)'0') + id);

        SendToSocket(s, BitConverter.GetBytes(buf3.Length));
        SendToSocket(s, buf3);

        data = RcvFromSocket(s, 4);

        size = BitConverter.ToInt32(data);

        data = RcvFromSocket(s, size);

        if (data[0] > 128)
            Logger.LogError($"Responded with {data[0]}");
    }

    static byte[] RcvFromSocket(Socket s, int len)
    {
        byte[] buf = new byte[len];

        int received = 0;
        int c = 0;

        while (received != len)
            if (s.Connected && c++ < 100)
                received += s.Receive(buf, received, buf.Length - received, SocketFlags.None);
            else
                throw new Exception("Couldn't receive data");

        return buf;
    }

    static void SendToSocket(Socket s, byte[] buf)
    {
        int sended = 0;
        int c = 0;

        while (sended != buf.Length)
            if (s.Connected && c++ < 100)
                sended += s.Send(buf, sended, buf.Length - sended, SocketFlags.None);
            else
                throw new Exception("Couldn't send data");
    }
}