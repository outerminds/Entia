using Entia.Core;
using Entia.Modules;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Stores
{
    public sealed class Segment<T> : Store<T> where T : struct, IComponent
    {
        public override int Capacity => _values.Capacity;
        public override int Count => _values.Count;
        public override ref T this[int index] => ref _values[index];
        public override IEnumerable<(Entity entity, T value)> Pairs =>
            Entities.Select(entity =>
            {
                TryIndex(entity, out var index);
                return (entity, _values[index]);
            });
        public override IEnumerable<Entity> Entities => _keys.Where(entity => entity != Entity.Zero);
        public override IEnumerable<T> Values => _values;

        readonly int _segment;
        readonly Entities _entities;

        Entity[] _keys = new Entity[Buffer<T>.Overflow];
        readonly Buffer<T> _values = new Buffer<T>();

        public Segment(int segment, Entities entities)
        {
            _segment = segment;
            _entities = entities;
        }

        public override bool TryIndex(Entity entity, out int index)
        {
            if (_entities.TryData(entity, out var data) && data.Mask.Has(_segment) && data.Index < _keys.Length && _keys[data.Index] == entity)
            {
                index = data.Index;
                return true;
            }

            index = default;
            return false;
        }

        public override bool TryGet(Entity entity, out T[] buffer, out int index)
        {
            if (TryIndex(entity, out var current) && _values.TryGet(current, out buffer, out index)) return true;

            buffer = default;
            index = default;
            return false;
        }

        public override bool Set(Entity entity, T value)
        {
            if (_entities.TryData(entity, out var data) && data.Segment.global == _segment)
            {
                if (_values.Set(value, data.Index))
                {
                    ArrayUtility.Ensure(ref _keys, data.Index + 1);
                    _keys[data.Index] = entity;
                    return true;
                }
                else return _keys[data.Index].Change(entity);
            }

            return false;
        }

        public override bool Add(Entity entity, T value) => Set(entity, value);

        public override bool Remove(Entity entity)
        {
            if (TryIndex(entity, out var index) && _values.Remove(index))
            {
                _keys[index] = default;
                return true;
            }

            return false;
        }

        public override bool Has(Entity entity) => TryIndex(entity, out _);

        public override bool CopyTo(Entity source, Entity target, Store<T> store)
        {
            if (TryGet(source, out var buffer, out var index))
            {
                store.Set(target, buffer[index]);
                return true;
            }

            return false;
        }

        public override bool Clear()
        {
            var cleared = _values.Clear();
            _keys.Clear();
            return cleared;
        }

        public override bool Resolve() => _values.Compact();
    }
}
