using Entia.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules.Query
{
    public interface IQuery : IEquatable<IQuery>
    {
        Filter Filter { get; }
        Func<BitMask, bool> Fits { get; }
    }

    public readonly struct Query : IQuery, IEquatable<Query>
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

        public static readonly Query True = new Query(Filter.Empty, _ => true);
        public static readonly Query False = new Query(Filter.Empty, _ => false);

        static readonly Concurrent<Dictionary<Query[], Query>> _all = new Dictionary<Query[], Query>(new Comparer<Query>());
        static readonly Concurrent<Dictionary<Query[], Query>> _any = new Dictionary<Query[], Query>(new Comparer<Query>());
        static readonly Concurrent<Dictionary<Query, Query>> _not = new Dictionary<Query, Query>();
        static readonly Concurrent<Dictionary<Type, Query>> _from = new Dictionary<Type, Query>();

        public static Query From(params Type[] types) => All(types.Select(From).ToArray());

        public static Query From(Type type)
        {
            using (var read = _from.Read(true))
            {
                if (read.Value.TryGetValue(type, out var query)) return query;

                using (var write = _from.Write())
                {
                    var mask = IndexUtility.GetMask(type);
                    return write.Value[type] = new Query(new Filter(mask, null, type), current => current.HasAll(mask));
                }
            }
        }

        public static Query<T> All<T>(Query<T> query, params Query[] queries) where T : struct, Queryables.IQueryable =>
            new Query<T>(All(queries.Prepend(query).ToArray()), query.TryGet);

        public static Query All(params Query[] queries)
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
                            return write.Value[queries] = new Query(filter, mask => mask.HasAll(filter.All) && mask.HasNone(filter.None));
                    }
                }
            }
        }

        public static Query Any(params Query[] queries)
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
                            return write.Value[queries] = new Query(filter, mask =>
                            {
                                foreach (var query in queries) if (query.Fits(mask)) return true;
                                return false;
                            });
                    }
                }
            }
        }

        public static Query None(params Query[] queries) => Not(All(queries));

        public static Query Not(Query query)
        {
            using (var read = _not.Read(true))
            {
                if (read.Value.TryGetValue(query, out var value)) return value;
                using (var write = _not.Write()) return write.Value[query] = new Query(~query.Filter, mask => !query.Fits(mask));
            }
        }

        public static Query Maybe(Query query) => new Query(new Filter(new BitMask(), new BitMask(), query.Filter.Types), True.Fits);

        public readonly Filter Filter;
        public readonly Func<BitMask, bool> Fits;

        Filter IQuery.Filter => Filter;
        Func<BitMask, bool> IQuery.Fits => Fits;

        public Query(Filter filter, Func<BitMask, bool> fits)
        {
            Filter = filter;
            Fits = fits;
        }

        public bool Equals(Query other) => Fits == other.Fits && Filter.Equals(other.Filter);
        public bool Equals(IQuery other) => other is Query query && Equals(query);
        public override bool Equals(object obj) => obj is Query query && Equals(query);
        public override int GetHashCode() => Filter.GetHashCode() ^ Fits.GetHashCode();
    }

    public readonly struct Query<T> : IQuery where T : struct, Queryables.IQueryable
    {
        public struct ItemEnumerator : IEnumerator<T>
        {
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            Query<T> _query;
            Entities _entities;
            Entities.Enumerator _enumerator;

            public ItemEnumerator(Query<T> query, Entities entities)
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
            readonly Query<T> _query;
            readonly Entities _entities;

            public ItemEnumerable(Query<T> query, Entities entities)
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

            Query<T> _query;
            Entities _entities;
            Entities.Enumerator _enumerator;

            public EntityEnumerator(Query<T> query, Entities entities)
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
            readonly Query<T> _query;
            readonly Entities _entities;

            public EntityEnumerable(Query<T> query, Entities entities)
            {
                _query = query;
                _entities = entities;
            }

            public EntityEnumerator GetEnumerator() => new EntityEnumerator(_query, _entities);
            IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static implicit operator Query(Query<T> query) => new Query(query.Filter, query.Fits);

        public readonly Filter Filter;
        public readonly Func<BitMask, bool> Fits;
        public readonly TryFunc<Entity, T> TryGet;

        Filter IQuery.Filter => Filter;
        Func<BitMask, bool> IQuery.Fits => Fits;

        public Query(Filter filter, Func<BitMask, bool> fits, TryFunc<Entity, T> tryGet)
        {
            Filter = filter;
            Fits = fits;
            TryGet = tryGet;
        }
        public Query(Query query, TryFunc<Entity, T> tryGet)
        {
            Filter = query.Filter;
            Fits = query.Fits;
            TryGet = tryGet;
        }

        public ItemEnumerable Items(Entities entities) => new ItemEnumerable(this, entities);
        public EntityEnumerable Entities(Entities entities) => new EntityEnumerable(this, entities);
        public bool Equals(Query<T> other) => Fits == other.Fits && TryGet == other.TryGet && Filter.Equals(other.Filter);
        public bool Equals(IQuery other) => other is Query<T> query && Equals(query);
        public override bool Equals(object obj) => obj is Query<T> query && Equals(query);
        public override int GetHashCode() => Filter.GetHashCode() ^ Fits.GetHashCode() ^ TryGet.GetHashCode();
    }
}
