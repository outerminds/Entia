using System;
using System.Runtime.InteropServices;

namespace Entia.Core
{
    public readonly struct Pin<T> : IDisposable where T : class
    {
        public static implicit operator Pin<T>(in T value) => new Pin<T>(value);

        public readonly T Value;
        public readonly IntPtr Pointer;
        readonly GCHandle _handle;

        public Pin(in T value)
        {
            Value = value;
            _handle = GCHandle.Alloc(value, GCHandleType.Pinned);
            Pointer = _handle.AddrOfPinnedObject();
        }

        public void Dispose() => _handle.Free();
    }
}
