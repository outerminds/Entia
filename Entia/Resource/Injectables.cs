using Entia.Core;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
    public readonly struct Resource<T> : IInjectable where T : struct, IResource
    {
        public readonly struct Read : IInjectable
        {
            sealed class Injector : Injector<Read>
            {
                public override Result<Read> Inject(MemberInfo member, World world) => new Read(world.Resources().GetBox<T>());
            }

            sealed class Depender : IDepender
            {
                public IEnumerable<IDependency> Depend(MemberInfo member, World world)
                {
                    yield return new Dependencies.Read(typeof(T));
                    foreach (var dependency in world.Dependers().Dependencies<T>()) yield return dependency;
                }
            }

            [Injector]
            static readonly Injector _injector = new Injector();
            [Depender]
            static readonly Depender _depender = new Depender();

            public ref readonly T Value => ref _box.Value;

            readonly Box<T> _box;

            public Read(Box<T> box) { _box = box; }
        }

        sealed class Injector : Injector<Resource<T>>
        {
            public override Result<Resource<T>> Inject(MemberInfo member, World world) => new Resource<T>(world.Resources().GetBox<T>());
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                yield return new Dependencies.Write(typeof(T));
                foreach (var dependency in world.Dependers().Dependencies<T>()) yield return dependency;
            }
        }

        [Injector]
        static readonly Injector _injector = new Injector();
        [Depender]
        static readonly Depender _depender = new Depender();

        public ref T Value => ref _box.Value;

        readonly Box<T> _box;

        public Resource(Box<T> box) { _box = box; }
    }
}
