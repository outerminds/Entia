using Entia.Core;
using Entia.Core.Documentation;
using Entia.Messages;
using Entia.Modules.Component;
using Entia.Modules.Message;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
    /// <summary>
    /// Module that stores and manages components.
    /// </summary>
    public sealed partial class Components : IModule, IClearable, IResolvable, IEnumerable<IComponent>
    {
        struct Data
        {
            public bool IsValid;
            public int Index;
            public Segment Segment;
            public int? Transient;
        }

        static Array[] _tags = new Array[8];

        /// <summary>
        /// Gets all the component segments.
        /// </summary>
        /// <value>
        /// The segments.
        /// </value>
        [ThreadSafe]
        public Segment[] Segments => _segments;

        readonly Messages _messages;
        readonly Emitter<OnException> _onException;
        readonly Emitter<Entia.Messages.Segment.OnCreate> _onCreate;
        readonly Emitter<Entia.Messages.Segment.OnMove> _onMove;
        readonly Transient _transient = new Transient();
        readonly Segment _created = new Segment(int.MaxValue, new BitMask());
        readonly Segment _destroyed = new Segment(int.MaxValue, new BitMask(), 1);
        readonly Dictionary<BitMask, Segment> _maskToSegment;
        readonly Segment _empty;
        (Data[] items, int count) _data;
        Segment[] _segments;
        Delegates[] _delegates;

        /// <summary>
        /// Initializes a new instance of the <see cref="Components"/> class.
        /// </summary>
        public Components(Entities entities, Messages messages, int capacity = 64) :
            this(messages, (new Data[capacity], 0), new[] { new Segment(0, new BitMask()) }, new Delegates[8])
        {
            foreach (var entity in entities) Initialize(entity);
        }

        // NOTE: this constructor is meant for cloning and serialization
        Components(Messages messages, in (Data[] items, int count) data, Segment[] segments, Delegates[] delegates)
        {
            _messages = messages;
            _data = data;
            _segments = segments;
            _delegates = delegates;
            _empty = segments[0];
            _maskToSegment = new Dictionary<BitMask, Segment>(segments.Length);
            foreach (var segment in segments) _maskToSegment[segment.Mask] = segment;

            _onException = _messages.Emitter<OnException>();
            _onCreate = _messages.Emitter<Entia.Messages.Segment.OnCreate>();
            _onMove = _messages.Emitter<Entia.Messages.Segment.OnMove>();

            _messages.React((in OnCreate message) => Initialize(message.Entity));
            _messages.React((in OnPostDestroy message) => Dispose(message.Entity));
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
        public IEnumerator<IComponent> GetEnumerator() => Get(States.All).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Resolve()
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
                if (data.IsValid)
                {
                    ref var entities = ref data.Segment.Entities;
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
            _data.Set(entity.Index, new Data { IsValid = true, Segment = segment, Index = index, Transient = transient });
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
        (BitMask mask, Metadata[] types) GetTargetData(in Data data) => data.Transient is int transient ?
            GetTargetData(_transient.Slots.items[transient]) : (data.Segment.Mask, data.Segment.Types);

        [ThreadSafe]
        (BitMask mask, Metadata[] types) GetTargetData(in Transient.Slot slot) => (slot.Mask, ComponentUtility.GetConcreteTypes(slot.Mask));

        [ThreadSafe]
        BitMask GetTargetMask(in Data data) => data.Transient is int transient ?
            _transient.Slots.items[transient].Mask : data.Segment.Mask;

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
            segment = _maskToSegment[clone] = ArrayUtility.Add(ref _segments, new Segment(_segments.Length, clone));
            _onCreate.Emit(new Entia.Messages.Segment.OnCreate { Segment = segment });
            return segment;
        }
    }
}