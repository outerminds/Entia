using Entia.Core;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Modules
{
    public sealed class Dependers : IModule, IEnumerable<IDepender>
    {
        readonly World _world;
        readonly TypeMap<IDependable, IDepender> _defaults = new TypeMap<IDependable, IDepender>();
        readonly TypeMap<IDependable, IDepender> _dependers = new TypeMap<IDependable, IDepender>();
        readonly Dictionary<MemberInfo, IDependency[]> _dependencies = new Dictionary<MemberInfo, IDependency[]>();

        public Dependers(World world) { _world = world; }

        public IDependency[] Dependencies<T>() => Dependencies(typeof(T));
        public IDependency[] Dependencies(MemberInfo member)
        {
            if (_dependencies.TryGetValue(member, out var dependencies)) return dependencies;

            var set = new HashSet<MemberInfo>();
            IEnumerable<IDependency> Next(MemberInfo current)
            {
                var attributes = current.GetCustomAttributes(true);
                if (set.Add(current) && attributes.OfType<IgnoreAttribute>().None())
                {
                    switch (current)
                    {
                        case Type type:
                            foreach (var dependency in type.Hierarchy().SelectMany(child => Get(child).Depend(type, _world)))
                                yield return dependency;
                            break;
                        case FieldInfo field:
                            foreach (var dependency in Next(field.FieldType)) yield return dependency;
                            break;
                        case PropertyInfo property:
                            foreach (var dependency in Next(property.PropertyType)) yield return dependency;
                            break;
                        case MethodInfo method:
                            foreach (var dependency in method.GetParameters()
                                .Select(parameter => parameter.ParameterType)
                                .Append(method.ReturnType)
                                .SelectMany(Next))
                                yield return dependency;
                            break;
                        case ConstructorInfo constructor:
                            foreach (var dependency in constructor.GetParameters()
                                .Select(parameter => parameter.ParameterType)
                                .SelectMany(Next))
                                yield return dependency;
                            break;
                    }
                }
            }

            return _dependencies[member] = Next(member).Distinct().ToArray();
        }

        public IDepender Default<T>() where T : struct, IDependable => _defaults.TryGet<T>(out var depender) ? depender : Default(typeof(T));
        public IDepender Default(Type dependable) => _defaults.Default(dependable, typeof(IDependable<>), typeof(DependerAttribute), () => new Default());
        public bool Has<T>() where T : struct, IDependable => _dependers.Has<T>(true);
        public bool Has(Type dependable) => _dependers.Has(dependable, true);
        public IDepender Get<T>() where T : struct, IDependable => _dependers.TryGet<T>(out var depender, true) ? depender : Default<T>();
        public IDepender Get(Type dependable) => _dependers.TryGet(dependable, out var depender, true) ? depender : Default(dependable);
        public bool Set<T>(IDepender depender) where T : struct, IDependable => _dependers.Set<T>(depender);
        public bool Set(Type dependable, IDepender depender) => _dependers.Set(dependable, depender);
        public bool Remove<T>() where T : struct, IDependable => _dependers.Remove<T>();
        public bool Remove(Type dependable) => _dependers.Remove(dependable);
        public bool Clear()
        {
            var cleared = _defaults.Clear() | _dependers.Clear() | _dependencies.Count > 0;
            _dependencies.Clear();
            return cleared;
        }
        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IDepender> GetEnumerator() => _dependers.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
