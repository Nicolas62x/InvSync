using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace InvSync;
static class Logger
{
    static Queue<string> TextQueue = new Queue<string>();
    static object console = new object();

    static Task ConsoleTask = Task.Run(() =>
    {
        while (true)
        {
            while (TextQueue.Count != 0)
                lock (console)
                    Console.Write(TextQueue.Dequeue());

            Thread.Sleep(5);
        }
    });

    public static void Log(string text)
    {
        lock (console)
            TextQueue.Enqueue($"\u001b[97m[{DateTime.Now}] {text}\n");
    }

    public static void LogWarn(string text)
    {
        lock (console)
            TextQueue.Enqueue($"\u001b[97m[{DateTime.Now}] \u001b[93m[Warn]\u001b[97m {text}\n");
    }

    public static void LogError(string text)
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
