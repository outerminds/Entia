using Entia.Components;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Messages;
using Entia.Modules.Component;
using Entia.Modules.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    /// <summary>
    /// Module that stores and manages components.
    /// </summary>
    public sealed partial class Components : IModule, IResolvable, IEnumerable<IComponent>
    {
        struct Data
        {
            public bool IsValid => Segment != null;

            public Segment Segment;
            public int Index;
            public int? Transient;
        }

        struct Emitters
        {
            public bool IsValid => OnAdd != null && OnRemove != null && OnEnable != null && OnDisable != null;

            public Action<Entity> OnAdd;
            public Action<Entity> OnRemove;
            public Action<Entity> OnEnable;
            public Action<Entity> OnDisable;
        }

        /// <summary>
        /// Gets all the component segments.
        /// </summary>
        /// <value>
        /// The segments.
        /// </value>
        [ThreadSafe]
        public Slice<Segment>.Read Segments => _segments.Slice();

        readonly Entities _entities;
        readonly Messages _messages;
        readonly Emitter<OnException> _onException;
        readonly Emitter<OnAdd> _onAdd;
        readonly Emitter<OnRemove> _onRemove;
        readonly Emitter<OnEnable> _onEnable;
        readonly Emitter<OnDisable> _onDisable;
        readonly Emitter<Entia.Messages.Segment.OnCreate> _onCreate;
        readonly Emitter<Entia.Messages.Segment.OnMove> _onMove;
        readonly Transient _transient = new Transient();
        readonly Segment _created = new Segment(int.MaxValue, new BitMask());
        readonly Segment _destroyed = new Segment(int.MaxValue, new BitMask(), 1);
        readonly Segment _empty = new Segment(0, new BitMask());
        readonly Dictionary<BitMask, Segment> _maskToSegment;
        (Data[] items, int count) _data = (new Data[64], 0);
        (Segment[] items, int count) _segments;
        (Emitters[] items, int count) _emitters = (new Emitters[8], 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="Components"/> class.
        /// </summary>
        public Components(Entities entities, Messages messages)
        {
            _entities = entities;
            _messages = messages;
            _onException = _messages.Emitter<OnException>();
            _onAdd = _messages.Emitter<OnAdd>();
            _onRemove = _messages.Emitter<OnRemove>();
            _onEnable = _messages.Emitter<OnEnable>();
            _onDisable = _messages.Emitter<OnDisable>();
            _onCreate = _messages.Emitter<Entia.Messages.Segment.OnCreate>();
            _onMove = _messages.Emitter<Entia.Messages.Segment.OnMove>();
            // NOTE: do not include '_created' and '_destroyed' here
            _segments = (new Segment[] { _empty }, 1);
            _maskToSegment = new Dictionary<BitMask, Segment> { { _empty.Mask, _empty } };
            _messages.React((in OnCreate message) => Initialize(message.Entity));
            _messages.React((in OnPostDestroy message) => Dispose(message.Entity));
            foreach (var entity in entities) Initialize(entity);
        }

        /// <summary>
        /// Tries the get segment associated with the <paramref name="entity"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="pair">The segment and the index of the entity within it.</param>
        /// <returns>Returns <c>true</c> if the segment was found; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TrySegment(Entity entity, out (Segment segment, int index) pair)
        {
            if (TryGetData(entity, out var data))
            {
                pair = (data.Segment, data.Index);
                return true;
            }

            pair = default;
            return false;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IComponent> GetEnumerator()
        {
            foreach (var data in _data.Slice())
                if (data.IsValid) foreach (var component in Get(data, States.All)) yield return component;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IResolvable.Resolve()
        {
            var keep = 0;
            for (int i = 0; i < _transient.Slots.count; i++)
            {
                ref var slot = ref _transient.Slots.items[i];
                ref var data = ref GetData(slot.Entity, out var success);
                if (success)
                {
                    switch (slot.Resolution)
                    {
                        case Transient.Resolutions.None:
                            {
                                data.Transient = default;
                                break;
                            }
                        case Transient.Resolutions.Disabled:
                            {
                                if (MoveDisabledTo(ref data, ref slot, i, keep)) keep++;
                                break;
                            }
                        case Transient.Resolutions.Move:
                            {
                                var enabled = GetSegment(slot.Enabled);
                                MoveSegmentTo((data.Segment, data.Index), enabled);
                                if (MoveDisabledTo(ref data, ref slot, i, keep)) keep++;
                                break;
                            }
                        case Transient.Resolutions.Initialize:
                            {
                                var segment = GetSegment(slot.Enabled);
                                CopySegmentTo((data.Segment, data.Index), (segment, segment.Entities.count++));
                                if (MoveDisabledTo(ref data, ref slot, i, keep)) keep++;
                                break;
                            }
                        case Transient.Resolutions.Dispose:
                            {
                                MoveSegmentTo((data.Segment, data.Index), _destroyed);
                                ClearDisabled(i, slot);
                                _destroyed.Entities.count = 0;
                                data = default;
                                break;
                            }
                    }
                }
            }

            return _created.Entities.count.Change(0) | _destroyed.Entities.count.Change(0) | _transient.Slots.count.Change(keep);
        }

        [ThreadSafe]
        ref Data GetData(Entity entity, out bool success)
        {
            if (entity.Index < _data.count)
            {
                ref var data = ref _data.items[entity.Index];
                if (data.Segment is Segment segment)
                {
                    ref var entities = ref segment.Entities;
                    success = data.Index < entities.count && entities.items[data.Index] == entity;
                    return ref data;
                }
            }

            success = false;
            return ref Dummy<Data>.Value;
        }

        [ThreadSafe]
        bool TryGetData(Entity entity, out Data data)
        {
            data = GetData(entity, out var success);
            return success;
        }

        int MoveSegmentTo(in (Segment segment, int index) source, Segment target)
        {
            if (source.segment == target) return source.index;

            var index = target.Entities.count++;
            CopySegmentTo(source, (target, index));
            // NOTE: copy the last entity to the moved entity's slot
            CopySegmentTo((source.segment, --source.segment.Entities.count), source);
            return index;
        }

        bool MoveDisabledTo(ref Data data, ref Transient.Slot slot, int source, int target)
        {
            if (slot.Disabled.IsEmpty)
            {
                data.Transient = default;
                return false;
            }
            else if (source == target)
            {
                slot.Resolution = Transient.Resolutions.Disabled;
                return true;
            }
            else
            {
                var disabled = GetSegment(slot.Disabled);
                return MoveTransientTo(ref data, ref slot, source, target, disabled.Types.data);
            }
        }

        bool MoveTransientTo(ref Data data, ref Transient.Slot slot, int source, int target, Metadata[] types)
        {
            var moved = false;
            for (int i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                if (TryGetStore(data, metadata, States.All, out var sourceStore, out var sourceIndex))
                {
                    var targetStore = _transient.Store(target, metadata, out var targetIndex);
                    Array.Copy(sourceStore, sourceIndex, targetStore, targetIndex, 1);
                    // NOTE: clearing is not strictly needed, but is done when the component type contains managed references in order to allow
                    // them to be collected by the garbage collector
                    if (!metadata.Data.IsPlain) Array.Clear(sourceStore, sourceIndex, 1);
                    moved = true;
                }
            }

            data.Transient = target;
            slot = ref slot.Swap(ref _transient.Slots.items[target]);
            slot.Resolution = Transient.Resolutions.Disabled;
            return moved;
        }

        void ClearDisabled(int transient, in Transient.Slot slot)
        {
            if (slot.Disabled.IsEmpty) return;
            var disabled = GetSegment(slot.Disabled);
            ClearTransient(transient, disabled.Types.data);
        }

        void ClearTransient(int transient, Metadata[] types)
        {
            for (int i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                if (metadata.Data.IsPlain) continue;
                if (_transient.TryStore(transient, metadata, out var store, out var adjusted))
                    Array.Clear(store, adjusted, 1);
            }
        }

        bool CopySegmentTo(in (Segment segment, int index) source, in (Segment segment, int index) target)
        {
            if (source == target) return false;

            ref var entity = ref source.segment.Entities.items[source.index];
            if (entity == Entity.Zero) return false;

            ref var data = ref _data.items[entity.Index];
            if (target.segment.Entities.Set(target.index, entity)) target.segment.Ensure();

            var types = target.segment.Types.data;
            for (var i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                var targetStore = target.segment.Store(metadata.Index);

                if (TryGetStore(data, metadata, States.All, out var sourceStore, out var sourceIndex))
                {
                    Array.Copy(sourceStore, sourceIndex, targetStore, target.index, 1);
                    // NOTE: clearing is not strictly needed, but is done when the component type contains managed references in order to allow
                    // them to be collected by the garbage collector
                    if (!metadata.Data.IsPlain) Array.Clear(sourceStore, sourceIndex, 1);
                }
            }

            var message = new Entia.Messages.Segment.OnMove { Entity = entity, Source = source, Target = target };
            data.Segment = target.segment;
            data.Index = target.index;
            entity = default;
            _onMove.Emit(message);
            return true;
        }

        void Initialize(Entity entity)
        {
            var transient = _transient.Reserve(entity, Transient.Resolutions.Initialize);
            var segment = _created;
            var index = segment.Entities.count++;
            segment.Entities.Ensure();
            segment.Entities.items[index] = entity;
            _data.Set(entity.Index, new Data { Segment = segment, Index = index, Transient = transient });
        }

        void Dispose(Entity entity)
        {
            ref var data = ref GetData(entity, out var success);
            if (success)
            {
                ref var slot = ref GetTransientSlot(entity, ref data, Transient.Resolutions.None);
                Clear(ref slot, States.All);
                slot.Resolution.Set(Transient.Resolutions.Dispose);
            }
        }

        (Segment enabled, Segment disabled) GetTargetSegments(in Data data, States include) => data.Transient is int transient ?
            GetTargetSegments(_transient.Slots.items[transient], include) :
            (include.HasAny(States.Enabled) ? data.Segment : _empty, _empty);

        (Segment enabled, Segment disabled) GetTargetSegments(in Transient.Slot slot, States include)
        {
            var enabled = include.HasAny(States.Enabled) ? GetSegment(slot.Enabled) : _empty;
            var disabled = include.HasAny(States.Disabled) ? GetSegment(slot.Disabled) : _empty;
            return (enabled, disabled);
        }

        ref Transient.Slot GetTransientSlot(Entity entity, ref Data data, Transient.Resolutions resolution)
        {
            if (data.Transient is int transient)
            {
                ref var slot = ref _transient.Slots.items[transient];
                slot.Resolution.Set(resolution);
                return ref slot;
            }

            data.Transient = transient = _transient.Reserve(entity, resolution, data.Segment.Mask);
            return ref _transient.Slots.items[transient];
        }

        Segment GetSegment(BitMask mask)
        {
            if (mask.IsEmpty) return _empty;
            if (_maskToSegment.TryGetValue(mask, out var segment)) return segment;
            var clone = new BitMask { mask };
            segment = _maskToSegment[clone] = _segments.Push(new Segment(_segments.count, clone));
            _onCreate.Emit(new Entia.Messages.Segment.OnCreate { Segment = segment });
            return segment;
        }

        ref readonly Emitters GetEmitters<T>() where T : struct, IComponent
        {
            var index = ComponentUtility.Concrete<T>.Data.Index;
            _emitters.Ensure(ComponentUtility.Concrete<T>.Data.Index + 1);
            ref var emitters = ref _emitters.items[index];
            if (!emitters.IsValid) emitters = CreateEmitters<T>();
            return ref emitters;
        }

        ref readonly Emitters GetEmitters(in Metadata metadata)
        {
            _emitters.Ensure(metadata.Index + 1);
            ref var emitters = ref _emitters.items[metadata.Index];
            if (!emitters.IsValid) emitters = CreateEmitters(metadata);
            return ref emitters;
        }

        Emitters CreateEmitters<T>() where T : struct, IComponent
        {
            var metadata = ComponentUtility.Concrete<T>.Data;
            var onAdd = _messages.Emitter<OnAdd<T>>();
            var onRemove = _messages.Emitter<OnRemove<T>>();
            var onEnable = _messages.Emitter<OnEnable<T>>();
            var onDisable = _messages.Emitter<OnDisable<T>>();
            return new Emitters
            {
                OnAdd = new Action<Entity>(entity =>
                {
                    onAdd.Emit(new OnAdd<T> { Entity = entity });
                    _onAdd.Emit(new OnAdd { Entity = entity, Component = metadata });
                }),
                OnRemove = new Action<Entity>(entity =>
                {
                    onRemove.Emit(new OnRemove<T> { Entity = entity });
                    _onRemove.Emit(new OnRemove { Entity = entity, Component = metadata });
                }),
                OnEnable = new Action<Entity>(entity =>
                {
                    onEnable.Emit(new OnEnable<T> { Entity = entity });
                    _onEnable.Emit(new OnEnable { Entity = entity, Component = metadata });
                }),
                OnDisable = new Action<Entity>(entity =>
                {
                    onDisable.Emit(new OnDisable<T> { Entity = entity });
                    _onDisable.Emit(new OnDisable { Entity = entity, Component = metadata });
                }),
            };
        }

        Emitters CreateEmitters(in Metadata metadata) => (Emitters)GetType()
            .InstanceMethods()
            .First(method => method.Name == nameof(CreateEmitters) && method.IsGenericMethod)
            .MakeGenericMethod(metadata.Type)
            .Invoke(this, Array.Empty<object>());
    }
}