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

            public int Index;
            public Segment Segment;
            public int? Transient;
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
        (Array[] items, int count) _stores = (new Array[8], 0);
        Delegates[] _delegates = new Delegates[8];

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
                        case Transient.Resolutions.Move:
                            {
                                var enabled = GetSegment(slot.Mask);
                                MoveTo((data.Segment, data.Index), enabled);
                                data.Transient = default;
                                break;
                            }
                        case Transient.Resolutions.Initialize:
                            {
                                var segment = GetSegment(slot.Mask);
                                CopyTo((data.Segment, data.Index), (segment, segment.Entities.count++));
                                data.Transient = default;
                                break;
                            }
                        case Transient.Resolutions.Dispose:
                            {
                                MoveTo((data.Segment, data.Index), _destroyed);
                                _destroyed.Entities.count = 0;
                                data = default;
                                break;
                            }
                    }
                }
            }

            return _created.Entities.count.Change(0) | _destroyed.Entities.count.Change(0) | _transient.Slots.count.Change(0);
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

        bool HasData(Entity entity)
        {
            GetData(entity, out var success);
            return success;
        }

        [ThreadSafe]
        bool TryGetData(Entity entity, out Data data)
        {
            data = GetData(entity, out var success);
            return success;
        }

        int MoveTo(in (Segment segment, int index) source, Segment target)
        {
            if (source.segment == target) return source.index;

            var index = target.Entities.count++;
            CopyTo(source, (target, index));
            // NOTE: copy the last entity to the moved entity's slot
            CopyTo((source.segment, --source.segment.Entities.count), source);
            return index;
        }

        bool CopyTo(in (Segment segment, int index) source, in (Segment segment, int index) target)
        {
            if (source == target) return false;

            ref var entity = ref source.segment.Entities.items[source.index];
            if (entity == Entity.Zero) return false;

            ref var data = ref _data.items[entity.Index];
            if (target.segment.Entities.Set(target.index, entity)) target.segment.Ensure();

            var types = target.segment.Components;
            for (var i = 0; i < types.Length; i++)
            {
                ref readonly var metadata = ref types[i];
                var targetStore = target.segment.Store(metadata);

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

        [ThreadSafe]
        Metadata[] GetTargetTypes(in Data data) => data.Transient is int transient ?
            GetTargetTypes(_transient.Slots.items[transient]) : data.Segment.Types;

        [ThreadSafe]
        Metadata[] GetTargetTypes(in Transient.Slot slot) => ComponentUtility.GetConcreteTypes(slot.Mask);

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

        // NOTE: make sure that segments are only created with components that have their 'Delegates' initialized;
        // 'Include' queries depend on it
        Segment GetSegment(BitMask mask)
        {
            if (mask.IsEmpty) return _empty;
            if (_maskToSegment.TryGetValue(mask, out var segment)) return segment;
            var clone = new BitMask { mask };
            segment = _maskToSegment[clone] = _segments.Push(new Segment(_segments.count, clone));
            _onCreate.Emit(new Entia.Messages.Segment.OnCreate { Segment = segment });
            return segment;
        }
    }
}