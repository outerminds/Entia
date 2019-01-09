using Entia.Dependables;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queriers;

namespace Entia.Queryables
{
	public readonly struct Maybe<T> : IQueryable, IDepend<T> where T : struct, IQueryable
	{
		sealed class Querier : Querier<Maybe<T>>
		{
			public override Query<Maybe<T>> Query(World world)
			{
				var query = world.Queriers().Query<T>();
				return new Query<Maybe<T>>(
					Modules.Query.Query.Maybe(query),
					(Entity entity, out Maybe<T> value) =>
					{
						value = query.TryGet(entity, out var item) ? new Maybe<T>(item) : new Maybe<T>();
						return true;
					});
			}
		}

		[Querier]
		static readonly Querier _querier = new Querier();

		public readonly bool Has;
		public readonly T Value;

		public Maybe(T value)
		{
			Has = true;
			Value = value;
		}

		public static implicit operator Maybe<T>(T value) => new Maybe<T>(value);
		public static implicit operator T? (Maybe<T> maybe) => maybe.Has ? maybe.Value : (T?)null;
	}
}
