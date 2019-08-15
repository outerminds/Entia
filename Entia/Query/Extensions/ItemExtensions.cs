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
        public static ref T Get<T>(in this Maybe<Write<T>> item, out bool success, out States state) where T : struct, IComponent
        {
            if (success = item.Has) return ref item.Value.Get(out state);
            state = default;
            return ref Dummy<T>.Value;
        }
        public static ref readonly T Get<T>(in this Maybe<Read<T>> item, out bool success, out States state) where T : struct, IComponent
        {
            if (success = item.Has) return ref item.Value.Get(out state);
            state = default;
            return ref Dummy<T>.Value;
        }
        public static ref T Get<T>(in this Write<T> item, out States state) where T : struct, IComponent
        {
            state = item.State;
            return ref item.Value;
        }
        public static ref readonly T Get<T>(in this Read<T> item, out States state) where T : struct, IComponent
        {
            state = item.State;
            return ref item.Value;
        }

        public static void Deconstruct<T>(in this Read<T> item, out T value) where T : struct, IComponent => value = item.Value;
        public static void Deconstruct<T>(in this Write<T> item, out T value) where T : struct, IComponent => value = item.Value;
        public static void Deconstruct<T>(in this Read<T> item, out T value, out States state) where T : struct, IComponent
        {
            value = item.Value;
            state = item.State;
        }
        public static void Deconstruct<T>(in this Write<T> item, out T value, out States state) where T : struct, IComponent
        {
            value = item.Value;
            state = item.State;
        }
    }
}