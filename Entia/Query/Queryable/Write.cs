using Entia.Core;
using Entia.Dependables;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queriers;

namespace Entia.Queryables
{
	public readonly struct Write<T> : IQueryable, IDepend<Dependables.Write<T>> where T : struct, IComponent
	{
		sealed class Querier : Querier<Write<T>>
		{
			public override Query<Write<T>> Query(World world)
			{
				var mask = IndexUtility<IComponent>.Cache<T>.Mask;
				return new Query<Write<T>>(new Filter(mask, null, typeof(T)), current => current.HasAll(mask), world.Components().TryWrite);
			}
		}

		[Querier]
		static readonly Querier _querier = new Querier();

		public ref T Value => ref _array[_index];

		readonly T[] _array;
		readonly int _index;

		public Write(T[] array, int index)
		{
			_array = array;
			_index = index;
		}

		public static implicit operator Read<T>(Write<T> write) => new Read<T>(write._array, write._index);
	}
}
