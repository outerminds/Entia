using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Dependencies
{
	public static class DependencyExtensions
	{
		public static IEnumerable<Type> Reads(this IEnumerable<IDependency> dependencies) => dependencies.OfType<Read>().Select(read => read.Type);
		public static IEnumerable<Type> Writes(this IEnumerable<IDependency> dependencies) => dependencies.OfType<Write>().Select(write => write.Type);
		public static IEnumerable<Type> Emits(this IEnumerable<IDependency> dependencies) => dependencies.OfType<Emit>().Select(emit => emit.Type);
		public static IEnumerable<Type> Reacts(this IEnumerable<IDependency> dependencies) => dependencies.OfType<React>().Select(react => react.Type);
	}
}
