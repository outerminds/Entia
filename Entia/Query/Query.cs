using System;
using System.Collections.Generic;
using System.Linq;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Modules.Component;

namespace Entia.Modules.Query
{
    [ThreadSafe]
    public readonly struct Query
    {
        public static readonly Query Empty = new Query((pointer, _) => pointer);

        public readonly Func<IntPtr, int, IntPtr> Fill;
        public readonly Metadata[] Types;

        public Query(Func<IntPtr, int, IntPtr> fill, params Metadata[] types)
        {
            Fill = fill;
            Types = types;
        }
    }

    [ThreadSafe]
    public readonly struct Query<T> where T : struct, Queryables.IQueryable
    {
        public static implicit operator Query(Query<T> query) => new Query(
            (pointer, index) =>
            {
                UnsafeUtility.ToReference<T>(pointer) = query.Get(index);
                return pointer + UnsafeUtility.Cache<T>.Size;
            },
            query.Types);

        public readonly Func<int, T> Get;
        public readonly Metadata[] Types;

        public Query(Func<int, T> get, params Metadata[] types)
        {
            Get = get;
            Types = types;
        }

        public Query(Func<int, T> get) : this(get, Array.Empty<Metadata>()) { }
        public Query(Func<int, T> get, params Metadata[][] types) : this(get, types.SelectMany(_ => _).ToArray()) { }
        public Query(Func<int, T> get, IEnumerable<Metadata> types) : this(get, types.ToArray()) { }
    }
}