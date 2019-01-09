using Entia.Core;
using Entia.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed class Components2 : IModule, IResolvable
    {
        public enum States : byte { None, Reserved, Resolved, Moving }

        public struct Item
        {
            public Entity Entity;
            public States State;
            public Next Next;
        }

        public struct Chunk
        {
            public static readonly Chunk Empty = new Chunk
            {
                Items = new Item[0],
                Stores = new Array[0]
            };

            public Item[] Items;
            public Array[] Stores;
        }

        public sealed class Segment
        {
            public const int Overflow = 8;

            // TODO: count doesn't take into account reserved but unused items
            // public int Count => Maximum - Free.count - Frozen.count;
            public int Capacity => Chunks.items[0].Items.Length + (Chunks.count - 1) * Overflow;
            public ref Chunk Chunk => ref Chunks.items[0];

            public int Index;
            public BitMask Mask;
            public (int minimum, int maximum) Range;
            public (int local, int global, Type type)[] Types;
            public (Chunk[] items, int count) Chunks;
            public int Maximum;
            public (int[] items, int count) Free = (new int[4], 0);
            public (int[] items, int count) Frozen = (new int[4], 0);

            public Segment(int index, BitMask mask, int capacity = Overflow)
            {
                Index = index;
                Mask = mask;
                Types = Mask
                    .Select(global => IndexUtility.TryGetType(global, out var type) && IndexUtility<IComponent>.TryGetIndex(type, out var pair) ?
                        (pair.local, pair.global, type) : default)
                    .SomeBy(data => data.type)
                    .ToArray();
                Range = (Types.Select(pair => pair.local).FirstOrDefault(), Types.Select(pair => pair.local + 1).LastOrDefault());
                Chunks = (new Chunk[] { CreateChunk(capacity), default, default, default }, 1);
            }

            public ref Chunk GetChunk(int index, out int adjusted)
            {
                var capacity = Chunk.Items.Length;
                if (index < capacity)
                {
                    adjusted = index;
                    return ref Chunk;
                }

                adjusted = (index - capacity) % Overflow;
                var overflow = (index - capacity) / Overflow;
                while (overflow >= Chunks.count) Chunks.Push(CreateChunk());

                return ref Chunks.items[overflow];
            }

            public ref Entity GetEntity(int index) => ref GetItem(index).Entity;
            public ref States GetState(int index) => ref GetItem(index).State;
            public ref Next GetNext(int index) => ref GetItem(index).Next;

            public ref Item GetItem(int index)
            {
                ref var chunk = ref GetChunk(index, out index);
                return ref chunk.Items[index];
            }

            public bool Has<T>() where T : struct, IComponent => Has(IndexUtility<IComponent>.Cache<T>.Index.local);
            public bool Has(int local)
            {
                var index = StoreIndex(local);
                return index >= 0 && index < Chunk.Stores.Length && Chunk.Stores[index] != null;
            }

            public bool TryStore<T>(int index, out (T[] store, int index) result) where T : struct, IComponent
            {
                ref var chunk = ref GetChunk(index, out index);
                var storeIndex = StoreIndex<T>();
                if (storeIndex >= 0 && storeIndex < chunk.Stores.Length && chunk.Stores[storeIndex] is T[] store)
                {
                    result = (store, index);
                    return true;
                }

                result = default;
                return false;
            }

            public bool TryStore(int index, int local, out (Array store, int index) result)
            {
                ref var chunk = ref GetChunk(index, out index);
                var storeIndex = StoreIndex(local);
                if (storeIndex >= 0 && storeIndex < chunk.Stores.Length && chunk.Stores[storeIndex] is Array store)
                {
                    result = (store, index);
                    return true;
                }

                result = default;
                return false;
            }

            public bool Resolve()
            {
                if (Chunks.count > 0)
                {
                    var capacity = Chunk.Items.Length;
                    var size = capacity + Chunks.count * Overflow;
                    ArrayUtility.Ensure(ref Chunk.Items, size);
                    foreach (var (local, _, type) in Types) ArrayUtility.Ensure(ref Chunk.Stores[StoreIndex(local)], type, size);

                    for (int i = 0; i < Chunks.count; i++)
                    {
                        var chunk = Chunks.items[i];
                        var index = capacity + i * Overflow;
                        Array.Copy(chunk.Items, 0, Chunk.Items, index, Overflow);
                        foreach (var (local, _, type) in Types)
                        {
                            var store = StoreIndex(local);
                            Array.Copy(chunk.Stores[store], 0, Chunk.Stores[store], index, Overflow);
                        }
                    }

                    Chunks.Clear();
                    return true;
                }

                return false;
            }

            int StoreIndex(int local) => local - Range.minimum;
            int StoreIndex<T>() where T : struct, IComponent => StoreIndex(IndexUtility<IComponent>.Cache<T>.Index.local);

            Chunk CreateChunk(int capacity = Overflow)
            {
                var chunk = new Chunk
                {
                    Items = new Item[capacity],
                    Stores = new Array[Range.maximum - Range.minimum]
                };
                foreach (var (local, _, type) in Types) chunk.Stores[StoreIndex(local)] = Array.CreateInstance(type, capacity);
                return chunk;
            }
        }

        public interface IBuffer
        {
            int Capacity { get; }
            int Overflows { get; }

            bool Resolve();
            bool Clear(int index);
            bool CopyTo(IBuffer buffer, int source, int target);
        }

        public sealed class Buffer<T> : IBuffer
        {
            public const int Overflow = 8;

            public int Capacity => _items.Length + Overflows * Overflow;
            public int Overflows => _overflows.Count;
            public ref T this[int index]
            {
                get
                {
                    if (TryGet(index, out var buffer, out var adjusted)) return ref buffer[adjusted];
                    throw new IndexOutOfRangeException();
                }
            }

            T[] _items = new T[Overflow];
            readonly List<T[]> _overflows = new List<T[]>();

            public bool TryGet(int index, out T[] chunk, out int adjusted)
            {
                if (index < _items.Length)
                {
                    chunk = _items;
                    adjusted = index;
                    return true;
                }

                var overflow = (index - _items.Length) / Overflow;
                if (overflow < _overflows.Count)
                {
                    chunk = _overflows[overflow];
                    adjusted = (index - _items.Length) % Overflow;
                    return true;
                }

                chunk = default;
                adjusted = default;
                return false;
            }

            public void Set(in T value, int index)
            {
                var (buffer, adjusted) = Get(index);
                buffer[adjusted] = value;
            }

            public (T[] chunk, int adjusted) Get(int index)
            {
                if (index < _items.Length) return (_items, index);

                var adjusted = (index - _items.Length) % Overflow;
                var overflow = (index - _items.Length) / Overflow;
                while (overflow >= _overflows.Count) _overflows.Add(new T[Overflow]);

                return (_overflows[overflow], adjusted);
            }

            public bool Clear(int index)
            {
                if (TryGet(index, out var chunk, out var adjusted))
                {
                    chunk[adjusted] = default;
                    return true;
                }

                return false;
            }

            public bool CopyTo(Buffer<T> buffer, int source, int target)
            {
                if (TryGet(source, out var sourceChunk, out var sourceAdjusted))
                {
                    var (targetChunk, targetAdjusted) = buffer.Get(target);
                    targetChunk[targetAdjusted] = sourceChunk[sourceAdjusted];
                    return true;
                }

                return false;
            }

            public void Clear()
            {
                _items.Clear();
                _overflows.Clear();
            }

            public bool Resolve()
            {
                var count = _items.Length;
                if (ArrayUtility.Ensure(ref _items, _items.Length + _overflows.Count * Overflow))
                {
                    for (var i = 0; i < _overflows.Count; i++)
                    {
                        var overflow = _overflows[i];
                        Array.Copy(overflow, 0, _items, count + i * Overflow, overflow.Length);
                    }

                    _overflows.Clear();
                    return true;
                }
                // TODO: shrink if too large?

                return false;
            }

            bool IBuffer.CopyTo(IBuffer buffer, int source, int target) => buffer is Buffer<T> casted && CopyTo(casted, source, target);
        }

        public struct Next
        {
            public Segment Segment;
            public int Index;
            public int Local;
        }

        struct Data
        {
            public (int index, Segment segment) Head;
            public (int index, Segment segment) Tail;
        }

        readonly Entities _entities;
        readonly Messages _messages;

        (Data[] items, int count) _entityToData;
        (Segment[] items, int count) _segments;
        readonly Segment _empty;
        readonly Dictionary<BitMask, Segment> _maskToSegment;
        readonly Dictionary<(Segment segment, int global), Segment> _transitions = new Dictionary<(Segment segment, int global), Segment>();

        public Components2(Entities entities, Messages messages)
        {
            _entities = entities;
            _messages = messages;

            _entityToData = (new Data[entities.Capacity], 0);
            _empty = new Segment(0, new BitMask());
            _segments = (new Segment[] { _empty }, 1);
            _maskToSegment = new Dictionary<BitMask, Segment> { { _empty.Mask, _empty } };
            _messages.React((in OnCreate message) => Initialize(message.Entity));
            _messages.React((in OnPreDestroy message) => Dispose(message.Entity));
        }

        public ref T Write<T>(Entity entity) where T : struct, IComponent
        {
            ref var data = ref GetData(entity);
            if (data.Tail.segment.Has<T>())
            {
                var current = data.Head;
                while (true)
                {
                    if (current.segment.TryStore<T>(current.index, out var pair)) return ref pair.store[pair.index];
                    var next = current.segment.GetItem(current.index).Next;
                    // NOTE: should not have to make this check since if the move chain has been traversed without finding a buffer, something went wrong
                    // if (next.Segment == null) break;
                    current = (next.Index, next.Segment);
                }
            }

            if (_messages.Has<OnException>()) _messages.Emit(new OnException { Exception = ExceptionUtility.MissingComponent(entity, typeof(T)) });
            return ref Dummy<T>.Value;
        }
        public bool Has<T>(Entity entity) where T : struct, IComponent => Has(entity, IndexUtility<IComponent>.Cache<T>.Index.local);
        public bool Has(Entity entity, Type component) => IndexUtility<IComponent>.TryGetIndex(component, out var index) && Has(entity, index.local);

        public bool Set<T>(Entity entity, in T component) where T : struct, IComponent
        {
            ref var data = ref GetData(entity);
            ref var item = ref GetItem(data, entity, out var success);
            if (success)
            {
                var add = !data.Tail.segment.Has<T>();
                // NOTE: component does not exist
                if (add) ChainMoveTo<T>(ref data, ref item, true);

                var current = data.Head;
                // NOTE: verify if the component has been added somewhere in the move chain
                while (true)
                {
                    if (current.segment.TryStore<T>(current.index, out var pair))
                    {
                        pair.store[pair.index] = component;
                        break;
                    }

                    var next = current.segment.GetNext(current.index);
                    // NOTE: should not have to make this check since if the move chain has been traversed without finding a buffer, something went wrong
                    // if (next.segment == null) break;
                    current = (next.Index, next.Segment);
                }

                return add;
            }

            return false;
        }

        public bool Remove<T>(Entity entity) where T : struct, IComponent
        {
            ref var data = ref GetData(entity);
            ref var item = ref GetItem(data, entity, out var success);
            if (success && data.Tail.segment.Has<T>())
            {
                ChainMoveTo<T>(ref data, ref item, false);
                return true;
            }

            return false;
        }

        public bool Clear(Entity entity)
        {
            ref var data = ref GetData(entity);
            ref var item = ref GetItem(data, entity, out var success);
            if (success)
            {
                CancelMoveChain(ref data, ref item, true);
                ChainMoveTo(ref data, ref item, _empty, int.MaxValue, false);
                CommitMoveChain(ref data, ref item, true);
                return true;
            }

            return false;
        }

        public void Resolve()
        {
            for (var i = 0; i < _segments.count; i++)
            {
                var segment = _segments.items[i];
                segment.Resolve();
                ResolveMoveChain(segment);
            }
        }

        bool CopyTo(in (int index, Segment segment) source, in (int index, Segment segment) target)
        {
            var success = false;
            foreach (var (local, _, _) in target.segment.Types) success |= CopyTo(local, source, target);
            return success;
        }

        bool CopyTo(int local, in (int index, Segment segment) source, in (int index, Segment segment) target)
        {
            if (source.segment.TryStore(source.index, local, out var pair1) && target.segment.TryStore(target.index, local, out var pair2))
            {
                Array.Copy(pair1.store, pair1.index, pair2.store, pair2.index, 1);
                return true;
            }

            return false;
        }

        int Reserve(Segment segment)
        {
            var index = segment.Free.count > 0 ? segment.Free.Pop() : segment.Maximum++;
            // NOTE: this call ensures that any chunk that needs to be created will be.
            segment.GetChunk(index, out _);
            return index;
        }

        void ChainMoveTo<T>(ref Data data, ref Item item, bool add) where T : struct, IComponent =>
            ChainMoveTo(ref data, ref item, IndexUtility<IComponent>.Cache<T>.Index, add);
        void ChainMoveTo(ref Data data, ref Item item, (int global, int local) index, bool add) =>
            ChainMoveTo(ref data, ref item, GetNextSegment(data.Tail.segment, index.global, add), index.local, add);

        void ChainMoveTo(ref Data data, ref Item item, Segment target, int local, bool add)
        {
            // NOTE: only freeze the index if the state was resolved to prevent duplicate freeze.
            if (item.State == States.Resolved) data.Head.segment.Frozen.Push(data.Head.index);

            var index = Reserve(target);
            item.State = States.Moving;
            item.Next = new Next { Segment = target, Index = index, Local = add ? local : ~local };
            data.Tail = (index, target);
        }

        bool ResolveMoveChain(Segment segment)
        {
            var moved = false;
            while (segment.Frozen.TryPop(out var index))
            {
                ref var item = ref segment.GetItem(index);
                if (item.Entity != Entity.Zero && item.State == States.Moving)
                {
                    ref var data = ref GetData(item.Entity);
                    if (data.Head.segment != data.Tail.segment)
                    {
                        CommitMoveChain(ref data, ref item, false);
                        moved = true;
                    }
                    // NOTE: when segment is the same, no use to move the entity.
                    else if (data.Head.index != data.Tail.index) CancelMoveChain(ref data, ref item, false);
                    // NOTE: this means that the entity has been destroyed.
                    else segment.Free.Push(index);
                }
            }

            return moved;
        }

        void CommitNextMoveChain(in Data data, ref Item item, bool freeze)
        {
            if (item.Next.Segment == null) return;

            ref var next = ref item.Next.Segment.GetItem(item.Next.Index);
            CommitNextMoveChain(data, ref next, freeze);

            // NOTE: if a component was added, copy it
            if (next.Next.Local >= 0) CopyTo(next.Next.Local, (next.Next.Index, next.Next.Segment), data.Tail);

            // NOTE: all indices except the last will be freed
            if (freeze) item.Next.Segment.Frozen.Push(item.Next.Index);
            else item.Next.Segment.Free.Push(item.Next.Index);
            item = default;
        }

        void CommitMoveChain(ref Data data, ref Item item, bool freeze)
        {
            if (item.State == States.Moving)
            {
                // NOTE: keep a copy of the entity since 'CommitNextMoveChain' will clear it
                var entity = item.Entity;
                // NOTE: commit move chain in reverse such that earlier added components have precedence over later ones
                CommitNextMoveChain(data, ref item, freeze);
                // NOTE: copy components from head after the ones from the move chain because they will always have precedence
                CopyTo(data.Head, data.Tail);
                data.Head = data.Tail;
                item = ref data.Tail.segment.GetItem(data.Tail.index);
                item = new Item { Entity = entity, State = States.Resolved };
            }
        }

        void CancelMoveChain(ref Data data, ref Item item, bool freeze)
        {
            if (item.State == States.Moving)
            {
                var current = item.Next;
                while (current.Segment != null)
                {
                    // NOTE: all indices except the first will be freed
                    if (freeze) current.Segment.Frozen.Push(current.Index);
                    else current.Segment.Free.Push(current.Index);

                    ref var next = ref current.Segment.GetItem(current.Index);
                    current = next.Next;
                    next = default;
                }

                item.State = States.Resolved;
                item.Next = default;
                data.Tail = data.Head;
            }
        }

        bool Has(Entity entity, int local)
        {
            ref var data = ref GetData(entity);
            ref var item = ref GetItem(data, entity, out var success);
            return success && data.Tail.segment.Has(local);
        }

        void Initialize(Entity entity)
        {
            var index = Reserve(_empty);
            _entityToData.count = Math.Max(_entityToData.count, entity.Index + 1);
            _entityToData.Ensure();
            _entityToData.items[entity.Index] = new Data { Head = (index, _empty), Tail = (index, _empty) };
            _empty.GetItem(index) = new Item { Entity = entity, State = States.Resolved };
        }

        void Dispose(Entity entity)
        {
            ref var data = ref GetData(entity);
            ref var item = ref GetItem(data, entity, out var success);
            if (success)
            {
                CancelMoveChain(ref data, ref item, true);
                data = default;
                item = default;
            }
        }

        ref Data GetData(Entity entity) => ref _entityToData.items[entity.Index];
        ref Item GetItem(in Data data, Entity entity, out bool success)
        {
            ref var item = ref data.Head.segment.GetItem(data.Head.index);
            success = item.Entity == entity;
            return ref item;
        }

        Segment GetNextSegment<T>(Segment segment, bool add) where T : struct, IComponent => GetNextSegment(segment, IndexUtility<IComponent>.Cache<T>.Index.global, add);
        Segment GetNextSegment(Segment segment, int global, bool add)
        {
            var toKey = (segment, add ? global : ~global);
            if (_transitions.TryGetValue(toKey, out var next)) return next;

            var mask = new BitMask(segment.Mask);
            if (add) mask.Add(global);
            else mask.Remove(global);

            if (!_maskToSegment.TryGetValue(mask, out next))
                next = _maskToSegment[mask] = CreateSegment(mask);

            var fromKey = (next, ~global);
            _transitions[toKey] = next;
            _transitions[fromKey] = segment;
            return next;
        }

        Segment CreateSegment(BitMask mask)
        {
            var segment = new Segment(_segments.count, mask);
            _segments.Push(segment);
            // _messages.Emit(new OnSegment { Segment = segment });
            return segment;
        }
    }
}