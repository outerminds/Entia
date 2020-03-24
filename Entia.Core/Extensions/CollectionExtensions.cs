using System.Collections.Generic;

namespace Entia.Core
{
    public static class CollectionExtensions
    {
        public static bool TryPop<T>(this Stack<T> stack, out T value)
        {
            if (stack.Count > 0)
            {
                value = stack.Pop();
                return true;
            }
            value = default;
            return false;
        }

        public static bool TryDequeue<T>(this Queue<T> queue, out T value)
        {
            if (queue.Count > 0)
            {
                value = queue.Dequeue();
                return true;
            }
            value = default;
            return false;
        }
    }
}