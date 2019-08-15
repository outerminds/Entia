using System;
using System.Runtime.InteropServices;

namespace Entia.Serialization
{
    public unsafe sealed class Reader : IDisposable
    {
        public readonly byte[] Bytes;
        public int Position;
        public int Remaining => Bytes.Length - Position;

        readonly GCHandle _handle;
        readonly byte* _pointer;

        public Reader(byte[] bytes)
        {
            Bytes = bytes;
            Bytes.Fix(out _handle, out _pointer);
        }

        public bool Read(out string value)
        {
            if (Read(out int count))
            {
                var pointer = stackalloc char[count];
                if (Read(pointer, count))
                {
                    value = new string(pointer, 0, count);
                    return true;
                }
            }
            value = default;
            return false;
        }

        public bool Read(out void* pointer)
        {
            if (Read(out IntPtr value))
            {
                pointer = (void*)value;
                return true;
            }
            pointer = default;
            return false;
        }

        public bool Read<T>(out T value) where T : unmanaged
        {
            var pointer = stackalloc T[1];
            if (Read(pointer, 1))
            {
                value = *pointer;
                return true;
            }
            value = default;
            return false;
        }

        public bool Read<T>(out T[] values, int count) where T : unmanaged
        {
            values = new T[count];
            fixed (T* pointer = values) return Read(pointer, count);
        }

        public bool Read<T>(T* pointer, int count) where T : unmanaged => Read((byte*)pointer, count * sizeof(T));
        public bool Read(IntPtr pointer, int size, int count) => Read((byte*)pointer, size * count);
        public bool Read(IntPtr bytes, int count) => Read((byte*)bytes, count);
        public bool Read(byte* bytes, int count)
        {
            if (count <= 0) return true;
            else if (count <= Remaining)
            {
                Buffer.MemoryCopy(_pointer + Position, bytes, count, count);
                Position += count;
                return true;
            }
            else return false;
        }

        public void Dispose() => _handle.Free();
    }
}