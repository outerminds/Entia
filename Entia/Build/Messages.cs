using Entia.Nodes;
using System;

namespace Entia.Messages
{
	public struct OnProfile : IMessage { public Node Node; public TimeSpan Elapsed; }
}
