using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
    [ThreadSafe]
    public readonly struct Resource<T> : IInjectable where T : struct, IResource
    {
        [ThreadSafe]
        public readonly struct Read : IInjectable
        {
            [Injector]
            static Injector<Read> Injector => Injectors.Injector.From(world => new Read(world.Resources().GetBox<T>()));
            [Depender]
            static IDepender Depender => Dependers.Depender.From(world => world.Dependers().Dependencies<T>().Prepend(new Dependencies.Read(typeof(T))));

            public ref readonly T Value => ref _box.Value;

            readonly Box<T> _box;

            public Read(Box<T> box) { _box = box; }
        }

        [Injector]
        static Injector<Resource<T>> Injector => Injectors.Injector.From(world => new Resource<T>(world.Resources().GetBox<T>()));
        [Depender]
        static IDepender Depender => Dependers.Depender.From(world => world.Dependers().Dependencies<T>().Prepend(new Dependencies.Write(typeof(T))));

        public ref T Value => ref _box.Value;

        readonly Box<T> _box;

        public Resource(Box<T> box) { _box = box; }
    }
}
