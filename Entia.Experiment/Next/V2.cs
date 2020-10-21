using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Entia.Core;
using Entia.Core.Documentation;

namespace Entia.Experiment.V2
{
    /*
    Concurrency Strict Table:
    - '~' prefix means that the left operation is deferred.
    - '~' suffix means that the top operation is deferred.
    - Prevents data races.
    - Prevents dangling pointers.
    - Prevents non-deterministic outcomes.
              | Create    | Destroy   | Read<T>   | Write<T>  | Add<T>    | Remove<T> | Read<U>   | Write<U>  | Add<U>    | Remove<U> |
    Create    |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |
    Destroy   |-----------|     Y     |   ~Y|N    |   ~Y|N    | ~Y|Y~|~Y~ | ~Y|Y~|~Y~ |   ~Y|N    |   ~Y|N    | ~Y|Y~|~Y~ | ~Y|Y~|~Y~ |
    Read<T>   |-----------|-----------|     Y     |     N     |   Y~|N    |   Y~|N    |     Y     |     Y     |     Y     |     Y     |
    Write<T>  |-----------|-----------|-----------|     N     |   Y~|N    |   Y~|N    |     Y     |     Y     |     Y     |     Y     |
    Add<T>    |-----------|-----------|-----------|-----------|  ~Y|Y~|N  |  ~Y|Y~|N  |     Y     |     Y     |     Y     |     Y     |
    Remove<T> |-----------|-----------|-----------|-----------|-----------|     Y     |     Y     |     Y     |     Y     |     Y     |

    * Add<T>|Remove<T> + Destroy
    - Final outcome is deterministic but intermediary state is not.
    - If it can be proven that the intermediary state can not be observed, then this is safe.
    - The return value of 'Add<T>' or 'Remove<T>' counts as intermediary state (probably not 'Destroy' though).
        - Example: 'if (Add<T>(entity)) Create();'
    - Might require 'Add<T>' and 'Remove<T>' to return 'void'.

    Concurrency Relaxed Table:
    - Prevents data races.
    - Tolerates invalid pointers.
        - If you have a 'Read<T>' pointer and then 'Remove<T>' it, you are responsible for not using the pointer.
    - Tolerates non-deterministic but consistent outcomes.
    - Assumes that programmer is more aware of the dangers of concurrent programs.
              | Create    | Destroy   | Read<T>   | Write<T>  | Add<T>    | Remove<T> | Read<U>   | Write<U>  | Add<U>    | Remove<U> |
    Create    |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |
    Destroy   |-----------|     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |     Y     |
    Read<T>   |-----------|-----------|     Y     |     N     |     N     |     Y     |     Y     |     Y     |     Y     |     Y     |
    Write<T>  |-----------|-----------|-----------|     N     |     N     |     Y     |     Y     |     Y     |     Y     |     Y     |
    Add<T>    |-----------|-----------|-----------|-----------|     N     |     N     |     Y     |     Y     |     Y     |     Y     |
    Remove<T> |-----------|-----------|-----------|-----------|-----------|     Y     |     Y     |     Y     |     Y     |     Y     |


    V2:
    Read<T>|Write<T> - Add<T>|Add<U>|Remove<T>|Remove<U>|Destroy
    - To prevent pointer invalidation, all structural changes (except maybe 'Create') must be deferred.
    - Even changes that do not concern the same components may invalidate other pointers since an entity moves
    with all its components.
    - Structural changes can also invalidate pointers of another entity from its segment because of the
    switching behavior of segment moves.

    - What if segments did not hold components but rather an array of ranges that points to the
    global component stores?
    - Or, instead of an array of ranges, it could be an array of chunk indices that point to specific
    chunks of the global component stores?
    - There would be some IBuffer[]


    Read<T> (or Write<T>) - Add<T>
    - must divide stores in chunks to preserve pointer validity
    - there might be tearing/race conditions if the Read<T> and Add<T> act on the same entity
    - since a system would have to prove that it cannot Read<T> and Add<T> act on the same entity to be safe and
    that this proof will most likely make the Add<T> useless, this will remains invalid.

    Read<T> (or Write<T>) - Remove<T>
    - may invalidate pointer if Read<T> and Remove<T> act on the same entity and cause it to move

    Read<T> (or Write<T>) - Add<U>
    - may invalidate pointer if Read<T> and Add<U> act on the same entity and cause it to move

    Read<T> (or Write<T>) - Remove<U>
    - may invalidate pointer if Read<T> and Remove<U> act on the same entity and cause it to move



    Systems:
        Systems should have one of the following format:
        - 'run' (systems that have no dependencies; these are meant to synchronize side effects
        on non ECS modules)
        - 'inject => run' (systems that don't require automatic access to entities)
        - 'inject => query => run' (full systems; common case)
        - '(inject, query) => run'? (alternative for above; more limited; might allow function pointers)

        Pointers
        - Could I make use of function pointers combined with component pointers?
        - Will require pinning of component stores (probably a '(IntPtr, GCHandle)[]' in each chunk).
        - Probably a good idea to run the garbage collector before pinning a store forever to give it
        a last chance to defragment memory.
        - Might require generating an obscene amount of overloads since function pointers cannot close
        over their context so the format 'inject => query => run' will need to become
        '(inject, query) => run'.
        - Measure performance before implementing this...

        SIMD
        - Support for systems that take a 'Span<T>' (or 'T[]' + 'int count') as input to allow
        for SIMD operations.



    Properties to test:
    - Sequential and parallel entity creation/destruction must produce the same changes.
        - Entity's index/generation may vary but components must be identical.
    - Sequential and automatic system scheduling must produce the same changes.
    */

