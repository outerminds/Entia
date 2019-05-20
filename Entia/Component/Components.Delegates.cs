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
            public bool IsValid;
            public Lazy<Metadata> IsDisabled;
            public Action<Entity> OnAdd;
            public Action<Entity> OnRemove;
            public Func<Entity, bool> Enable;
            public Func<Entity, bool> Disable;
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
            var metadata = ComponentUtility.Cache<T>.Data;
            var onAdd = _messages.Emitter<OnAdd<T>>();
            var onRemove = _messages.Emitter<OnRemove<T>>();
            var recusive = typeof(T).Is<IsDisabled>() || typeof(T).Is(typeof(IsDisabled<>), definition: true);
            return new Delegates
            {
                IsValid = true,
                IsDisabled = new Lazy<Metadata>(recusive ?
                    new Func<Metadata>(() => default) :
                    new Func<Metadata>(() => ComponentUtility.Cache<IsDisabled<T>>.Data)),
                OnAdd = entity =>
                {
                    onAdd.Emit(new OnAdd<T> { Entity = entity });
                    _onAdd.Emit(new OnAdd { Entity = entity, Component = metadata });
                },
                OnRemove = entity =>
                {
                    onRemove.Emit(new OnRemove<T> { Entity = entity });
                    _onRemove.Emit(new OnRemove { Entity = entity, Component = metadata });
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