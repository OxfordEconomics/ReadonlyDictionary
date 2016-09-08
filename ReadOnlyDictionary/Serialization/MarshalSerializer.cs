﻿using ProtoBuf;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ReadonlyDictionary.Serialization
{
    // http://stackoverflow.com/questions/3278827/how-to-convert-a-structure-to-a-byte-array-in-c
    public class MarshalSerializer<T> : ISerializer<T> where T: struct
    {
        public byte[] Serialize(T value)
        {
            return StructureToByteArray(value);
        }

        public T Deserialize(byte[] bytes)
        {
            object value = default(T);
            ByteArrayToStructure(bytes, ref value);
            return (T)value;
        }

        private byte[] StructureToByteArray(object obj)
        {
            int len = Marshal.SizeOf(obj);

            byte[] arr = new byte[len];

            IntPtr ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        private void ByteArrayToStructure(byte[] bytearray, ref object obj)
        {
            int len = Marshal.SizeOf(obj);

            IntPtr i = Marshal.AllocHGlobal(len);

            Marshal.Copy(bytearray, 0, i, len);

            obj = Marshal.PtrToStructure(i, obj.GetType());

            Marshal.FreeHGlobal(i);
        }

        public object GetState()
        {
            return null;
        }
    }
}
