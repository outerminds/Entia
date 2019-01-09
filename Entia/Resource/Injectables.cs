using Entia.Core;
using Entia.Dependables;
using Entia.Injectors;
using Entia.Modules;
using System.Reflection;

namespace Entia.Injectables
{
    public readonly struct Resource<T> : IInjectable, IDepend<Write<T>>
        where T : struct, IResource
    {
        public readonly struct Read : IInjectable, IDepend<Read<T>>
        {
            sealed class Injector : Injector<Read>
            {
                public override Result<Read> Inject(MemberInfo member, World world) => new Read(world.Resources().Box<T>());
            }

            [Injector]
            static readonly Injector _injector = new Injector();

            public ref readonly T Value => ref _box.Value;

            readonly Box<T> _box;

            public Read(Box<T> box) { _box = box; }

            public static implicit operator Read(Resource<T> write) => new Read(write._box);
        }

        sealed class Injector : Injector<Resource<T>>
        {
            public override Result<Resource<T>> Inject(MemberInfo member, World world) => new Resource<T>(world.Resources().Box<T>());
        }

        [Injector]
        static readonly Injector _injector = new Injector();
        public static implicit operator Read(Resource<T> resource) => new Read(resource._box);

        public ref T Value => ref _box.Value;

        readonly Box<T> _box;

        public Resource(Box<T> box) { _box = box; }
    }
}
