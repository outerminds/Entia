using Entia.Core.Documentation;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Injectables
{
    [ThreadSafe]
    public readonly struct AllResources : IInjectable, IEnumerable<IResource>
    {
        [ThreadSafe]
        public readonly struct Read : IInjectable
        {
            [Injector]
            static Injector<Read> Injector => Injectors.Injector.From(world => new Read(world.Resources()));
            [Depender]
            static IDepender Depender => Dependers.Depender.From(new Dependencies.Read(typeof(IResource)));

            readonly Modules.Resources _resources;
            public Read(Modules.Resources resources) { _resources = resources; }

            /// <inheritdoc cref="Modules.Resources.Get{T}()"/>
            public ref readonly T Get<T>() where T : struct, IResource => ref _resources.Get<T>();
            /// <inheritdoc cref="Modules.Resources.Get(Type)"/>
            public IResource Get(Type type) => _resources.Get(type);
            /// <inheritdoc cref="Modules.Resources.TryGet{T}(out T)"/>
            public bool TryGet<T>(out T resource) where T : struct, IResource => _resources.TryGet<T>(out resource);
            /// <inheritdoc cref="Modules.Resources.TryGet(Type, out IResource)"/>
            public bool TryGet(Type type, out IResource resource) => _resources.TryGet(type, out resource);
            /// <inheritdoc cref="Modules.Resources.Has{T}()"/>
            public bool Has<T>() where T : struct, IResource => _resources.Has<T>();
            /// <inheritdoc cref="Modules.Resources.Has(Type)"/>
            public bool Has(Type type) => _resources.Has(type);
        }

        [Injector]
        static Injector<AllResources> Injector => Injectors.Injector.From(world => new AllResources(world.Resources()));
        [Depender]
        static IDepender Depender => Dependers.Depender.From(new Dependencies.Write(typeof(IResource)));

        readonly Modules.Resources _resources;
        public AllResources(Modules.Resources resources) { _resources = resources; }

        /// <inheritdoc cref="Modules.Resources.TryGet{T}(out T)"/>
        public bool TryGet<T>(out T resource) where T : struct, IResource => _resources.TryGet<T>(out resource);
        /// <inheritdoc cref="Modules.Resources.TryGet(Type, out IResource)"/>
        public bool TryGet(Type type, out IResource resource) => _resources.TryGet(type, out resource);
        /// <inheritdoc cref="Modules.Resources.Get{T}()"/>
        public ref T Get<T>() where T : struct, IResource => ref _resources.Get<T>();
        /// <inheritdoc cref="Modules.Resources.Get(Type)"/>
        public IResource Get(Type type) => _resources.Get(type);
        /// <inheritdoc cref="Modules.Resources.Set{T}(in T)"/>
        public void Set<T>(in T resource) where T : struct, IResource => _resources.Set(resource);
        /// <inheritdoc cref="Modules.Resources.Set(IResource)"/>
        public void Set(IResource resource) => _resources.Set(resource);
        /// <inheritdoc cref="Modules.Resources.Has{T}()"/>
        public bool Has<T>() where T : struct, IResource => _resources.Has<T>();
        /// <inheritdoc cref="Modules.Resources.Has(Type)"/>
        public bool Has(Type type) => _resources.Has(type);

        // NOTE: do not give easy access to 'Remove/Clear' methods since they may have unwanted side-effects (such as invalidating other 'Resource<T>')

        /// <inheritdoc cref="Modules.Resources.GetEnumerator()"/>
        public IEnumerator<IResource> GetEnumerator() => _resources.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _resources.GetEnumerator();
    }

    [ThreadSafe]
    public readonly struct Resource<T> : IInjectable where T : struct, IResource
    {
        [ThreadSafe]
        public readonly struct Read : IInjectable
        {
            [Injector]
            static Injector<Read> Injector => Injectors.Injector.From(world => new Read(world.Resources().Box<T>()));
            [Depender]
            static IDepender Depender => Dependers.Depender.From<T>(new Dependencies.Read(typeof(T)));

            public ref readonly T Value => ref _box.Value;

            readonly Box<T>.Read _box;

            public Read(Box<T>.Read box) { _box = box; }
        }

        [Injector]
        static Injector<Resource<T>> Injector => Injectors.Injector.From(world => new Resource<T>(world.Resources().Box<T>()));
        [Depender]
        static IDepender Depender => Dependers.Depender.From<T>(new Dependencies.Write(typeof(T)));

        public ref T Value => ref _box.Value;

        readonly Box<T> _box;

        public Resource(Box<T> box) { _box = box; }
    }
}
