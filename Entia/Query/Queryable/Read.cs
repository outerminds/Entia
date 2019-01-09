using Entia.Core;
using Entia.Dependables;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queriers;

namespace Entia.Queryables
{
	public readonly struct Read<T> : IQueryable, IDepend<Dependables.Read<T>> where T : struct, IComponent
	{
		sealed class Querier : Querier<Read<T>>
		{
			public override Query<Read<T>> Query(World world)
			{
				var mask = IndexUtility<IComponent>.Cache<T>.Mask;
				return new Query<Read<T>>(new Filter(mask, null, typeof(T)), current => current.HasAll(mask), world.Components().TryRead);
			}
		}

		[Querier]
		static readonly Querier _querier = new Querier();

		public ref readonly T Value => ref _array[_index];

		readonly T[] _array;
		readonly int _index;

		public Read(T[] array, int index)
		{
			_array = array;
			_index = index;
		}
	}
}
