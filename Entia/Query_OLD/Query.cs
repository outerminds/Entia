using Entia.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules.Query
{
    public interface IQuery_OLD : IEquatable<IQuery_OLD>
    {
        Filter Filter { get; }
        Func<BitMask, bool> Fits { get; }
    }

    public readonly struct Query_OLD : IQuery_OLD, IEquatable<Query_OLD>
    {
        sealed class Comparer<T> : IEqualityComparer<T[]>
        {
            public bool Equals(T[] x, T[] y)
            {
                if (x.Length != y.Length) return false;
                for (var i = 0; i < x.Length; i++)
                    if (!EqualityComparer<T>.Default.Equals(x[i], y[i])) return false;
                return true;
            }

            public int GetHashCode(T[] obj)
            {
                var hash = 0;
                for (var i = 0; i < obj.Length; i++)
                    hash ^= EqualityComparer<T>.Default.GetHashCode(obj[i]);
                return hash;
            }
        }

        public static readonly Query_OLD True = new Query_OLD(Filter.Empty, _ => true);
        public static readonly Query_OLD False = new Query_OLD(Filter.Empty, _ => false);

        static readonly Concurrent<Dictionary<Query_OLD[], Query_OLD>> _all = new Dictionary<Query_OLD[], Query_OLD>(new Comparer<Query_OLD>());
        static readonly Concurrent<Dictionary<Query_OLD[], Query_OLD>> _any = new Dictionary<Query_OLD[], Query_OLD>(new Comparer<Query_OLD>());
        static readonly Concurrent<Dictionary<Query_OLD, Query_OLD>> _not = new Dictionary<Query_OLD, Query_OLD>();
        static readonly Concurrent<Dictionary<Type, Query_OLD>> _from = new Dictionary<Type, Query_OLD>();

        public static Query_OLD From(params Type[] types) => All(types.Select(From).ToArray());

        public static Query_OLD From(Type type)
        {
            using (var read = _from.Read(true))
            {
                if (read.Value.TryGetValue(type, out var query)) return query;

                using (var write = _from.Write())
                {
                    var mask = IndexUtility.GetMask(type);
                    return write.Value[type] = new Query_OLD(new Filter(mask, null, type), current => current.HasAll(mask));
                }
            }
        }

        public static Query_OLD<T> All<T>(Query_OLD<T> query, params Query_OLD[] queries) where T : struct, Queryables.IQueryable =>
            new Query_OLD<T>(All(queries.Prepend(query).ToArray()), query.TryGet);

        public static Query_OLD All(params Query_OLD[] queries)
        {
            queries = queries.Distinct().OrderBy(query => query.GetHashCode()).ToArray();

            using (var read = _all.Read(true))
            {
                if (read.Value.TryGetValue(queries, out var value)) return value;

                using (var write = _all.Write())
                {
                    switch (queries.Length)
                    {
                        case 0: return write.Value[queries] = True;
                        case 1: return write.Value[queries] = queries[0];
                        default:
                            var filter = queries.Aggregate(Filter.Empty, (current, query) => current | query.Filter);
                            return write.Value[queries] = new Query_OLD(filter, mask => mask.HasAll(filter.All) && mask.HasNone(filter.None));
                    }
                }
            }
        }

        public static Query_OLD Any(params Query_OLD[] queries)
        {
            queries = queries.Distinct().OrderBy(query => query.GetHashCode()).ToArray();

            using (var read = _any.Read(true))
            {
                if (read.Value.TryGetValue(queries, out var value)) return value;

                using (var write = _any.Write())
                {
                    switch (queries.Length)
                    {
                        case 0: return write.Value[queries] = False;
                        case 1: return write.Value[queries] = queries[0];
                        default:
                            var filter = queries.Skip(1).Aggregate(queries[0].Filter, (current, query) => current & query.Filter);
                            return write.Value[queries] = new Query_OLD(filter, mask =>
                            {
                                foreach (var query in queries) if (query.Fits(mask)) return true;
                                return false;
                            });
                    }
                }
            }
        }

        public static Query_OLD None(params Query_OLD[] queries) => Not(All(queries));

        public static Query_OLD Not(Query_OLD query)
        {
            using (var read = _not.Read(true))
            {
                if (read.Value.TryGetValue(query, out var value)) return value;
                using (var write = _not.Write()) return write.Value[query] = new Query_OLD(~query.Filter, mask => !query.Fits(mask));
            }
        }

        public static Query_OLD Maybe(Query_OLD query) => new Query_OLD(new Filter(new BitMask(), new BitMask(), query.Filter.Types), True.Fits);

        public readonly Filter Filter;
        public readonly Func<BitMask, bool> Fits;

        Filter IQuery_OLD.Filter => Filter;
        Func<BitMask, bool> IQuery_OLD.Fits => Fits;

        public Query_OLD(Filter filter, Func<BitMask, bool> fits)
        {
            Filter = filter;
            Fits = fits;
        }

        public bool Equals(Query_OLD other) => Fits == other.Fits && Filter.Equals(other.Filter);
        public bool Equals(IQuery_OLD other) => other is Query_OLD query && Equals(query);
        public override bool Equals(object obj) => obj is Query_OLD query && Equals(query);
        public override int GetHashCode() => Filter.GetHashCode() ^ Fits.GetHashCode();
    }

    public readonly struct Query_OLD<T> : IQuery_OLD where T : struct, Queryables.IQueryable
    {
        public struct ItemEnumerator : IEnumerator<T>
        {
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            Query_OLD<T> _query;
            Entities _entities;
            Entities.Enumerator _enumerator;

            public ItemEnumerator(Query_OLD<T> query, Entities entities)
            {
                _query = query;
                _entities = entities;
                _enumerator = entities.GetEnumerator();
                Current = default;
            }

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    var entity = _enumerator.Current;
                    if (_entities.TryMask(entity, out var mask) && _query.Fits(mask) && _query.TryGet(entity, out var item))
                    {
                        Current = item;
                        return true;
                    }
                }

                return false;
            }
            public void Reset() => _enumerator.Reset();
            public void Dispose() => _enumerator.Dispose();
        }

        public readonly struct ItemEnumerable : IEnumerable<T>
        {
            readonly Query_OLD<T> _query;
            readonly Entities _entities;

            public ItemEnumerable(Query_OLD<T> query, Entities entities)
            {
                _query = query;
                _entities = entities;
            }

            public ItemEnumerator GetEnumerator() => new ItemEnumerator(_query, _entities);
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public struct EntityEnumerator : IEnumerator<Entity>
        {
            public Entity Current => _enumerator.Current;
            object IEnumerator.Current => Current;

            Query_OLD<T> _query;
            Entities _entities;
            Entities.Enumerator _enumerator;

            public EntityEnumerator(Query_OLD<T> query, Entities entities)
            {
                _query = query;
                _entities = entities;
                _enumerator = entities.GetEnumerator();
            }

            public bool MoveNext()
            {
                while (_enumerator.MoveNext())
                {
                    var entity = _enumerator.Current;
                    if (_entities.TryMask(entity, out var mask) && _query.Fits(mask)) return true;
                }

                return false;
            }
            public void Reset() => _enumerator.Reset();
            public void Dispose() => _enumerator.Dispose();
        }

        public readonly struct EntityEnumerable : IEnumerable<Entity>
        {
            readonly Query_OLD<T> _query;
            readonly Entities _entities;

            public EntityEnumerable(Query_OLD<T> query, Entities entities)
            {
                _query = query;
                _entities = entities;
            }

            public EntityEnumerator GetEnumerator() => new EntityEnumerator(_query, _entities);
            IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static implicit operator Query_OLD(Query_OLD<T> query) => new Query_OLD(query.Filter, query.Fits);

        public readonly Filter Filter;
        public readonly Func<BitMask, bool> Fits;
        public readonly TryFunc<Entity, T> TryGet;

        Filter IQuery_OLD.Filter => Filter;
        Func<BitMask, bool> IQuery_OLD.Fits => Fits;

        public Query_OLD(Filter filter, Func<BitMask, bool> fits, TryFunc<Entity, T> tryGet)
        {
            Filter = filter;
            Fits = fits;
            TryGet = tryGet;
        }
        public Query_OLD(Query_OLD query, TryFunc<Entity, T> tryGet)
        {
            Filter = query.Filter;
            Fits = query.Fits;
            TryGet = tryGet;
        }

        public ItemEnumerable Items(Entities entities) => new ItemEnumerable(this, entities);
        public EntityEnumerable Entities(Entities entities) => new EntityEnumerable(this, entities);
        public bool Equals(Query_OLD<T> other) => Fits == other.Fits && TryGet == other.TryGet && Filter.Equals(other.Filter);
        public bool Equals(IQuery_OLD other) => other is Query_OLD<T> query && Equals(query);
        public override bool Equals(object obj) => obj is Query_OLD<T> query && Equals(query);
        public override int GetHashCode() => Filter.GetHashCode() ^ Fits.GetHashCode() ^ TryGet.GetHashCode();
    }
}
