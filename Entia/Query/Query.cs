using System;
using System.Collections.Generic;
using System.Linq;
using Entia.Core;
using Entia.Modules.Component;

namespace Entia.Modules.Query
{
    public readonly struct Query<T> where T : struct, Queryables.IQueryable
    {
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