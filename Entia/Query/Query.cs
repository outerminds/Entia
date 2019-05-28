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
        public static readonly Query Empty = new Query((_, __) => { });

        public readonly Action<IntPtr, int> Fill;
        public readonly Metadata[] Types;

        public Query(Action<IntPtr, int> fill, params Metadata[] types)
        {
            Fill = fill;
            Types = types;
        }
    }

    [ThreadSafe]
    public readonly struct Query<T> where T : struct, Queryables.IQueryable
    {
        public static implicit operator Query(Query<T> query) => new Query(
            (pointer, index) => UnsafeUtility.As<T>(pointer) = query.Get(index),
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