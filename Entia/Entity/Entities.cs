using Entia.Core;
using Entia.Messages;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    public sealed class Entities : IModule, IResolvable, IEnumerable<Entity>
    {
        public struct Enumerator : IEnumerator<Entity>
        {
            public Entity Current => new Entity(_index, _entities._data.items[_index].Generation);
            object IEnumerator.Current => Current;

            Entities _entities;
            int _index;

            public Enumerator(Entities entities)
            {
                _entities = entities;
                _index = -1;
            }

            public bool MoveNext()
            {
                while (++_index < _entities._data.count)
                    if (_entities._data.items[_index].Allocated) return true;

                return false;
            }
            public void Reset() => _index = -1;
            public void Dispose() => _entities = null;
        }

        struct Data
        {
            public uint Generation;
            public bool Allocated;
        }

        public int Capacity => _data.items.Length;
        public int Count => _data.count - _free.count - _frozen.count;

        readonly Messages _messages;
        (int[] items, int count) _free = (new int[8], 0);
        (int[] items, int count) _frozen = (new int[8], 0);
        (Data[] items, int count) _data = (new Data[64], 0);

        public Entities(Messages messages) { _messages = messages; }

        public Entity Create()
        {
            var reserved = ReserveIndex();
            ref var data = ref _data.items[reserved];
            var entity = new Entity(reserved, ++data.Generation);
            data.Allocated = true;
            _messages.Emit(new OnCreate { Entity = entity });
            return entity;
        }

        public bool Destroy(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                Destroy(entity, ref data);
                return true;
            }

            return false;
        }

        public bool Has(Entity entity)
        {
            GetData(entity, out var success);
            return success;
        }

        public bool Clear()
        {
            var cleared = _free.count > 0 || _frozen.count > 0 || _data.count > 0;
            for (int i = 0; i < _data.count; i++)
            {
                ref var data = ref _data.items[i];
                if (data.Allocated) Destroy(new Entity(i, data.Generation), ref data);
            }
            // NOTE: do not clear '_data' such that the generation counters are not lost; this prevents collisions if a reference to an old entity was kept
            return _free.Clear() | _frozen.Clear() | cleared;
        }

        public void Resolve()
        {
            while (_frozen.count > 0) _free.Push(_frozen.Pop());
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        ref Data GetData(Entity entity, out bool success)
        {
            if (entity.Index < _data.count)
            {
                ref var data = ref _data.items[entity.Index];
                success = data.Allocated && data.Generation == entity.Generation;
                return ref data;
            }

            success = false;
            return ref Dummy<Data>.Value;
        }

        void Destroy(Entity entity, ref Data data)
        {
            _messages.Emit(new OnPreDestroy { Entity = entity });
            data.Allocated = false;
            _frozen.Push(entity.Index);
            _messages.Emit(new OnPostDestroy { Entity = entity });
        }

        int ReserveIndex()
        {
            // Priotising the increase of the maximum index until it hits the capacity makes sure that all available indices are used.
            var index = _data.count < _data.items.Length || _free.count == 0 ? _data.count++ : _free.Pop();
            _data.Ensure();
            return index;
        }
    }
}