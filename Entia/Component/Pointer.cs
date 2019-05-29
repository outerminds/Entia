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

namespace Entia.Modules.Component
{
    [ThreadSafe]
    public readonly struct Pointer<T> : IQueryable, IDependable where T : struct, IComponent
    {
        unsafe sealed class Querier : IQuerier
        {
            public bool TryQuery(in Context context, out Query.Query query)
            {
                if (ComponentUtility.TryGetMetadata<T>(false, out var metadata))
                {
                    var segment = context.Segment;
                    var state = context.World.Components().State(segment.Mask, metadata);
                    if (context.Include.HasAny(state))
                    {
                        query = metadata.Kind == Metadata.Kinds.Tag ?
                            new Query.Query((pointer, _) =>
                            {
                                var target = (IntPtr*)pointer;
                                *target = UnsafeUtility.AsPointer(ref Dummy<T>.Value);
                            }, metadata) :
                            new Query.Query((pointer, index) =>
                            {
                                var store = segment.Fixed(metadata).store as T[];
                                var target = (IntPtr*)pointer;
                                *target = UnsafeUtility.AsPointer(ref store[index]);
                            }, metadata);
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
    }
}
