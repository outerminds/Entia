using System;
using System.Collections.Generic;
using System.Linq;
using Entia.Core;
using Entia.Modules.Component;

namespace Entia.Modules.Query
{
    public readonly struct Query2<T> where T : struct, Queryables.IQueryable
    {
        public readonly Func<Box<int>, T> Get;
        public readonly Metadata[] Types;

        public Query2(Func<Box<int>, T> get, Metadata[] types)
        {
            Get = get;
            Types = types;
        }

        public Query2(Func<Box<int>, T> get, params Metadata[][] types) : this(get, types.SelectMany(_ => _).ToArray()) { }
        public Query2(Func<Box<int>, T> get, IEnumerable<Metadata> types) : this(get, types.ToArray()) { }
    }
}