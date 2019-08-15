using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;

namespace Entia.Queryables
{
    [ThreadSafe]
    public readonly struct Write<T> : IQueryable where T : struct, IComponent
    {
        sealed class Querier : Querier<Write<T>>
        {
            public override bool TryQuery(in Context context, out Query<Write<T>> query)
            {
                if (ComponentUtility.Abstract<T>.TryConcrete(out var metadata))
                {
                    var segment = context.Segment;
                    var state = context.World.Components().State(segment.Mask, metadata);
                    if (context.Include.HasAny(state))
                    {
                        query = metadata.Kind == Metadata.Kinds.Tag ?
                            new Query<Write<T>>(_ => new Write<T>(0, Dummy<T>.Array.One, state)) :
                            new Query<Write<T>>(index => new Write<T>(index, segment.Store(metadata) as T[], state), metadata);
                        return true;
                    }
                }

                query = default;
                return false;
            }
        }

        public static implicit operator Read<T>(in Write<T> write) => new Read<T>(write._index, write._array, write.State);

        [Querier]
        static Querier _querier => new Querier();
        [Implementation]
        static IDepender _depender => Depender.From<T>(new Write(typeof(T)));

        public ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[_index];
        }
        public readonly States State;

        readonly int _index;
        readonly T[] _array;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Write(int index, T[] array, States state)
        {
            _index = index;
            _array = array;
            State = state;
        }
    }
}
