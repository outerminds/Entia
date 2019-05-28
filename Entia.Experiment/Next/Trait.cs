using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Experiment.Traits
{
    public static class Trait
    {
        static class Cache<TTrait>
        {
            public static readonly TypeMap<object, Delegate> Implementations = new TypeMap<object, Delegate>();
        }

        public static bool Implement<TInstance, TTrait>(Func<TInstance, TTrait> implement) =>
            Cache<TTrait>.Implementations.Set<TInstance>(implement);

        public static bool TryGet<TInstance, TTrait>(this TInstance instance, out TTrait trait)
        {
            if (Cache<TTrait>.Implementations.TryGet<TInstance>(out var value) && value is Func<TInstance, TTrait> implement)
            {
                trait = implement(instance);
                return true;
            }
            trait = default;
            return false;
        }
    }

    public interface IImplementation<TInstance, TTrait>
    {
        TTrait Implement(TInstance instance);
    }

    public interface ITrait { }

    public struct Enumerator<T> : ITrait
    {
        public Func<T> Current;
        public Func<bool> MoveNext;
    }

    public struct Enumerable<T> : ITrait
    {
        public Func<Enumerator<T>> GetEnumerator;
    }

    public struct BobaEnumerator<T> : IImplementation<Boba<T>, Enumerator<T>>
    {
        public Enumerator<T> Implement(Boba<T> instance)
        {
            return new ArrayEnumerator<T>().Implement(instance.Array);
        }
    }

    public struct ArrayEnumerator<T> : IImplementation<T[], Enumerator<T>>
    {
        public Enumerator<T> Implement(T[] instance)
        {
            var index = -1;
            return new Enumerator<T>
            {
                Current = () => instance[index],
                MoveNext = () => ++index < instance.Length
            };
        }
    }

    public class Boba<T>
    {
        static Boba()
        {
            Trait.Implement<Boba<T>, Enumerator<T>>(instance =>
            {
                var index = -1;
                return new Enumerator<T>
                {
                    Current = () => instance.Array[index],
                    MoveNext = () => ++index < instance.Array.Length
                };
            });
        }

        public readonly T[] Array;

        IEnumerable<T> Karl()
        {
            if (this.TryGet(out Enumerable<T> enumerable))
            {
                var enumerator = enumerable.GetEnumerator();
                while (enumerator.MoveNext()) yield return enumerator.Current();
            }
        }
    }
}