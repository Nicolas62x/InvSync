using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace invsinc
{
    static class dataBaseServer
    {
        static ushort port = 7342;
        static Socket listener;
        static List<string> saving = new List<string>();
        static Dictionary<string, DateTime> prelogin = new Dictionary<string, DateTime>();
        static Dictionary<string, List<string>> Servers = new Dictionary<string, List<string>>();
        static int playerCount = 0;
        static object playercountlock = new object();
        static int preloginTo = 30000;
        static string ExePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        static string defaultSettings = "#  __  __  _____  _____ _                        __      \n# |  \\/  |/ ____|/ ____| |                      / _|     \n# | \\  / | |    | (___ | |__   __ _ _ __ _ __  | |_ _ __ \n# | |\\/| | |     \\___ \\| '_ \\ / _` | '__| '_ \\ |  _| '__|\n# | |  | | |____ ____) | | | | (_| | |  | |_) || | | |   \n# |_|  |_|\\_____|_____/|_| |_|\\__,_|_|  | .__(_)_| |_|   \n#                                       | |              \n#                                       |_|              \n#whitelisted ips:\nIPs:\n - 127.0.0.1\n#PreloginTimeOut (ms)\nPl_To: 30000\n#port\nPort: 7342";

        static Dictionary<string, Dictionary<string, string>> listes = new Dictionary<string, Dictionary<string, string>>();
        static List<string> extIp = new List<string>();
        static List<string> tosave = new List<string>();

        static string lastLock = "";

        static bool debug = false;

        static string FilesPath = "";

        static byte running = 1;

        static UInt64 count = 0;

        static object FileLock = new object();

        public static void SaveRequest(int id, int seed)
        {
            Socket cli = new Socket(SocketType.Stream, ProtocolType.Tcp);
            cli.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
            cli.Connect(new IPEndPoint(IPAddress.IPv6Loopback, 7342));

            List<byte> data = new List<byte>();

            data.Add(1);
            data.Add((byte)id.ToString().Length);

            data.AddRange(Encoding.UTF8.GetBytes(id.ToString()));

            Random R = new Random(seed);

            for (int i = 0; i < 2048; i++)
            {
                data.Add((byte)R.Next(256));
            }

            

            cli.Send(BitConverter.GetBytes(data.Count));
            cli.Send(data.ToArray());

            byte[] len = new byte[4];

            cli.Receive(len);

            int len2 = BitConverter.ToInt32(len);

            byte[] dat = new byte[len2];

            cli.Receive(dat);

            if (dat[0] != 1)
                LogError($"Couldn't save id {id}");

            cli.Dispose();
        }

        public static byte[] LoadRequest(int id)
        {
            Socket cli = new Socket(SocketType.Stream, ProtocolType.Tcp);
            cli.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
            cli.Connect(new IPEndPoint(IPAddress.IPv6Loopback, 7342));

            List<byte> data = new List<byte>();

            data.Add(0);
            data.Add((byte)id.ToString().Length);

            data.AddRange(Encoding.UTF8.GetBytes(id.ToString()));

            cli.Send(BitConverter.GetBytes(data.Count));
            cli.Send(data.ToArray());

            byte[] len = new byte[4];

            cli.Receive(len);

            int len2 = BitConverter.ToInt32(len);

            byte[] dat = new byte[len2];

            int tmp = 0;

            while (tmp != len2)
                tmp+= cli.Receive(dat,tmp,len2-tmp, SocketFlags.None);

            cli.Dispose();

            if (dat[0] != 0)
                LogError($"Couldn't read id {id}");

            return dat.TakeLast(len2 - 1).ToArray();
        }

        public static void Main(string[] args)
        {
            /*for (int i = 0; i < 2; i++)
            {

                int val = i;

                Task.Run(() =>
                {
                    Thread.Sleep(1000);

                    Random r = new Random((int)DateTime.Now.Ticks);

                    while (true)
                    {
                        int value = r.Next();
                        SaveRequest(val, value);

                        byte[] dat = LoadRequest(val);

                        Random R = new Random(value);

                        for (int i = 0; i < 2048; i++)
                        {
                            if (R.Next(256) != dat[i])
                            {
                                LogError("Error didn't read correctly");
                                break;
                            }
                        }


                    }


                });

            }*/

            ExePath = ExePath.Substring(0, ExePath.Length - ExePath.Split('/', '\\').Last().Length);
            Console.ForegroundColor = ConsoleColor.White;
            List<string> ips = new List<string>();
            if (!File.Exists(ExePath + "settings.yaml"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                LogWarn(" Couldn't find settings.yaml\nCreating default one in " + ExePath);
                Console.WriteLine(defaultSettings);
                StreamWriter sw = File.CreateText(ExePath + "settings.yaml");
                sw.Write(defaultSettings);
                sw.Dispose();

                Console.WriteLine("-------------");
            }

            {/*
                if (!Directory.Exists(ExePath + "list"))
                    Directory.CreateDirectory(ExePath + "list");

                List<string> listestoread = new List<string>();

                string[] files = Directory.GetFiles(ExePath + "list");

                foreach (string f in files)
                {
                    string[] F = f.Remove(0, (ExePath + "list/").Length).Split('.');

                    if ((F[1] == "back" || F[1] == "mcslist") && !listestoread.Contains(F[0]))
                    {
                        listestoread.Add(F[0]);
                    }

                    if (F[1] == "back")
                    {
                        if (File.Exists(ExePath + "list/" + F[0] + ".mcslist"))
                        {
                            if (!Directory.Exists(ExePath + "list/Backups"))
                                Directory.CreateDirectory(ExePath + "list/Backups");

                            if (File.Exists(ExePath + "list//Backups/" + F[0] + ".mcslist"))
                                File.Delete(ExePath + "list//Backups/" + F[0] + ".mcslist");

                            File.Move(ExePath + "list/" + F[0] + ".mcslist", ExePath + "list//Backups/" + F[0] + ".mcslist");
                            LogWarn(" Moved (most likely) corrupted file: " + F[0] + ".mcslist" + " to " + ExePath + "list//Backups/" + F[0] + ".mcslist");
                        }

                        File.Move(ExePath + "list/" + F[0] + ".back", ExePath + "list/" + F[0] + ".mcslist");
                        LogWarn(" Restored: " + F[0] + " with a backup, last saving process must have been interupted");

                    }


                }

                if (listestoread.Count > 0)
                {
                    foreach (string s in listestoread)
                    {
                        Console.WriteLine("Loading " + s);

                        FileStream fs = File.OpenRead(ExePath + "list/" + s + ".mcslist");

                        if (fs.Length > Int32.MaxValue)
                        {
                            LogError(" file is bigger than max supported size (" + s + ".mcslist)");
                            Console.ReadKey();
                            return;
                        }

                        byte[] data = new byte[fs.Length];

                        fs.Read(data, 0, (int)fs.Length);
                        fs.Close();

                        int ptr = 0;

                        Dictionary<string, string> dic = new Dictionary<string, string>();

                        while (ptr < data.Length)
                        {
                            byte len = data[ptr];
                            ptr++;
                            string identifier = UTF8Encoding.UTF8.GetString(data, ptr, len);
                            ptr += len;
                            int datlen = BitConverter.ToInt32(data, ptr);
                            ptr += 4;
                            string dat = UTF8Encoding.UTF8.GetString(data, ptr, datlen);
                            ptr += datlen;
                            dic.Add(identifier, dat);
                        }

                        listes.Add(s, dic);
                    }
                }
                */
            }

            {
                Log("Reading Settings.yaml...");

                StreamReader sr = File.OpenText(ExePath + "settings.yaml");

                string config = sr.ReadToEnd();

                sr.Dispose();

                string[] lines = config.Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].StartsWith("#"))
                    {

                        if (lines[i].StartsWith("IPs:"))
                        {
                            Console.WriteLine("Whitelisted ips:");
                            i++;
                            while (lines[i].Trim().StartsWith("-"))
                            {
                                string[] ip = lines[i].Trim().Split(' ');

                                if (ip.Length == 2)
                                {
                                    ips.Add(ip[1]);
                                    Console.WriteLine(ip[1]);
                                }
                                else
                                {
                                    Console.WriteLine("Unknown argument: \"" + lines[i] + "\"");
                                }

                                i++;
                            }
                        }
                        else if (lines[i].StartsWith("Pl_To: "))
                        {

                            if (int.TryParse(lines[i].Substring(7).Trim(), out int val))
                            {
                                preloginTo = val;
                                Console.WriteLine("Prelogin time out: " + val);
                            }
                            else
                                Console.WriteLine("Unknown argument: \"" + lines[i] + "\"");
                        }
                        else if (lines[i].StartsWith("Port: "))
                        {
                            if (ushort.TryParse(lines[i].Substring(5).Trim(), out ushort val))
                            {
                                port = val;
                                Console.WriteLine("port: " + val);
                            }
                            else
                                Console.WriteLine("Unknown argument: \"" + lines[i] + "\"");
                        }
                        else if (lines[i] != "")
                            Console.WriteLine("Unknown argument: \"" + lines[i] + "\"");
                    }
                }


            }

            listener = new Socket(SocketType.Stream,ProtocolType.Tcp);

            listener.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
            listener.Listen();

            FilesPath = ExePath + "/Dats/";

            if (!Directory.Exists(FilesPath))
                Directory.CreateDirectory(FilesPath);

            Log("Running on port: " + port);

            

            
            UInt64 Lcount = 0;
            double ps = 0;

            listener.BeginAccept(HandleCo, null);

            /*Task.Run(() =>
            {
                
                
                while (running == 1)
                {
                    TcpClient tcp;

                    try
                    {
                        TitleWrite($"{ playerCount + " Online   " + Servers.Count + " Server   " + prelogin.Count + " Prelogin " + ps.ToString("F2") + "/s"}");


                        tcp = listener.AcceptTcpClient();
                        dt = DateTime.Now;
                        count++;
                    }
                    catch (Exception e)
                    {
                        LogError(e.ToString());
                        continue;
                    }

                    

                    if (!ips.Contains(((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString()))
                    {
                        LogWarn(" Ip is not whitelisted : " + ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString());

                        if (!extIp.Contains(((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString().Split(':')[0]))
                            extIp.Add(((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString().Split(':')[0]);

                        try
                        {
                            tcp.Close();
                            tcp.Dispose();
                        }
                        catch (Exception e)
                        {
                            LogError(e.ToString());
                        }


                    }
                    else


                }

                running = 2;

            });*/

            Task.Run(() =>
            {
                TimeSpan ts = TimeSpan.FromMilliseconds(250);
                while (running == 1)
                {
                    
                    Thread.Sleep(ts);

                    UInt64 c = Lcount;

                    Lcount = count;

                    c = Lcount - c;

                    ps = ps * 0.80 + c * 0.20;

                    try
                    {
                        TitleWrite($"{ playerCount + " Online   " + Servers.Count + " Server   " + prelogin.Count + " Prelogin " + ps.ToString("F2") + "/s"}");
                    }
                    catch (Exception)
                    {
                    }

                }
            });

            /*Task.Run(() =>
            {
                TimeSpan ts = TimeSpan.FromMinutes(5);
                while (running == 1)
                {
                    
                    Thread.Sleep(ts);

                    saveLists();

                }
            });*/


            while (true)
            {
                string s = Console.ReadLine();

                if (s == "stop")
                {
                    Console.WriteLine("Stopping...");
                    running = 0;

                    try
                    {
                        listener.Dispose();
                    }
                    catch (Exception)
                    {

                    }

                    while (running == 0)
                        Thread.Sleep(1);

                    Console.WriteLine("Stopped");

                    //saveLists();

                    return;
                }
                else if (s == "list")
                {
                    if (lastLock != "")
                        Console.WriteLine("Waiting for: " + lastLock);

                    int total = 0;

                    lock (Servers)
                    {
                        lastLock = "this";
                        try
                        {
                            if (Servers.Count > 0)
                            {
                                Console.WriteLine("Current servers:");
                                foreach (KeyValuePair<string, List<string>> pair in Servers)
                                {
                                    Console.WriteLine(pair.Key + " - Online: " + pair.Value.Count);
                                    total += pair.Value.Count;
                                }
                                Console.WriteLine("Total: " + total);
                            }
                            else
                            {
                                Console.WriteLine("There are currently no server running");
                            }
                        }
                        catch (Exception e)
                        {
                            LogError(e.ToString());
                        }
                        lastLock = "";
                    }

                }
                else if (s == "ext")
                {
                    if (extIp.Count > 0)
                    {
                        Console.WriteLine("Unauthorized Ips connections requests:");
                        string[] ipss = extIp.ToArray();
                        foreach (string ss in ipss)
                            Console.WriteLine(ss);
                    }
                    else
                        Console.WriteLine("There are no unauthorized ips that have tried to connect");

                }
                else if (s == "debug")
                {
                    debug = !debug;
                    Console.WriteLine("Debug mode: " + debug);
                }
                else
                {
                    Console.WriteLine("Command list: ");
                    Console.WriteLine("stop: stop the database correctly");
                    Console.WriteLine("list: list all registered servers");
                    Console.WriteLine("ext: list all not whitelisted ips that tryied to connect");
                    Console.WriteLine("debug: enable / disable debug");
                }


            }
        }

        static void HandleCo(IAsyncResult res)
        {
            Socket s;

            try
            {
                s = listener.EndAccept(res);
            }
            catch (Exception)
            {
                s = null;
            }

        a:
            int c = 0;

            try
            {
                if (running == 1)
                    listener.BeginAccept(HandleCo, null);
                else
                    running = 2;
            }
            catch (Exception)
            {
                if (c++ > 100)
                    throw;
                goto a;
            }

            if (s is not null)
            {
                HandleRequest(s);
                count++;
                s.Dispose();
            }
                
        }

        static void HandleRequest(Socket s)
        {
            if (!s.Connected)
                LogError("Socket was not connected");

            try
            {
                DateTime start = DateTime.Now;

                byte[] packet = new byte[4];

                try
                {
                    if (s.Receive(packet,4, SocketFlags.None) != 4)
                        throw new Exception();
                }
                catch (Exception)
                {
                    LogError("Couldn't read 4 bytes");
                    return;
                }

                int size = BitConverter.ToInt32(packet, 0);

                if (size > 1000000000 || size <= 0)
                {
                    LogError("Invalid packet size :" + size);
                    return;
                }

                packet = new byte[size];

                try
                {
                    if (s.Receive(packet) != size)
                        throw new Exception();
                }
                catch (Exception)
                {
                    LogError($"Couldn't read {packet.Length} bytes");
                    return;
                }

                int ptr = 0;

                byte packetid = packet[ptr];

                ptr++;

                byte namelen = packet[ptr];
                ptr++;

                string name = UTF8Encoding.UTF8.GetString(packet, ptr, namelen);
                ptr += namelen;

                switch (packetid)
                {
                    case 0://demande du fichier


                        Log("FileRequest of " + name + ".dat");

                        rst:

                        try
                        {
                            while (saving.Contains(name))
                                Thread.Sleep(1);
                        }
                        catch (Exception)
                        {

                        }

                        lock (FileLock)
                        {
                            if (saving.Contains(name))
                                goto rst;

                            saving.Add(name);
                        }

                        try
                        {
                            if (File.Exists(FilesPath + name + ".dat"))
                            {
                                byte[] dat2 = File.ReadAllBytes(FilesPath + name + ".dat");

                                s.Send(BitConverter.GetBytes(dat2.Length + 1));
                                s.Send(new byte[1]);
                                s.Send(dat2);

                                ulong value = 0;
                                for (int i = 0; i < dat2.Length; i++)
                                {
                                    value += dat2[i];
                                }
                                //Log("FileRequest Sended " + name + ".dat -> " + value.ToString("X") + " " + dat2.Length + " bytes");
                            }
                            else
                            {
                                s.Send(BitConverter.GetBytes(1));
                                s.Send(new byte[1] { 254 });
                            }
                        }
                        catch (Exception e)
                        {
                            LogError($"Error in file request of {name}\n{e}");
                        }

                        

                        lock (FileLock)
                        {
                            saving.Remove(name);
                        }

                        break;


                    case 1://ecrire le fichier

                        Log("SaveRequest of " + name + ".dat");

                        rst2:

                        try
                        {
                            while (saving.Contains(name))
                                Thread.Sleep(1);
                        }
                        catch (Exception)
                        {

                        }

                        

                        lock (FileLock)
                        {
                            if (saving.Contains(name))
                                goto rst2;

                            saving.Add(name);
                        }

                        try
                        {
                            int filesize = size - ptr;

                            if (filesize > 0)
                            {
                                byte[] file = new byte[filesize];


                                for (int i = 0; i < filesize; i++)
                                {
                                    file[i] = packet[ptr + i];
                                }

                                FileStream fs = File.Open(FilesPath + name + ".dat", FileMode.Create);

                                fs.Write(file, 0, file.Length);

                                fs.Dispose();

                                s.Send(BitConverter.GetBytes(1));
                                s.Send(new byte[1] { 1 });

                                ulong value = 0;
                                for (int i = 0; i < file.Length; i++)
                                {
                                    value += file[i];
                                }
                                //Log("SaveRequest Writed " + name + ".dat -> " + value.ToString("X") + " " + file.Length + " bytes");
                            }
                            else
                            {
                                s.Send(BitConverter.GetBytes(1));
                                s.Send(new byte[1] { 255 });
                            }
                        }
                        catch (Exception e)
                        {
                            LogError($"Error in save request of {name}\n{e}");
                        }                        

                        lock (FileLock)
                        {
                            saving.Remove(name);
                        }

                        break;


                    case 2://add a server

                        Log("Adding server: " + name);

                        lock (Servers)
                        {
                            lastLock = "Add a Server";
                            try
                            {
                                if (Servers.ContainsKey(name))
                                {
                                    lock (playercountlock)
                                        if (Servers.TryGetValue(name, out List<string> srv))
                                            playerCount -= srv.Count;
                                    Servers.Remove(name);
                                }


                                Servers.Add(name, new List<string>());
                            }
                            catch (Exception e)
                            {
                                LogError(e.ToString());
                            }
                            lastLock = "";
                        }

                        s.Send(BitConverter.GetBytes(1));
                        s.Send(new byte[1] { 2 });

                        break;

                    case 3://login



                        if (Servers.TryGetValue(name, out List<string> serv))
                        {
                            byte namelen2 = packet[ptr];
                            ptr++;

                            string name2 = UTF8Encoding.UTF8.GetString(packet, ptr, namelen2);
                            ptr += namelen;

                            lock (prelogin)
                            {



                                lock (Servers)
                                {
                                    lastLock = "Login";
                                    try
                                    {
                                        foreach (List<string> servs in Servers.Values)
                                        {
                                            lock (servs)
                                            {
                                                if (servs.Contains(name2))
                                                {
                                                    s.Send(BitConverter.GetBytes(1));
                                                    s.Send(new byte[1] { 252 });

                                                    LogWarn($"Player {name2} is allready connected");

                                                    return;
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        LogError(e.ToString());
                                    }
                                    lastLock = "";
                                }

                                lock (serv)
                                {
                                    Log("login " + name2 + " on: " + name);
                                    serv.Add(name2);


                                    if (prelogin.ContainsKey(name2))
                                        prelogin.Remove(name2);
                                }


                            }

                            lock (playercountlock)
                            {
                                playerCount++;
                            }

                            s.Send(BitConverter.GetBytes(1));
                            s.Send(new byte[1] { 3 });

                        }
                        else
                        {
                            s.Send(BitConverter.GetBytes(1));
                            s.Send(new byte[1] { 253 });
                        }


                        break;

                    case 4://deconect

                        if (Servers.TryGetValue(name, out List<string> serv2))
                        {
                            byte namelen2 = packet[ptr];
                            ptr++;

                            string name2 = UTF8Encoding.UTF8.GetString(packet, ptr, namelen2);
                            ptr += namelen;

                            lock (serv2)
                            {

                                if (serv2.Contains(name2))
                                {
                                    Log("Disconecting " + name2 + " from: " + name);
                                    serv2.Remove(name2);
                                }
                                else
                                {
                                    LogWarn($"Player {name2} is not in {name} (Deconect Request)");
                                }

                            }

                            lock (playercountlock)
                            {
                                playerCount--;
                            }

                            s.Send(BitConverter.GetBytes(1));
                            s.Send(new byte[1] { 3 });

                        }
                        else
                        {
                            s.Send(BitConverter.GetBytes(1));
                            s.Send(new byte[1] { 253 });
                        }


                        break;

                    case 5://player count

                        int count = -1;

                        if (name == "all")
                        {

                            lock (Servers)
                            {
                                lastLock = "PlayerCount";
                                try
                                {
                                    count = 0;
                                    foreach (List<string> servs in Servers.Values)
                                    {
                                        count += servs.Count;
                                    }
                                }
                                catch (Exception e)
                                {
                                    LogError(e.ToString());
                                }
                                lastLock = "";
                            }

                            lock (playercountlock)
                                playerCount = count;

                            s.Send(BitConverter.GetBytes(5));
                            s.Send(new byte[1] { 5 });
                            s.Send(BitConverter.GetBytes(count));

                        }
                        else
                        {

                            lock (Servers)
                            {
                                lastLock = "PlayerCount2";
                                if (Servers.TryGetValue(name, out List<string> servv))
                                {
                                    count = servv.Count;
                                }
                                lastLock = "";
                            }

                            if (count > -1)
                            {
                                //Console.WriteLine("Sent " + name + " PlayerCount: " + count);

                                s.Send(BitConverter.GetBytes(5));
                                s.Send(new byte[1] { 5 });
                                s.Send(BitConverter.GetBytes(count));
                            }
                            else
                            {
                                LogWarn(" Server: " + name + " Doesn't exist");

                                s.Send(BitConverter.GetBytes(1));
                                s.Send(new byte[1] { 253 });
                            }

                        }


                        break;

                    case 6://prelogin



                        lock (prelogin)
                        {
                            if (prelogin.ContainsKey(name) && prelogin.TryGetValue(name, out DateTime t0))
                            {
                                if ((DateTime.Now - t0).TotalMilliseconds < preloginTo)
                                {
                                    s.Send(BitConverter.GetBytes(1));
                                    s.Send(new byte[1] { 252 });

                                    LogWarn($"Player {name} is allready preloged");

                                    return;
                                }
                                else
                                {
                                    prelogin.Remove(name);
                                }

                            }

                            lock (Servers)
                            {
                                lastLock = "Prelog";
                                try
                                {
                                    foreach (List<string> servs in Servers.Values)
                                    {
                                        lock (servs)
                                        {
                                            if (servs.Contains(name))
                                            {

                                                s.Send(BitConverter.GetBytes(1));
                                                s.Send(new byte[1] { 252 });

                                                LogWarn($"Player {name} is allready connected");

                                                return;
                                            }
                                        }

                                    }
                                }
                                catch (Exception e)
                                {
                                    LogError(e.ToString());
                                }

                                lastLock = "";
                            }

                            prelogin.Add(name, DateTime.Now);
                        }


                        Log("Prelogin " + name);

                        s.Send(BitConverter.GetBytes(1));
                        s.Send(new byte[1] { 3 });


                        Thread.Sleep(preloginTo);

                        lock (prelogin)
                            if (prelogin.ContainsKey(name))
                            {
                                prelogin.Remove(name);

                                Log("Prelogin TimeOut of " + name);
                            }


                        break;

                        
                            case 7://list

                                count = -1;

                                List<string> players = new List<string>();

                        if (name == "all")
                        {

                            lock (Servers)
                            {
                                lastLock = "List";
                                try
                                {
                                    foreach (List<string> servs in Servers.Values)
                                    {
                                        lock (servs)
                                        {
                                            foreach (string s2 in servs)
                                                players.Add(s2);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    LogError(e.ToString());
                                }
                                lastLock = "";
                            }

                            count = players.Count;

                            lock (playercountlock)
                                playerCount = count;

                            string text = "";

                            foreach (string s2 in players)
                            {
                                text += text.Length > 0 ? " " + s2 : s2;
                            }
                            byte[] buf2 = UTF8Encoding.UTF8.GetBytes(text);

                            s.Send(BitConverter.GetBytes(5 + buf2.Length));
                            s.Send(new byte[1] { 6 });
                            s.Send(BitConverter.GetBytes(count));
                            s.Send(buf2);

                        }
                        else
                        {

                            lock (Servers)
                            {
                                lastLock = "List2";
                                try
                                {
                                    if (Servers.TryGetValue(name, out List<string> servv))
                                    {
                                        lock (servv)
                                        {
                                            foreach (string s2 in servv)
                                                players.Add(s2);
                                        }
                                        count = players.Count;
                                    }
                                }
                                catch (Exception e)
                                {
                                    LogError(e.ToString());
                                }
                                lastLock = "";
                            }

                            if (count > -1)
                            {

                                string text = "";

                                foreach (string s2 in players)
                                {
                                    text += text.Length > 0 ? " " + s2 : s2;
                                }
                                byte[] buf3 = UTF8Encoding.UTF8.GetBytes(text);

                                s.Send(BitConverter.GetBytes(5 + buf3.Length));
                                s.Send(new byte[1] { 6 });
                                s.Send(BitConverter.GetBytes(count));
                                s.Send(buf3);
                            }
                            else
                            {
                                LogWarn(" Server: " + name + " Doesn't exist (player list request)");

                                s.Send(BitConverter.GetBytes(1));
                                s.Send(new byte[1] { 253 });

                            }

                        }

                        break;

                        { /*

                            case 8://add element in list

                                byte len = packet[ptr];
                                ptr++;
                                string identifier = UTF8Encoding.UTF8.GetString(packet, ptr, len);
                                ptr += len;

                                int datlen = BitConverter.ToInt32(packet, ptr);
                                string data = UTF8Encoding.UTF8.GetString(packet, ptr, len);

                                lock (listes)
                                {
                                    if (listes.TryGetValue(name, out Dictionary<string, string> dic))
                                    {
                                        lock (dic)
                                        {
                                            if (dic.ContainsKey(identifier))
                                            {
                                                dic.Remove(identifier);
                                            }
                                            dic.Add(identifier, data);
                                        }
                                    }
                                    else
                                    {
                                        Dictionary<string, string> dic2 = new Dictionary<string, string>();
                                        dic2.Add(identifier, data);
                                        listes.Add(name, dic2);
                                    }
                                }

                                strm.Write(BitConverter.GetBytes(1), 0, 4);
                                strm.WriteByte(7);
                                strm.Flush();

                                lock (tosave)
                                {
                                    tosave.Add(name);
                                }

                                break;

                            case 9://remove element in list

                                len = packet[ptr];
                                ptr++;
                                identifier = UTF8Encoding.UTF8.GetString(packet, ptr, len);
                                ptr += len;


                                lock (listes)
                                {
                                    if (listes.TryGetValue(name, out Dictionary<string, string> dic))
                                    {
                                        lock (dic)
                                        {
                                            if (dic.ContainsKey(identifier))
                                            {
                                                dic.Remove(identifier);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        strm.Write(BitConverter.GetBytes(1), 0, 4);
                                        strm.WriteByte(251);
                                        strm.Flush();
                                        LogWarn(" List: " + name + " Doesn't exist");
                                        break;
                                    }
                                }
                                strm.Write(BitConverter.GetBytes(1), 0, 4);
                                strm.WriteByte(8);
                                strm.Flush();

                                lock (tosave)
                                {
                                    tosave.Add(name);
                                }

                                break;

                            case 10://request element in list

                                len = packet[ptr];
                                ptr++;
                                identifier = UTF8Encoding.UTF8.GetString(packet, ptr, len);
                                ptr += len;

                                string dat = "";

                                lock (listes)
                                {
                                    if (listes.TryGetValue(name, out Dictionary<string, string> dic))
                                    {
                                        lock (dic)
                                        {
                                            if (!dic.TryGetValue(identifier, out dat))
                                            {
                                                strm.Write(BitConverter.GetBytes(1), 0, 4);
                                                strm.WriteByte(250);
                                                strm.Flush();
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        strm.Write(BitConverter.GetBytes(1), 0, 4);
                                        strm.WriteByte(251);
                                        strm.Flush();

                                        LogWarn(" List: " + name + " Doesn't exist");
                                        break;
                                    }
                                }
                                byte[] buf = UTF8Encoding.UTF8.GetBytes(dat);
                                strm.Write(BitConverter.GetBytes(5 + buf.Length), 0, 4);
                                strm.WriteByte(9);
                                strm.Write(BitConverter.GetBytes(buf.Length), 0, 4);
                                strm.Write(buf, 0, buf.Length);
                                strm.Flush();

                                break;*/
                        }

                    case 11://suprime le fichier

                        Log("Delete request of " + name + ".dat");

                        while (saving.Contains(name))
                            Thread.Sleep(1);

                        lock (saving)
                        {
                            saving.Add(name);
                        }

                        if (File.Exists(FilesPath + name + ".dat"))
                        {
                            File.Delete(FilesPath + name + ".dat");

                            s.Send(BitConverter.GetBytes(1));
                            s.Send(new byte[1] { 10 });

                            //Log("SaveRequest Writed " + name + ".dat -> " + value.ToString("X") + " " + file.Length + " bytes");
                        }
                        else
                        {
                            s.Send(BitConverter.GetBytes(1));
                            s.Send(new byte[1] { 249 });
                        }

                        lock (saving)
                        {
                            saving.Remove(name);
                        }

                        break;

                    default:

                        LogError($"Invalid Packet ({packet.Length} bytes) " + BitConverter.ToString(packet));

                        s.Send(BitConverter.GetBytes(1));
                        s.Send(new byte[1] { 255 });

                        break;
                }

            }
            catch (Exception e)
            {
                LogError($"Unknown Error\n{e}");

                try
                {
                    s.Send(BitConverter.GetBytes(1));
                    s.Send(new byte[1] { 255 });
                }
                catch (Exception)
                {


                }
            }
        }

        static Queue<string> TextQueue = new Queue<string>();
        static object console = new object();

        static Task ConsoleTask = Task.Run(() =>
        {
            while (running == 1)
            {
                while (TextQueue.Count != 0)
                    lock (console)
                        Console.Write(TextQueue.Dequeue());
                
                Thread.Sleep(5);
            }
        });

        static void Log(string text)
        {
            lock (console)
                TextQueue.Enqueue($"\u001b[97m[{DateTime.Now}] {text}\n");
        }

        static void LogWarn(string text)
        {
            lock (console)
                TextQueue.Enqueue($"\u001b[97m[{DateTime.Now}] \u001b[93m[Warn]\u001b[97m {text}\n");
        }

        static void LogError(string text)
        {
            lock (console)
                TextQueue.Enqueue($"\u001b[97m[{ DateTime.Now}] \u001b[91m[Error]\u001b[97m {text}\n");
        }

        public static void TitleWrite(string text)
        {
            lock (console)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Console.Title = text;
                else
                    TextQueue.Enqueue($"\u001B]0;{text}\u0007");
        }

        /*static void saveLists()
        {
            lock (tosave)
            {
                while (tosave.Count > 0)
                {
                    string name = tosave.First();

                    Dictionary<string, string> dic = null;

                    lock (listes)
                    {
                        listes.TryGetValue(name, out dic);
                    }

                    if (dic != null)
                    {
                        if (File.Exists(ExePath + "list/" + name + ".mcslist"))
                            File.Move(ExePath + "list/" + name + ".mcslist", ExePath + "list/" + name + ".back");

                        FileStream fs = File.OpenWrite(ExePath + "list/" + name + ".mcslist");

                        lock (dic)
                        {
                            foreach (KeyValuePair<string, string> pair in dic)
                            {
                                byte[] buf = UTF8Encoding.UTF8.GetBytes(pair.Key);

                                fs.WriteByte((byte)buf.Length);
                                fs.Write(buf, 0, buf.Length);

                                buf = UTF8Encoding.UTF8.GetBytes(pair.Value);

                                fs.Write(BitConverter.GetBytes(buf.Length), 0, 4);
                                fs.Write(buf, 0, buf.Length);

                            }
                        }
                        fs.Flush();
                        fs.Close();
                    }

                    if (File.Exists(ExePath + "list/" + name + ".back"))
                        File.Delete(ExePath + "list/" + name + ".back");

                    tosave.Remove(name);

                }
            }
        }*/

    }
}
