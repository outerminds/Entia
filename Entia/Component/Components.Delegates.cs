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
        struct Delegates
        {
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
            public Lazy<Metadata> IsDisabled;
            public Action<Entity> OnAdd;
            public Action<Entity> OnRemove;
            public Action<Entity> OnEnable;
            public Action<Entity> OnDisable;
        }

        ref readonly Delegates GetDelegates<T>(in Metadata metadata) where T : struct, IComponent
        {
            ArrayUtility.Ensure(ref _delegates, metadata.Index + 1);
            ref var delegates = ref _delegates[metadata.Index];
            if (!delegates.IsValid) delegates = CreateDelegates<T>();
            return ref delegates;
        }

        ref readonly Delegates GetDelegates(in Metadata metadata)
        {
            ArrayUtility.Ensure(ref _delegates, metadata.Index + 1);
            ref var delegates = ref _delegates[metadata.Index];
            if (!delegates.IsValid) delegates = CreateDelegates(metadata);
            return ref delegates;
        }

        [ThreadSafe]
        bool TryGetDelegates(in Metadata metadata, out Delegates delegates)
        {
            if (metadata.Index < _delegates.Length) return (delegates = _delegates[metadata.Index]).IsValid;
            delegates = default;
            return false;
        }

        Delegates CreateDelegates<T>() where T : struct, IComponent
        {
            if (typeof(T).Is(typeof(Disabled<>), definition: true)) return Delegates.Empty;

            var metadata = ComponentUtility.Cache<T>.Data;
            var onAdd = _messages.Emitter<OnAdd<T>>();
            var onRemove = _messages.Emitter<OnRemove<T>>();
            var onEnable = _messages.Emitter<OnEnable<T>>();
            var onDisable = _messages.Emitter<OnDisable<T>>();
            return new Delegates
            {
                IsValid = true,
                IsDisabled = new Lazy<Metadata>(() => ComponentUtility.Cache<Disabled<T>>.Data),
                OnAdd = entity =>
                {
                    onAdd.Emit(new OnAdd<T> { Entity = entity });
                    _onAdd.Emit(new OnAdd { Entity = entity, Component = metadata });
                },
                OnRemove = entity =>
                {
                    onRemove.Emit(new OnRemove<T> { Entity = entity });
                    _onRemove.Emit(new OnRemove { Entity = entity, Component = metadata });
                },
                OnEnable = entity =>
                {
                    onEnable.Emit(new OnEnable<T> { Entity = entity });
                    _onEnable.Emit(new OnEnable { Entity = entity, Component = metadata });
                },
                OnDisable = entity =>
                {
                    onDisable.Emit(new OnDisable<T> { Entity = entity });
                    _onDisable.Emit(new OnDisable { Entity = entity, Component = metadata });
                }
            };
        }

        Delegates CreateDelegates(in Metadata metadata) => (Delegates)GetType()
            .InstanceMethods()
            .First(method => method.Name == nameof(CreateDelegates) && method.IsGenericMethod)
            .MakeGenericMethod(metadata.Type)
            .Invoke(this, Array.Empty<object>());
    }
}