using System.Collections.Generic;

namespace Entia.Core
{
	public static class QueueExtensions
	{
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
