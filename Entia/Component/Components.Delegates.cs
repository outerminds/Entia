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
    public sealed partial class Components
    {
        struct Delegates
        {
            public static class Silent<TComponent, TMessage> where TComponent : struct, IComponent where TMessage : struct, IMessage
            {
                public static readonly bool Is = typeof(TComponent).Is<ISilent>() || typeof(TComponent).Is<ISilent<TMessage>>();
            }

            public static class Cache<T> where T : struct, IComponent
            {
                public static readonly bool IsSilent = typeof(T).Is<ISilent>();
                public static readonly bool IsEnabled = typeof(T).Is<IEnabled>();
                public static readonly Delegates Empty = new Delegates
                {
                    IsValid = true,
                    Enabled = IsEnabled,
                    IsDisabled = new Lazy<Metadata>(() => ComponentUtility.Concrete<IsDisabled<T>>.Data),
                    OnAdd = Default<OnAdd, OnAdd<T>>(),
                    OnRemove = Default<OnRemove, OnRemove<T>>(),
                    OnEnable = Default<OnEnable, OnEnable<T>>(),
                    OnDisable = Default<OnDisable, OnDisable<T>>(),
                };

                static Action<Entity> Default<TA, TB>() where TA : struct, IMessage where TB : struct, IMessage =>
                    Silent<T, TA>.Is && Silent<T, TB>.Is ? (_ => { }) : default(Action<Entity>);
            }

            public static readonly Delegates Empty = new Delegates
            {
                IsValid = true,
                IsDisabled = new Lazy<Metadata>(() => default),
                OnAdd = _ => { },
                OnRemove = _ => { },
                OnEnable = _ => { },
                OnDisable = _ => { },
            };

            public bool IsValid;
            public bool Enabled;
            public Lazy<Metadata> IsDisabled;
            public Action<Entity> OnAdd;
            public Action<Entity> OnRemove;
            public Action<Entity> OnEnable;
            public Action<Entity> OnDisable;
            public Func<Messages, Delegates> Clone;
        }

        [ThreadSafe]
        static Delegates CreateDelegates<T>(Messages messages) where T : struct, IComponent
        {
            var delegates = Delegates.Cache<T>.Empty;
            delegates.Clone = CreateDelegates<T>;
            if (Delegates.Cache<T>.IsSilent) return delegates;

            var metadata = ComponentUtility.Concrete<T>.Data;
            // OnAdd
            if (!Delegates.Silent<T, OnAdd<T>>.Is)
            {
                var emitter = messages.Emitter<OnAdd<T>>();
                delegates.OnAdd += entity => emitter.Emit(new OnAdd<T> { Entity = entity });
            }
            if (!Delegates.Silent<T, OnAdd>.Is)
            {
                var emitter = messages.Emitter<OnAdd>();
                delegates.OnAdd += entity => emitter.Emit(new OnAdd { Entity = entity, Component = metadata });
            }

            // OnRemove
            if (!Delegates.Silent<T, OnRemove<T>>.Is)
            {
                var emitter = messages.Emitter<OnRemove<T>>();
                delegates.OnRemove += entity => emitter.Emit(new OnRemove<T> { Entity = entity });
            }
            if (!Delegates.Silent<T, OnRemove>.Is)
            {
                var emitter = messages.Emitter<OnRemove>();
                delegates.OnRemove += entity => emitter.Emit(new OnRemove { Entity = entity, Component = metadata });
            }

            // OnEnable
            if (!Delegates.Silent<T, OnEnable<T>>.Is)
            {
                var emitter = messages.Emitter<OnEnable<T>>();
                delegates.OnEnable += entity => emitter.Emit(new OnEnable<T> { Entity = entity });
            }
            if (!Delegates.Silent<T, OnEnable>.Is)
            {
                var emitter = messages.Emitter<OnEnable>();
                delegates.OnEnable += entity => emitter.Emit(new OnEnable { Entity = entity, Component = metadata });
            }

            // OnDisable
            if (!Delegates.Silent<T, OnDisable<T>>.Is)
            {
                var emitter = messages.Emitter<OnDisable<T>>();
                delegates.OnDisable += entity => emitter.Emit(new OnDisable<T> { Entity = entity });
            }
            if (!Delegates.Silent<T, OnDisable>.Is)
            {
                var emitter = messages.Emitter<OnDisable>();
                delegates.OnDisable += entity => emitter.Emit(new OnDisable { Entity = entity, Component = metadata });
            }

            return delegates;
        }

        [ThreadSafe]
        static Delegates CreateDelegates(in Metadata metadata, Messages messages) => (Delegates)typeof(Components)
            .StaticMethods()
            .First(method => method.Name == nameof(CreateDelegates) && method.IsGenericMethod)
            .MakeGenericMethod(metadata.Type)
            .Invoke(null, new object[] { messages });

        ref readonly Delegates GetDelegates<T>(in Metadata metadata) where T : struct, IComponent
        {
            ArrayUtility.Ensure(ref _delegates, metadata.Index + 1);
            ref var delegates = ref _delegates[metadata.Index];
            if (!delegates.IsValid) delegates = CreateDelegates<T>(_messages);
            return ref delegates;
        }

        ref readonly Delegates GetDelegates(in Metadata metadata)
        {
            ArrayUtility.Ensure(ref _delegates, metadata.Index + 1);
            ref var delegates = ref _delegates[metadata.Index];
            if (!delegates.IsValid) delegates = CreateDelegates(metadata, _messages);
            return ref delegates;
        }

        [ThreadSafe]
        bool TryGetDelegates(in Metadata metadata, out Delegates delegates)
        {
            if (metadata.Index < _delegates.Length) return (delegates = _delegates[metadata.Index]).IsValid;
            delegates = default;
            return false;
        }
    }
}