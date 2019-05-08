using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
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
        [ThreadSafe]
        public bool TryGet<T>(out T resource) where T : struct, IResource => _resources.TryGet<T>(out resource);
        /// <inheritdoc cref="Modules.Resources.TryGet(Type, out IResource)"/>
        [ThreadSafe]
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
        [ThreadSafe]
        public bool Has<T>() where T : struct, IResource => _resources.Has<T>();
        /// <inheritdoc cref="Modules.Resources.Has(Type)"/>
        [ThreadSafe]
        public bool Has(Type type) => _resources.Has(type);
        /// <inheritdoc cref="Modules.Resources.Remove{T}()"/>
        public bool Remove<T>() where T : struct, IResource => _resources.Remove<T>();
        /// <inheritdoc cref="Modules.Resources.Remove(Type)"/>
        public bool Remove(Type type) => _resources.Remove(type);
        /// <inheritdoc cref="Modules.Resources.Clear()"/>
        public bool Clear() => _resources.Clear();
        /// <inheritdoc cref="Modules.Resources.GetEnumerator()"/>
        public IEnumerator<IResource> GetEnumerator() => _resources.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _resources.GetEnumerator();
    }

    public readonly struct Resource<T> : IInjectable where T : struct, IResource
    {
        [ThreadSafe]
        public readonly struct Read : IInjectable
        {
            [Injector]
            static Injector<Read> Injector => Injectors.Injector.From(world => new Read(world.Resources().GetBox<T>()));
            [Depender]
            static IDepender Depender => Dependers.Depender.From<T>(new Dependencies.Read(typeof(T)));

            public ref readonly T Value => ref _box.Value;

            readonly Box<T> _box;

            public Read(Box<T> box) { _box = box; }
        }

        [Injector]
        static Injector<Resource<T>> Injector => Injectors.Injector.From(world => new Resource<T>(world.Resources().GetBox<T>()));
        [Depender]
        static IDepender Depender => Dependers.Depender.From<T>(new Dependencies.Write(typeof(T)));

        public ref T Value => ref _box.Value;

        readonly Box<T> _box;

        public Resource(Box<T> box) { _box = box; }
    }
}
