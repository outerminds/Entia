using Entia.Core;
using Entia.Messages;
using System;
using System.Linq;
using System.Reflection;

namespace Entia.Modules.Component
{
    public static class MessageUtility
    {
        static class Cache<TMessage, TBase> where TMessage : struct, IMessage
        {
            public static readonly Concurrent<TypeMap<TBase, Action<Messages, Entity>>> Messages = new TypeMap<TBase, Action<Messages, Entity>>();
        }

        static Action<Messages, Entity> GetOrAdd<TMessage, TBase>(int index, Func<Action<Messages, Entity>> create, Action post = null) where TMessage : struct, IMessage =>
            Cache<TMessage, TBase>.Messages.ReadValueOrWrite(index, (index, create, post),
                state => state.create(),
                state => state.post?.Invoke());

        static Action<Messages, Entity> GetOrAdd<TMessage, TBase>(Metadata metadata, string name) where TMessage : struct, IMessage
        {
            using (var read = Cache<TMessage, TBase>.Messages.Read())
                if (read.Value.TryGet(metadata.Index, out var action)) return action;

            return (Action<Messages, Entity>)MakeMethod(metadata.Type, name).Invoke(null, Type.EmptyTypes);
        }

        static MethodInfo MakeMethod(Type type, string name) => typeof(MessageUtility)
            .GetMethods(TypeUtility.Static)
            .First(current => current.Name == name && current.IsGenericMethod && current.GetParameters().Length == 0)
            .MakeGenericMethod(type);

        public static void OnAdd<T>(this Messages messages, Entity entity) where T : struct, IComponent => OnAdd<T>()(messages, entity);
        public static void OnAdd(this Messages messages, Entity entity, in Metadata metadata) => OnAdd(metadata)(messages, entity);
        public static Action<Messages, Entity> OnAdd(in Metadata metadata) => GetOrAdd<OnAdd, IComponent>(metadata, nameof(OnAdd));
        public static Action<Messages, Entity> OnAdd<T>() where T : struct, IComponent => GetOrAdd<OnAdd, IComponent>(
            ComponentUtility.Cache<T>.Data.Index,
            () => (messages, entity) =>
            {
                messages.Emit(new OnAdd<T> { Entity = entity });
                messages.Emit(new OnAdd { Entity = entity, Component = ComponentUtility.Cache<T>.Data });
            },
            () => OnRemove<T>());

        public static void OnRemove<T>(this Messages messages, Entity entity) where T : struct, IComponent => OnRemove<T>()(messages, entity);
        public static void OnRemove(this Messages messages, Entity entity, in Metadata metadata) => OnRemove(metadata)(messages, entity);
        public static Action<Messages, Entity> OnRemove(in Metadata metadata) => GetOrAdd<OnRemove, IComponent>(metadata, nameof(OnRemove));
        public static Action<Messages, Entity> OnRemove<T>() where T : struct, IComponent => GetOrAdd<OnRemove, IComponent>(
            ComponentUtility.Cache<T>.Data.Index,
            () => (messages, entity) =>
            {
                messages.Emit(new OnRemove<T> { Entity = entity });
                messages.Emit(new OnRemove { Entity = entity, Component = ComponentUtility.Cache<T>.Data });
            },
            () => OnAdd<T>());
    }
}