    public sealed class Node
    {
        public static class System
        {
            public static Node Run(Action run) => throw null;
        }

        public static Node Inject<T>(InFunc<T, Node> provide) => throw null;
        public static Node Inject<T1, T2>(InFunc<T1, T2, Node> provide) => throw null;
        public static Node Sequence(params Node[] nodes) => throw null;
    }

    public static class NodeExtensions
    {
        public static Runner Schedule(this Node node, World world) => new Systems(world).Schedule(node);
    }

    public delegate void Runner();

    public readonly struct Systems
    {
        public Systems(World world)
        {

        }

        public Runner Schedule(Node node) => throw null;
    }

    public static class Boba
    {
        public static void Fett(Func<bool> @continue)
        {
            var world = new World();
            var initialize = Node.Sequence().Schedule(world);
            var preRun = Node.Sequence().Schedule(world);
            var run = Node.Sequence().Schedule(world);
            var postRun = Node.Sequence().Schedule(world);
            var finalize = Node.Sequence().Schedule(world);

            initialize();
            while (@continue())
            {
                preRun();
                run();
                postRun();
            }
            finalize();
        }
    }

    public sealed class World
    {
        TypeMap<object, object> _state = new();

        public bool TryGet<T>(out T state)
        {
            if (_state.TryGet<T>(out var value, false, true))
            {
                state = (T)value;
                return true;
            }
            state = default;
            return false;
        }

        public T Get<T>() where T : new() => Get(() => new T());
        public T Get<T>(Func<T> initialize)
        {
            if (TryGet<T>(out var state)) return state;
            lock (_state)
            {
                if (TryGet<T>(out state)) return state;
                state = initialize();
                var clone = _state.Clone();
                clone.Set(state.GetType(), state);
                Interlocked.Exchange(ref _state, clone);
                return state;
            }
        }
    }

