using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CashlessImage
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HeaderStruct
    {
        public int DataLength;
        public int BitsPerPixel;

        public byte[] ToBytes()
        {
            int len = Marshal.SizeOf(this);
            byte[] arr = new byte[len];
            IntPtr ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static HeaderStruct FromBytes(byte[] bytearray)
        {
            HeaderStruct obj = new HeaderStruct();
            int len = Marshal.SizeOf(obj);
            IntPtr i = Marshal.AllocHGlobal(len);
            Marshal.Copy(bytearray, 0, i, len);
            obj = (HeaderStruct) Marshal.PtrToStructure(i, obj.GetType());
            Marshal.FreeHGlobal(i);
            return obj;
        }
    }
}
