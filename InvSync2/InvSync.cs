using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace InvSync;
static class InvSync
{
    public static readonly string Path;
    static Socket listener = null;
    static InvSyncConfig config;

    static InvSync()
    {
        Path = new FileInfo(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName).DirectoryName;

        if (Path is null)
            throw new Exception("Failled to get executable path");

        config = InvSyncConfig.LoadConfig();
    }

    public static void Listen()
    {
        if (listener is not null)
            throw new Exception("Can't start listening twice");

        listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.IPv6Any, config.Port));
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

        StartListening();

        if (config.IPs.Contains(((IPEndPoint)s.RemoteEndPoint).Address.ToString()))
        {
            s.NoDelay = true;
            s.LingerState.Enabled = false;

            SyncConnection.Listen(s);
        }
        else
        {
            Logger.LogWarn($"IP {((IPEndPoint)s.RemoteEndPoint).Address} is not autorized to connect");

            s.Dispose();
        }
    }

    const int AverageSeconds = 10;
    static Queue<DateTime> requests = new Queue<DateTime>();
    static object locker = new object();

    public static void AddRequest()
    {
        lock (locker)
        {
            UpdateRequests();
            requests.Enqueue(DateTime.Now.AddSeconds(AverageSeconds));
        }
    }

    static void UpdateRequests()
    {
        lock (locker)
            while (requests.Count > 0 && requests.Peek() < DateTime.Now)
                requests.Dequeue();
    }

    public static float RequestPerS 
    { 
        get 
        {
            lock (locker)
            {
                UpdateRequests();

                return requests.Count / (float)AverageSeconds;
            }
        } 
    }
}
