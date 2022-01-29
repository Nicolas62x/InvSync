
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace InvSync;
static class FileManager
{
    public static readonly string Path;
    static FileManager()
    {
        Path = $"{InvSync.Path}/Dats/";

        if (!Directory.Exists(Path))
            Directory.CreateDirectory(Path);
    }

    static HashSet<string> files = new HashSet<string>();
    static object locker = new object();

    public static byte[] GetFile(string file)
    {
        AquireFileLock(file);

        try
        {
            if (!File.Exists($"{Path}{file}.dat"))
                return null;

            return File.ReadAllBytes($"{Path}{file}.dat");
        }
        finally
        {
            ReleaseFileLock(file);
        }
    }

    public static void DeleteFile(string file)
    {
        AquireFileLock(file);

        try
        {
            File.Delete($"{Path}{file}.dat");
        }
        catch (Exception)
        {

        }
        finally
        {
            ReleaseFileLock(file);
        }
    }

    public static void SetFile(string file, byte[] data)
    {
        AquireFileLock(file);

        try
        {
            File.WriteAllBytes($"{Path}{file}.dat", data);
        }
        finally
        {
            ReleaseFileLock(file);
        }
    }

    static void AquireFileLock(string file)
    {
        while (true)
        {
            lock (locker)
                if (!files.Contains(file))
                {
                    files.Add(file);
                    break;
                }
            Thread.Sleep(1);
        }
    }

    static void ReleaseFileLock(string file)
    {
        lock (locker)
            files.Remove(file);
    }
}