    public readonly struct Entities
    {
        internal sealed class Meta
        {
            public readonly Type Type;
            public readonly int Index;
            public readonly bool Plain;

            public Meta(Type type, int index)
            {
                Type = type;
                Index = index;
            }
        }

        internal struct Data
        {
            public uint Generation;
            public int Alive;
            public int Index;
            public int Segment;
        }

        internal class State
        {
            const int Shift = 5;
            const int Size = 1 << Shift;
            const int Mask = Size - 1;

            public int Capacity => _data.Length * Size;

            readonly ConcurrentBag<int> _free = new ConcurrentBag<int>();
            Data[][] _data = { };
            int _last;

            public bool Has(Entity entity)
            {
                ref var data = ref DataAt(entity.Index);
                return data.Generation == entity.Generation && data.Alive > 0;
            }

            public void Create(Span<Entity> entities)
            {
                var created = 0;
                while (created < entities.Length)
                {
                    if (_free.TryTake(out var index))
                    {
                        ref var slot = ref DataAt(index);
                        // TODO: can this cause tearing when observed from the 'Enumerator'?
                        // Do this instead?
                        // var data = new Data { Generation = slot.Generation + 1, Alive = 1 };
                        // Interlocked.Exchange(
                        //     ref UnsafeUtility.As<Data, long>(ref slot),
                        //     UnsafeUtility.As<Data, long>(ref data));
                        slot = new Data { Generation = slot.Generation + 1, Alive = 1 };
                        entities[created++] = new Entity(index, slot.Generation);
                    }
                    else
                    {
                        var count = entities.Length - created;
                        index = Interlocked.Add(ref _last, count) - count;
                        var slots = DataAt(index, count);
                        for (int i = 0; i < slots.Length; i++)
                        {
                            ref var slot = ref slots[i];
                            slot = new Data { Generation = 1, Alive = 1 };
                            entities[created++] = new Entity(index + i, slot.Generation);
                        }
                    }
                }
            }

            // TODO: Add batch destruction: bool Destroy(Span<Entity> buffer)
            public bool Destroy(Entity entity, out int segment, out int index)
            {
                ref var data = ref DataAt(entity.Index);
                if (data.Generation == entity.Generation && Interlocked.Decrement(ref data.Alive) == 0)
                {
                    segment = data.Segment;
                    index = data.Index;
                    _free.Add(entity.Index);
                    return true;
                }
                segment = default;
                index = default;
                return false;
            }

            ref Data DataAt(int index) => ref _data[index >> Shift][index & Mask];

            Span<Data> DataAt(int index, int count)
            {
                var chunk = index >> Shift;
                var item = index & Mask;
                // Use a lock rather than 'CompareExchange' to prevent thread fighting.
                while (chunk >= _data.Length) lock (_data) Interlocked.Exchange(ref _data, _data.Append(new Data[Size]));
                return _data[chunk].AsSpan(item, count);
            }
        }

        readonly State _entities;
        readonly Components.State _components;
        readonly Components.Segment _empty;

        public Entities(World world)
        {
            _entities = world.Get<State>();
            _components = world.Get<Components.State>();
            _empty = _components.Segment();
        }

        public bool Has(Entity entity) => _entities.Has(entity);

        public Entity Create()
        {
            var entities = (Span<Entity>)stackalloc Entity[1];
            Create(entities);
            return entities[0];
        }

        public void Create(Span<Entity> entities)
        {
            _entities.Create(entities);
            _empty.Add(entities);
        }

        public bool Destroy(Entity entity)
        {
            if (_entities.Destroy(entity, out var segment, out var index))
            {
                // TODO: Since 'Destroy' and 'Remove' are in 2 distinct steps, a query could observe an destroyed entity.
                // Might not be a big deal since it essentially only affects 'Has(entity)'...
                _components.Segments[segment].Remove(index);
                return true;
            }
            return false;
        }
    }

    /*
    The 'Defer' module defers structural changes to entities to a synchronization point.
    - Structural changes include 'Add<T>', 'Remove<T>' and 'Destroy'.
    - Parallel systems will resolve their deferred actions sequentially, based on their declaration order.
    - Some actions may be ignored if they are made redundant by a later action.
        - Example: 'Add<T> -> Remove<T> -> Destroy' for the same entity can simply enact the 'Destroy' (what about messages?).
    - Resolving deferred actions may be done in parallel of a further system if dependency analysis allows it.
    */
    // public readonly struct Defer
    // {
    //     sealed class Data
    //     {
    //         public readonly int Index;
    //     }

    //     sealed class Store
    //     {
    //         public Array[] Chunks;
    //         public int Count;
    //         public int Last = -1;
    //         public readonly Action<Array, int> Resolve;
    //     }

    //     readonly struct Item<T>
    //     {
    //         public readonly T State;
    //         public readonly Action<T> Do;

    //         public Item(in T state, Action<T> @do)
    //         {
    //             State = state;
    //             Do = @do;
    //         }
    //     }

