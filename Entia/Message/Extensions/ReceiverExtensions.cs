using System.Collections.Generic;

namespace Entia.Modules.Message
{
	public static class ReceiverExtensions
	{
		public static IEnumerable<T> Pop<T>(this Receiver<T> receiver)
			where T : struct, IMessage
		{
			while (receiver.TryPop(out var message)) yield return message;
		}
	}
}
