using Entia.Core;
using Entia.Messages;
using Entia.Segments;
using System;
using System.Linq;
using System.Reflection;

namespace Entia.Modules
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

        static Action<Messages, Entity> GetOrAdd<TMessage, TBase>(Type type, int index, string name) where TMessage : struct, IMessage
        {
            using (var read = Cache<TMessage, TBase>.Messages.Read())
                if (read.Value.TryGet(index, out var action)) return action;

            return (Action<Messages, Entity>)MakeMethod(type, name).Invoke(null, Type.EmptyTypes);
        }

        static MethodInfo MakeMethod(Type type, string name) => typeof(MessageUtility)
            .GetMethods(TypeUtility.Static)
            .First(current => current.Name == name && current.IsGenericMethod && current.GetParameters().Length == 0)
            .MakeGenericMethod(type);

        public static void OnAddTag<T>(this Messages messages, Entity entity) where T : struct, ITag => OnAddTag<T>()(messages, entity);
        public static void OnAddTag(this Messages messages, Entity entity, Type type, int local) => OnAddTag(type, local)(messages, entity);
        public static Action<Messages, Entity> OnAddTag(Type type, int local) => GetOrAdd<OnAdd, ITag>(type, local, nameof(OnAddTag));
        public static Action<Messages, Entity> OnAddTag<T>() where T : struct, ITag => GetOrAdd<OnAdd, ITag>(
            IndexUtility<ITag>.Cache<T>.Index.local,
            () =>
            {
                var index = IndexUtility<ITag>.Cache<T>.Index;
                return (messages, entity) =>
                {
                    messages.Emit(new OnAdd<T> { Entity = entity });
                    messages.Emit(new OnAdd { Entity = entity, Type = typeof(T), Index = index });
                };
            },
            () => OnRemoveTag<T>());

        public static void OnRemoveTag<T>(this Messages messages, Entity entity) where T : struct, ITag => OnRemoveTag<T>()(messages, entity);
        public static void OnRemoveTag(this Messages messages, Entity entity, Type type, int local) => OnRemoveTag(type, local)(messages, entity);
        public static Action<Messages, Entity> OnRemoveTag(Type type, int local) => GetOrAdd<OnRemove, ITag>(type, local, nameof(OnRemoveTag));
        public static Action<Messages, Entity> OnRemoveTag<T>() where T : struct, ITag => GetOrAdd<OnRemove, ITag>(
            IndexUtility<ITag>.Cache<T>.Index.local,
            () =>
            {
                var index = IndexUtility<ITag>.Cache<T>.Index;
                return (messages, entity) =>
                {
                    messages.Emit(new OnRemove<T> { Entity = entity });
                    messages.Emit(new OnRemove { Entity = entity, Type = typeof(T), Index = index });
                };
            },
            () => OnAddTag<T>());

        public static void OnAddComponent<T>(this Messages messages, Entity entity) where T : struct, IComponent => OnAddComponent<T>()(messages, entity);
        public static void OnAddComponent(this Messages messages, Entity entity, Type type, int local) => OnAddComponent(type, local)(messages, entity);
        public static Action<Messages, Entity> OnAddComponent(Type type, int local) => GetOrAdd<OnAdd, IComponent>(type, local, nameof(OnAddComponent));
        public static Action<Messages, Entity> OnAddComponent<T>() where T : struct, IComponent => GetOrAdd<OnAdd, IComponent>(
            IndexUtility<IComponent>.Cache<T>.Index.local,
            () =>
            {
                var index = IndexUtility<IComponent>.Cache<T>.Index;
                return (messages, entity) =>
                {
                    messages.Emit(new OnAdd<T> { Entity = entity });
                    messages.Emit(new OnAdd { Entity = entity, Type = typeof(T), Index = index });
                };
            },
            () => OnRemoveComponent<T>());

        public static void OnRemoveComponent<T>(this Messages messages, Entity entity) where T : struct, IComponent => OnRemoveComponent<T>()(messages, entity);
        public static void OnRemoveComponent(this Messages messages, Entity entity, Type type, int local) => OnRemoveComponent(type, local)(messages, entity);
        public static Action<Messages, Entity> OnRemoveComponent(Type type, int local) => GetOrAdd<OnRemove, IComponent>(type, local, nameof(OnRemoveComponent));
        public static Action<Messages, Entity> OnRemoveComponent<T>() where T : struct, IComponent => GetOrAdd<OnRemove, IComponent>(
            IndexUtility<IComponent>.Cache<T>.Index.local,
            () =>
            {
                var index = IndexUtility<IComponent>.Cache<T>.Index;
                return (messages, entity) =>
                {
                    messages.Emit(new OnRemove<T> { Entity = entity });
                    messages.Emit(new OnRemove { Entity = entity, Type = typeof(T), Index = index });
                };
            },
            () => OnAddComponent<T>());

        public static void OnCreateEntity<T>(this Messages messages, Entity entity) where T : struct, ISegment => OnCreateEntity<T>()(messages, entity);
        public static void OnCreateEntity(this Messages messages, Entity entity, Type type, int index) => OnCreateEntity(type, index)(messages, entity);
        public static Action<Messages, Entity> OnCreateEntity(Type type, int index) => GetOrAdd<OnCreate, ISegment>(type, index, nameof(OnCreateEntity));
        public static Action<Messages, Entity> OnCreateEntity<T>() where T : struct, ISegment => GetOrAdd<OnCreate, ISegment>(
            IndexUtility<ISegment>.Cache<T>.Index.local,
            () =>
            {
                var index = IndexUtility<ISegment>.Cache<T>.Index;
                var segment = (index.global, index.local, typeof(T));
                return (messages, entity) =>
                {
                    messages.Emit(new OnCreate<T> { Entity = entity });
                    messages.Emit(new OnCreate { Entity = entity, Segment = segment });
                };
            },
            () => { OnPreDestroyEntity<T>(); OnPostDestroyEntity<T>(); });

        public static void OnPreDestroyEntity<T>(this Messages messages, Entity entity) where T : struct, ISegment => OnPreDestroyEntity<T>()(messages, entity);
        public static void OnPreDestroyEntity(this Messages messages, Entity entity, Type type, int index) => OnPreDestroyEntity(type, index)(messages, entity);
        public static Action<Messages, Entity> OnPreDestroyEntity(Type type, int index) => GetOrAdd<OnPreDestroy, ISegment>(type, index, nameof(OnPreDestroyEntity));
        public static Action<Messages, Entity> OnPreDestroyEntity<T>() where T : struct, ISegment => GetOrAdd<OnPreDestroy, ISegment>(
            IndexUtility<ISegment>.Cache<T>.Index.local,
            () =>
            {
                var index = IndexUtility<ISegment>.Cache<T>.Index;
                var segment = (index.global, index.local, typeof(T));
                return (messages, entity) =>
                {
                    messages.Emit(new OnPreDestroy<T> { Entity = entity });
                    messages.Emit(new OnPreDestroy { Entity = entity, Segment = segment });
                };
            },
            () => { OnCreateEntity<T>(); OnPostDestroyEntity<T>(); });

        public static void OnPostDestroyEntity<T>(this Messages messages, Entity entity) where T : struct, ISegment => OnPostDestroyEntity<T>()(messages, entity);
        public static void OnPostDestroyEntity(this Messages messages, Entity entity, Type type, int index) => OnPostDestroyEntity(type, index)(messages, entity);
        public static Action<Messages, Entity> OnPostDestroyEntity(Type type, int index) => GetOrAdd<OnPostDestroy, ISegment>(type, index, nameof(OnPostDestroyEntity));
        public static Action<Messages, Entity> OnPostDestroyEntity<T>() where T : struct, ISegment => GetOrAdd<OnPostDestroy, ISegment>(
            IndexUtility<ISegment>.Cache<T>.Index.local,
            () =>
            {
                var index = IndexUtility<ISegment>.Cache<T>.Index;
                var segment = (index.global, index.local, typeof(T));
                return (messages, entity) =>
                {
                    messages.Emit(new OnPostDestroy<T> { Entity = entity });
                    messages.Emit(new OnPostDestroy { Entity = entity, Segment = segment });
                };
            },
            () => { OnCreateEntity<T>(); OnPreDestroyEntity<T>(); });
    }
}
