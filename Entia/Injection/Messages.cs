using System.Reflection;

namespace Entia.Messages
{
	public struct OnInject : IMessage
	{
		public MemberInfo Member;
		public object Value;
	}
}
