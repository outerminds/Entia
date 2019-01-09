using Entia.Core;
using Entia.Modules;
using Entia.Segments;
using System;

namespace Entia.Stores.Factories
{
	public interface IFactory
	{
		Type Type { get; }
		IStore Create(Type segment, World world);
	}

	public abstract class Factory<T> : IFactory where T : struct, IComponent
	{
		Type IFactory.Type => typeof(T);

		public abstract Store<T> Create(Type segment, World world);
		IStore IFactory.Create(Type segment, World world) => Create(segment, world);
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class FactoryAttribute : PreserveAttribute { }

	public sealed class Default<T> : Factory<T> where T : struct, IComponent
	{
		public override Store<T> Create(Type segment, World world)
		{
			if (segment != typeof(Default) && IndexUtility<ISegment>.TryGetIndex(segment, out var index))
				return new Segment<T>(index.global, world.Entities());
			return new Stores.Default<T>();
		}
	}

	public sealed class Indexed<T> : Factory<T> where T : struct, IComponent
	{
		public override Store<T> Create(Type segment, World world)
		{
			if (segment == typeof(Default)) return new Stores.Indexed<T>();
			return world.Stores().Store<T>(typeof(Default));
		}
	}
}