    //     sealed class State
    //     {
    //         static class Cache<T>
    //         {
    //             public static readonly int Index = _counter++;
    //         }

    //         static int _counter;

    //         Store[] _stores;
    //         int _count;
    //         int _last = -1;

    //         public void Do<T>(in T state, Action<T> @do)
    //         {
    //             (int chunk, int item) Decompose(int index) => (index >> 3, index & 7);

    //             var index = Cache<T>.Index;
    //             if (index >= _stores.Length)
    //             {
    //                 lock (this)
    //                 {
    //                     while (index >= _stores.Length)
    //                     {
    //                         var stores = new Store[_stores.Length * 2];
    //                         Array.Copy(_stores, stores, _stores.Length);
    //                         Interlocked.Exchange(ref _stores, stores);
    //                     }
    //                 }
    //             }

    //             if (_stores[index] == null)
    //             {
    //                 lock (this)
    //                 {
    //                     if (_stores[index] == null)
    //                         Interlocked.Exchange(ref _stores[index], new Store());
    //                 }
    //             }

    //             var store = _stores[index];
    //             ref var chunks = ref store.Chunks;
    //             var indices = Decompose(Interlocked.Increment(ref store.Last));

    //             if (indices.chunk >= chunks.Length)
    //             {
    //                 lock (store)
    //                 {
    //                     while (indices.chunk >= chunks.Length)
    //                         Interlocked.Exchange(ref chunks, chunks.Append(new Item<T>[8]));
    //                 }
    //             }

    //             var items = (Item<T>[])chunks[indices.chunk];
    //             items[indices.item] = new Item<T>(state, @do);
    //         }
    //     }

    //     readonly State _state;

    //     public Defer(World world)
    //     {
    //         _state = world.Get<State>();
    //     }
    // }

