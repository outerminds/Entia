using Entia.Segments;
using System;

namespace Entia.Messages
{
	public struct OnCreate : IMessage
	{
		public Entity Entity;
		public (int global, int local, Type type) Segment;
	}

	public struct OnCreate<T> : IMessage where T : struct, ISegment { public Entity Entity; }

	public struct OnPreDestroy : IMessage
	{
		public Entity Entity;
		public (int global, int local, Type type) Segment;
	}

	public struct OnPreDestroy<T> : IMessage where T : struct, ISegment { public Entity Entity; }

	public struct OnPostDestroy : IMessage
	{
		public Entity Entity;
		public (int global, int local, Type type) Segment;
	}

	public struct OnPostDestroy<T> : IMessage where T : struct, ISegment { public Entity Entity; }
}