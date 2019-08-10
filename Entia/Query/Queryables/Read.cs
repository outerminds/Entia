using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Modules;
using Entia.Modules.Query;
using Entia.Queriers;

namespace Entia.Queryables
{
    [ThreadSafe]
    public readonly struct Read<T> : IQueryable where T : struct, IComponent
    {
        sealed class Querier : Querier<Read<T>>
        {
            public override bool TryQuery(in Context context, out Query<Read<T>> query)
            {
                if (context.World.Queriers().TryQuery<Write<T>>(context, out var write))
                {
                    query = new Query<Read<T>>(index => write.Get(index), write.Types);
                    return true;
                }

                query = default;
                return false;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Implementation]
        static readonly IDepender _depender = Depender.From<T>(new Read(typeof(T)));

        public ref readonly T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[_index];
        }
        public readonly States State;

        readonly int _index;
        readonly T[] _array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Read(int index, T[] array, States state)
        {
            _index = index;
            _array = array;
            State = state;
        }
    }
}
