using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace InvSync;
static class InvSync
{
    static string Path;
    static Socket? listener = null;

    static InvSync()
    {
        Path = new FileInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName).DirectoryName;

    }

    public static void Listen(IPEndPoint ep)
    {
        if (listener is not null)
            throw new Exception("Can't start listening twice");

        listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(ep);
        listener.Listen(8);

        StartListening();
    }

    const int retryAttemps = 10;
    static void StartListening()
    {
        for (int i = 1; i <= retryAttemps; i++)
            try
            {
                listener.BeginAccept(OnConnectionReceived, null);
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

    static void OnConnectionReceived(IAsyncResult res)
    {
        Socket s = listener.EndAccept(res);

        s.NoDelay = true;
        s.LingerState.Enabled = false;

        SyncConnection.Listen(s);

        StartListening();
    }
}
