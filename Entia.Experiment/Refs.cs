using Entia.Injectables;
using Entia.Modules;
using Entia.Queryables;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Experiment
{
    public unsafe readonly ref struct Ref<T>
    {
        public readonly bool Valid;
        public readonly void* Pointer;

        public ref T Value
        {
            get
            {
                if (Valid) return ref Unsafe.AsRef<T>(Pointer);
                throw new InvalidOperationException();
            }
        }

        public Ref(ref T value)
        {
            Pointer = Unsafe.AsPointer(ref value);
            Valid = true;
        }
    }

    namespace References
    {
        public struct Hierarchy
        {
            public ref Entity Parent => ref _hierarchy.Value.Parent;
            public ref List<Entity> Children => ref _hierarchy.Value.Children;
            Write<Components.Hierarchy> _hierarchy;
            public Hierarchy(Write<Components.Hierarchy> hierarchy) { _hierarchy = hierarchy; }
        }
    }

    public delegate void RefAction<T>(ref T value);
    public delegate ref TOut RefMapFunc<TIn, TOut>(ref TIn input);
    public delegate Ref<TOut> RefBindFunc<TIn, TOut>(ref TIn input);

    public static class RefExtensions
    {
        public static Ref<T> Getz<T>(this Components<T> components, Entity entity) where T : struct, IComponent =>
            components.TryGet(entity, out var write) ? new Ref<T>(ref write.Value) : default;

        public static Ref<TOut> Map<TIn, TOut>(this in Ref<TIn> reference, RefMapFunc<TIn, TOut> map) =>
            reference.Valid ? new Ref<TOut>(ref map(ref reference.Value)) : default;
        public static Ref<TOut> Bind<TIn, TOut>(this in Ref<TIn> reference, RefBindFunc<TIn, TOut> bind) =>
            reference.Valid ? bind(ref reference.Value) : default;

        public static bool Try<T>(this in Ref<T> reference, RefAction<T> action)
        {
            if (reference.Valid)
            {
                action(ref reference.Value);
                return true;
            }
            return false;
        }

        static void Boba()
        {
            var world = new World();

            Components<Components.Hierarchy> hierarchies = default;

            var entity = world.Entities().Create();
            ref var hierarchy = ref hierarchies.Getz(entity).Value;

            hierarchies.Getz(entity).Try((ref Components.Hierarchy h1) =>
            {
                if (h1.Parent is Entity parent)
                    hierarchies.Getz(parent).Try((ref Components.Hierarchy h2) => h2.Children.Add(parent));
            });
        }
    }
}
