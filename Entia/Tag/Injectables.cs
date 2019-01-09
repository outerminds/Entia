using Entia.Core;
using Entia.Dependables;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Reflection;

namespace Entia.Injectables
{
	public readonly struct AllTags : IInjectable,
		IDepend<Dependencies.Unknown, Read<Entity>, Emit<Messages.OnAdd>, Emit<Messages.OnRemove>>
	{
		sealed class Injector : Injector<AllTags>
		{
			public override Result<AllTags> Inject(MemberInfo member, World world) => new AllTags(world.Tags());
		}

		[Injector]
		static readonly Injector _injector = new Injector();

		readonly Modules.Tags _tags;

		public AllTags(Modules.Tags tags) { _tags = tags; }

		public bool Has<T>(Entity entity) where T : struct, ITag => _tags.Has<T>(entity);
		public bool Has(Entity entity, Type type) => _tags.Has(entity, type);
		public bool Set<T>(Entity entity) where T : struct, ITag => _tags.Set<T>(entity);
		public bool Set(Entity entity, Type type) => _tags.Set(entity, type);
		public bool Remove<T>(Entity entity) where T : struct, ITag => _tags.Remove<T>(entity);
		public bool Remove(Entity entity, Type type) => _tags.Remove(entity, type);
		public bool Clear<T>() where T : struct, ITag => _tags.Clear<T>();
		public bool Clear(Type type) => _tags.Clear(type);
		public bool Clear(Entity entity) => _tags.Clear(entity);
		public bool Clear() => _tags.Clear();
	}

	public readonly struct Tags<T> : IInjectable,
		IDepend<Read<Entity>, Write<T>, Emit<Messages.OnAdd>, Emit<Messages.OnRemove>, Emit<Messages.OnAdd<T>>, Emit<Messages.OnRemove<T>>>
		where T : struct, ITag
	{
		public readonly struct Read : IInjectable, IDepend<Read<Entity>, Read<T>>
		{
			sealed class Injector : Injector<Read>
			{
				public override Result<Read> Inject(MemberInfo member, World world) => new Read(world.Tags());
			}

			[Injector]
			static readonly Injector _injector = new Injector();

			readonly Modules.Tags _tags;

			public Read(Modules.Tags tags) { _tags = tags; }

			public bool Has(Entity entity) => _tags.Has<T>(entity);
		}

		sealed class Injector : Injector<Tags<T>>
		{
			public override Result<Tags<T>> Inject(MemberInfo member, World world) => new Tags<T>(world.Tags());
		}

		[Injector]
		static readonly Injector _injector = new Injector();
		public static implicit operator Read(Tags<T> tags) => new Read(tags._tags);

		readonly Modules.Tags _tags;

		public Tags(Modules.Tags tags) { _tags = tags; }

		public bool Has(Entity entity) => _tags.Has<T>(entity);
		public bool Set(Entity entity) => _tags.Set<T>(entity);
		public bool Remove(Entity entity) => _tags.Remove<T>(entity);
		public bool Clear() => _tags.Clear<T>();
	}
}
