using Entia.Core;
using System;

namespace Entia.Modules
{
	public static class ExceptionUtility
	{
		public static Exception MissingComponent(Entity entity, Type type) =>
			new InvalidOperationException($"Missing component of type '{type.Format()}' on entity '{entity}'. Returning a dummy reference instead.");
	}
}
