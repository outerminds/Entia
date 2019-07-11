using System.Runtime.InteropServices;

namespace Entia.Modules.Serialization
{
    public static class SerializationUtility
    {
        public static unsafe void Fix(this byte[] bytes, out GCHandle handle, out byte* pointer)
        {
            handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            pointer = (byte*)handle.AddrOfPinnedObject();
        }
    }
}