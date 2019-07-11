using System;
using System.Runtime.InteropServices;
using Entia.Core;

namespace Entia.Modules.Serialization
{
    public sealed unsafe class Writer : IDisposable
    {
        public ref int Position => ref _bytes.count;
        public int Capacity => _bytes.items.Length;

        (byte[] items, int count) _bytes;
        GCHandle _handle;
        byte* _pointer;

        public Writer(int capacity = 64)
        {
            _bytes = (new byte[capacity], 0);
            _bytes.items.Fix(out _handle, out _pointer);
        }

        public void Write(string value)
        {
            Write(value.Length);
            fixed (char* pointer = value) Write(pointer, value.Length);
        }

        public void Write<T>(T value) where T : unmanaged => Write((byte*)&value, sizeof(T));
        public void Write<T>(params T[] values) where T : unmanaged => Write(values, 0, values.Length);
        public void Write<T>(T[] values, int count) where T : unmanaged => Write(values, 0, count);
        public void Write<T>(T[] values, int start, int count) where T : unmanaged
        {
            fixed (T* pointer = values) Write(pointer + start, count);
        }

        public void Write<T>(T* pointer, int count) where T : unmanaged => Write((byte*)pointer, count * sizeof(T));
        public void Write(IntPtr pointer, int size, int count) => Write((byte*)pointer, size * count);
        public void Write(IntPtr bytes, int count) => Write((byte*)bytes, count);
        public void Write(byte* bytes, int count)
        {
            if (count <= 0) return;

            var position = _bytes.count;
            _bytes.count += count;
            if (_bytes.Ensure())
            {
                _handle.Free();
                _bytes.items.Fix(out _handle, out _pointer);
            }

            Buffer.MemoryCopy(bytes, _pointer + position, count, count);
        }

        public byte[] ToArray() => _bytes.ToArray();

        public void Dispose() => _handle.Free();
    }
}