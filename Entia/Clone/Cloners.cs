using System;
using System.Collections;
using System.Collections.Generic;
using Entia.Cloneable;
using Entia.Cloners;
using Entia.Core;

namespace Entia.Modules
{
    public sealed class Cloners : IModule, IEnumerable<ICloner>
    {
        readonly World _world;
        readonly TypeMap<object, ICloner> _cloners = new TypeMap<object, ICloner>();
        readonly TypeMap<object, ICloner> _defaults = new TypeMap<object, ICloner>();

        public Cloners(World world) { _world = world; }

        public Result<T> Clone<T>(in T instance, TypeData type) => Get(type.Type).Clone(instance, type, _world).Cast<T>();
        public Result<T> Clone<T>(in T instance) => Get<T>().Clone(instance, TypeUtility.Cache<T>.Data, _world).Cast<T>();
        public Result<object> Clone(object instance, TypeData type) => Get(type.Type).Clone(instance, type, _world);
        public Result<object> Clone(object instance, Type type) => Clone(instance, TypeUtility.GetData(type));
        public ICloner Default<T>() => _defaults.TryGet<T>(out var cloner) ? cloner : Default(typeof(T));
        public ICloner Default(Type type) => _defaults.Default(type, typeof(ICloneable<>), typeof(ClonerAttribute), () => new Default());
        public ICloner Get<T>() => _cloners.TryGet<T>(out var cloner, true) ? cloner : Default<T>();
        public ICloner Get(Type type) => _cloners.TryGet(type, out var cloner, true) ? cloner : Default(type);
        public bool Has<T>() => _cloners.Has<T>(true);
        public bool Has(Type type) => _cloners.Has(type, true);
        public bool Set<T>(Cloner<T> cloner) => _cloners.Set<T>(cloner);
        public bool Set(Type type, ICloner cloner) => _cloners.Set(type, cloner);
        public bool Remove<T>() => _cloners.Remove<T>();
        public bool Remove(Type type) => _cloners.Remove(type);
        public bool Clear() => _defaults.Clear() | _cloners.Clear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<ICloner> GetEnumerator() => _cloners.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}