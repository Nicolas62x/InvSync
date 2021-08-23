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

        public static void Main(string[] args)
        {

            ExePath = ExePath.Substring(0, ExePath.Length - ExePath.Split('/', '\\').Last().Length);
            Console.ForegroundColor = ConsoleColor.White;
            List<string> ips = new List<string>();
            if (!File.Exists(ExePath + "settings.yaml"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                LogWarn("Couldn't find settings.yaml\nCreating default one in " + ExePath);
                Console.WriteLine(defaultSettings);
                StreamWriter sw = File.CreateText(ExePath + "settings.yaml");
                sw.Write(defaultSettings);
                sw.Dispose();

                Console.WriteLine("-------------");
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

        static byte[] RcvFromSocket(Socket s, int len)
        {
            byte[] buf = new byte[len];

            int received = 0;

            while (received != len)
                received += s.Receive(buf, received, buf.Length - received, SocketFlags.None);

            return buf;
        }

        static void HandleRequest(Socket s)
        {
            if (!s.Connected)
                LogError("Socket was not connected");

            try
            {
                DateTime start = DateTime.Now;

                byte[] packet;

                try
                {
                    packet = RcvFromSocket(s, 4);
                }
                catch (Exception)
                {
                    LogError("Couldn't read packet size");
                    return;
                }

                int size = BitConverter.ToInt32(packet, 0);

                if (size > 1_000_000_000 || size <= 0)
                {
                    LogError("Invalid packet size :" + size);
                    return;
                }

                try
                {
                    packet = RcvFromSocket(s, size);
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

                        try
                        {

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
                        }
                        catch (Exception e)
                        {
                            LogError($"Error adding server: {name}\n{e}");
                        }

                        break;

                    case 3://login

                        try
                        {
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
                        }
                        catch (Exception e)
                        {
                            LogError($"Error in login of: {name}\n{e}");
                        }

                        break;

                    case 4://disconnect

                        try
                        {
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
                                        LogWarn($"Player {name2} is not in {name} (Disconnect Request)");
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
                        }
                        catch (Exception e)
                        {
                            LogError($"Error in disconnect of: {name}\n{e}");
                        }

                        break;

                    case 5://player count

                        try
                        {
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
                                    LogWarn("Server: " + name + " Doesn't exist");

                                    s.Send(BitConverter.GetBytes(1));
                                    s.Send(new byte[1] { 253 });
                                }

                            }
                        }
                        catch (Exception e)
                        {
                            LogError($"Error in playercount of: {name}\n{e}");
                        }

                        break;

                    case 6://prelogin

                        try
                        {
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
                        }
                        catch (Exception e)
                        {
                            LogError($"Error in prelogin of: {name}\n{e}");
                        }

                        break;


                    case 7://list

                        try
                        {
                            int count = -1;

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
                                    LogWarn("Server: " + name + " Doesn't exist (player list request)");

                                    s.Send(BitConverter.GetBytes(1));
                                    s.Send(new byte[1] { 253 });

                                }

                            }
                        }
                        catch (Exception e)
                        {
                            LogError($"Error in list of: {name}\n{e}");
                        }

                        break;

                    case 11://suprime le fichier
                       

                        Log("Delete request of " + name + ".dat");

                    rst3:

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
                                goto rst3;

                            saving.Add(name);
                        }

                        try
                        {
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
                        }
                        catch (Exception e)
                        {
                            LogError($"Error in delete request of {name}\n{e}");
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
    }
}