    public readonly struct Components
    {
        internal sealed class Meta
        {
            public enum Kinds { Data, Tag }

            public readonly Type Type;
            public readonly int Index;
            public readonly Kinds Kind;

            public Meta(Type type, int index)
            {
                Type = type;
                Index = index;
                Kind = type.Fields(true, false).Any() ? Kinds.Data : Kinds.Tag;
            }
        }

        internal struct Chunk
        {
            public readonly Entity[] Entities;
            public readonly Array[] Stores;
            public int Count;

            public Chunk(Entity[] entities, Array[] stores)
            {
                Entities = entities;
                Stores = stores;
                Count = 0;
            }
        }

        internal sealed class Segment
        {
            public delegate void Initialize<TState>(Array[] stores, int index, int count, in TState state);

            public readonly Meta[] Metas;
            public readonly Meta[] Data;
            public readonly Meta[] Tags;
            public int Capacity => _chunks.Length * _size;

            readonly int _size;
            readonly int _mask;
            readonly int _shift;
            readonly int _minimum;
            readonly int _maximum;
            readonly ConcurrentBag<int> _free = new ConcurrentBag<int>();
            Chunk[] _chunks = { };

            public Segment(Meta[] metas, int grow = 5)
            {
                Metas = metas.OrderBy(meta => meta.Index).ToArray();
                (Data, Tags) = Metas.Split(meta => meta.Kind == Meta.Kinds.Data);
                _size = 1 << grow;
                _mask = _size - 1;
                _shift = grow;
                _minimum = metas.Length == 0 ? 0 : metas.Min(meta => meta.Index);
                _maximum = metas.Length == 0 ? 0 : metas.Max(meta => meta.Index + 1);
            }

            public void Add<TState>(Span<Entity> entities, in TState state, Initialize<TState> initialize)
            {
                var index = 0;
                while (index < entities.Length)
                {
                    ref var chunk = ref _free.TryTake(out var free) ? ref _chunks[free] : ref ChunkAt(Capacity, out _, out _);
                    // Chunk is full. Lock is not required since if there is a concurrent 'Remove', the chunk's index will
                    // be added back in the '_free' bag. It is estimated to be better to not take the lock in this much more
                    // common case.
                    if (chunk.Count == _size) continue;
                    lock (chunk.Entities)
                    {
                        var reserved = Math.Min(_size - chunk.Count, entities.Length - index);
                        if (reserved == 0) continue;
                        else if (reserved == 1) chunk.Entities[chunk.Count] = entities[index];
                        else entities.CopyTo(chunk.Entities.AsSpan(chunk.Count, reserved));
                        initialize(chunk.Stores, index, reserved, state);
                        chunk.Count += reserved;
                        index += reserved;
                    }
                }
            }

            public void Add(Span<Entity> entities) => Add(entities, default, (Array[] _, int __, int ___, in Unit ____) => { });

            public void Remove(int index)
            {
                ref var chunk = ref ChunkAt(index, out var free, out var target);
                lock (chunk.Entities)
                {
                    var source = --chunk.Count;
                    if (source == target) return;
                    chunk.Entities[target] = chunk.Entities[source];
                    foreach (var meta in Data)
                    {
                        var store = chunk.Stores[Store(meta)];
                        Array.Copy(store, source, store, target, 1);
                        Array.Clear(store, source, 1);
                    }
                }
                _free.Add(free);
            }

            public int Store(Meta meta) => meta.Index + _minimum;

            ref Chunk ChunkAt(int index, out int chunk, out int item)
            {
                chunk = index >> _shift;
                item = index & _mask;
                while (chunk >= _chunks.Length)
                {
                    var entities = new Entity[_size];
                    var stores = new Array[_maximum - _minimum];
                    foreach (var meta in Metas) stores[Store(meta)] = Array.CreateInstance(meta.Type, _size);
                    // Use a lock rather than 'CompareExchange' to prevent thread fighting.
                    lock (_chunks) Interlocked.Exchange(ref _chunks, _chunks.Append(new Chunk(entities, stores)));
                }
                return ref _chunks[chunk];
            }
        }

        internal sealed class State
        {
            public Segment[] Segments = { };

            readonly ConcurrentDictionary<Type, Meta> _metas = new ConcurrentDictionary<Type, Meta>();
            int _counter;
            (int index, int segment)[][] _chunks = { new (int, int)[byte.MaxValue] };

            [ThreadSafe]
            public Meta Meta(Type type) => _metas.GetOrAdd(type, key => new Meta(key, Interlocked.Increment(ref _counter)));

            public Segment Segment(params Meta[] metas) => throw null;

            public ref T Get<T>(Entity entity, Meta meta) => throw null;
        }

        readonly State _state;

        public Components(World world) => _state = world.Get<State>();
    }

    public readonly struct Component<T> where T : struct
    {
        readonly Components.State _state;
        readonly Components.Meta _meta;

        public Component(World world)
        {
            _state = world.Get<Components.State>();
            _meta = _state.Meta(typeof(T));
        }
    }

    public readonly struct Template<T>
    {
        public delegate void Initialize(in T value, Array store, int index);

        public readonly (Type type, Initialize initialize)[] Initializers;
    }

    public readonly struct Factory<T>
    {
        readonly Entities.State _state;
        readonly Components.Segment _segment;
        readonly (int store, Template<T>.Initialize initialize)[] _initializers;

        public Factory(in Template<T> template, World world)
        {
            var state = world.Get<Components.State>();
            var pairs = template.Initializers
                .Select(pair => (meta: state.Meta(pair.type), pair.initialize))
                .OrderBy(pair => pair.meta.Index)
                .ToArray();
            var segment = state.Segment(pairs.Select(pair => pair.meta));

            _state = world.Get<Entities.State>();
            _segment = segment;
            _initializers = pairs.Select(pair => (segment.Store(pair.meta), pair.initialize));
        }

        public Entity Create(in T value)
        {
            var entities = (Span<Entity>)stackalloc Entity[1];
            Create(value, entities);
            return entities[0];
        }

        public void Create(in T value, Span<Entity> entities)
        {
            _state.Create(entities);
            _segment.Add(entities, (value, this), (Array[] stores, int index, int count, in (T value, Factory<T> factory) state) =>
            {
                foreach (var pair in state.factory._initializers)
                {
                    var store = stores[pair.store];
                    for (int i = index; i < count; i++) pair.initialize(state.value, store, i);
                }
            });
        }
    }
}