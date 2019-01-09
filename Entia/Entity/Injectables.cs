using Entia.Core;
using Entia.Dependables;
using Entia.Injectors;
using Entia.Modules;
using Entia.Segments;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
	public readonly struct AllEntities : IInjectable, IEnumerable<Entity>,
		IDepend<Write<Entity>, Emit<Messages.OnCreate>, Emit<Messages.OnPreDestroy>, Emit<Messages.OnPostDestroy>, Emit<Messages.OnAdd>, Emit<Messages.OnRemove>>
	{
		public readonly struct Read : IInjectable, IEnumerable<Entity>, IDepend<Read<Entity>>
		{
			sealed class Injector : Injector<Read>
			{
				public override Result<Read> Inject(MemberInfo member, World world) => new Read(world.Entities());
			}

			[Injector]
			static readonly Injector _injector = new Injector();

			public int Count => _entities.Count();

			readonly Entities _entities;

			public Read(Entities entities) { _entities = entities; }

			public bool Has(Entity entity) => _entities.Has(entity);
			public bool Has<T>(Entity entity) where T : struct, ISegment => _entities.Has<T>(entity);
			public bool Has(Entity entity, Type segment) => _entities.Has(entity, segment);
			public Entities.SegmentEnumerable Get<T>() where T : struct, ISegment => _entities.Get<T>();
			public Entities.SegmentEnumerable Get(Type segment) => _entities.Get(segment);

			public Entities.Enumerator GetEnumerator() => _entities.GetEnumerator();
			IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		sealed class Injector : Injector<AllEntities>
		{
			public override Result<AllEntities> Inject(MemberInfo member, World world) => new AllEntities(world.Entities());
		}

		[Injector]
		static readonly Injector _injector = new Injector();

		public static implicit operator Read(AllEntities entities) => new Read(entities._entities);

		public int Count => _entities.Count();

		readonly Entities _entities;

		public AllEntities(Entities entities) { _entities = entities; }

		public Entity Create() => _entities.Create();
		public Entity Create<T>() where T : struct, ISegment => _entities.Create<T>();
		public Entity Create(Type segment) => _entities.Create(segment);
		public bool Destroy(Entity entity) => _entities.Destroy(entity);
		public bool Destroy<T>(Entity entity) where T : struct, ISegment => _entities.Destroy<T>(entity);
		public bool Destroy(Entity entity, Type segment) => _entities.Destroy(entity, segment);
		public Entities.SegmentEnumerable Get<T>() where T : struct, ISegment => _entities.Get<T>();
		public Entities.SegmentEnumerable Get(Type segment) => _entities.Get(segment);
		public bool Has(Entity entity) => _entities.Has(entity);
		public bool Has<T>(Entity entity) where T : struct, ISegment => _entities.Has<T>(entity);
		public bool Has(Entity entity, Type segment) => _entities.Has(entity, segment);
		public bool Clear() => _entities.Clear();
		public bool Clear(Type segment) => _entities.Clear(segment);
		public bool Clear<T>() where T : struct, ISegment => _entities.Clear<T>();

		public Entities.Enumerator GetEnumerator() => _entities.GetEnumerator();
		IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	public readonly struct Entities<T> : IInjectable, IEnumerable<Entity>,
		IDepend<Write<Entity>, Emit<Messages.OnAdd>, Emit<Messages.OnRemove>>,
		IDepend<Emit<Messages.OnCreate>, Emit<Messages.OnPreDestroy>, Emit<Messages.OnPostDestroy>>,
		IDepend<Emit<Messages.OnCreate<T>>, Emit<Messages.OnPreDestroy<T>>, Emit<Messages.OnPostDestroy<T>>>
		where T : struct, ISegment
	{
		public readonly struct Read : IInjectable, IEnumerable<Entity>, IDepend<Read<Entity>>
		{
			sealed class Injector : Injector<Read>
			{
				public override Result<Read> Inject(MemberInfo member, World world) => new Read(world.Entities());
			}

			[Injector]
			static readonly Injector _injector = new Injector();

			public int Count => _entities.Count<T>();

			readonly Entities _entities;

			public Read(Entities entities) { _entities = entities; }

			public bool Has(Entity entity) => _entities.Has<T>(entity);

			public Entities.SegmentEnumerator GetEnumerator() => _entities.Get<T>().GetEnumerator();
			IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		sealed class Injector : Injector<Entities<T>>
		{
			public override Result<Entities<T>> Inject(MemberInfo member, World world) => new Entities<T>(world.Entities());
		}

		[Injector]
		static readonly Injector _injector = new Injector();

		public static implicit operator Read(Entities<T> entities) => new Read(entities._entities);

		public int Count => _entities.Count<T>();

		readonly Entities _entities;

		public Entities(Entities entities) { _entities = entities; }

		public Entity Create() => _entities.Create<T>();
		public bool Destroy(Entity entity) => _entities.Destroy<T>(entity);
		public bool Has(Entity entity) => _entities.Has<T>(entity);
		public bool Clear() => _entities.Clear<T>();

		public Entities.SegmentEnumerator GetEnumerator() => _entities.Get<T>().GetEnumerator();
		IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
