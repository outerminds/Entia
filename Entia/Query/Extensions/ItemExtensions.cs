using Entia.Core;
using Entia.Core.Documentation;

namespace Entia.Queryables
{
    [ThreadSafe]
    public static partial class ItemExtensions
    {
        public static bool TryGet<T>(in this Maybe<T> item, out T value) where T : struct, IQueryable
        {
            value = item.Value;
            return item.Has;
        }
        public static ref T Get<T>(in this Maybe<Write<T>> item, out bool success) where T : struct, IComponent => ref (success = item.Has) ? ref item.Value.Value : ref Dummy<T>.Value;
        public static ref readonly T Get<T>(in this Maybe<Read<T>> item, out bool success) where T : struct, IComponent => ref (success = item.Has) ? ref item.Value.Value : ref Dummy<T>.Read.Value;

        public static void Deconstruct<T>(in this Read<T> item, out T value) where T : struct, IComponent => value = item.Value;
        public static void Deconstruct<T>(in this Write<T> item, out T value) where T : struct, IComponent => value = item.Value;
    }
}