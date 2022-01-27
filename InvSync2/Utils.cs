using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InvSync;
static class Utils
{
    public static unsafe T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
        fixed (byte* ptr = bytes)
        {
            return (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
        }
    }
    public static unsafe byte[] StructureToByteArray<T>(T value) where T : struct
    {
        byte[] result = new byte[Marshal.SizeOf(value)];

        fixed (byte* ptr = result)
        {
            Marshal.StructureToPtr(value, (IntPtr)ptr, false);
        }

        return result;
    }

    public static void ReturnBuf<T>(ref T[] buf, ArrayPool<T> pool)
    {
        pool.Return(buf);
        buf = null;
    }
}
