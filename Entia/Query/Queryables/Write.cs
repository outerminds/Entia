using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;

namespace Entia.Queryables
{
    [ThreadSafe]
    public readonly struct Write<T> : IQueryable where T : struct, IComponent
    {
        sealed class Querier : Querier<Write<T>>
        {
            public override bool TryQuery(in Context context, out Query<Write<T>> query)
            {
                if (ComponentUtility.TryGetMetadata<T>(false, out var metadata))
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

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                yield return new Write(typeof(T));
                foreach (var dependency in world.Dependers().Dependencies<T>()) yield return dependency;
            }
        }

        [Querier]
        static readonly Querier _querier = new Querier();
        [Depender]
        static readonly Depender _depender = new Depender();

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
