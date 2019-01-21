using Entia.Core;
using Entia.Dependables;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Injectors;
using Entia.Modules;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Entia.Injectables
{
    public readonly struct Emitter<T> : IInjectable where T : struct, IMessage
    {
        sealed class Injector : Injector<Emitter<T>>
        {
            public override Result<Emitter<T>> Inject(MemberInfo member, World world) => new Emitter<T>(world.Messages().Emitter<T>());
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                yield return new Emit(typeof(T));
                yield return new Write(typeof(T));
                foreach (var dependency in world.Dependers().Dependencies<T>()) yield return dependency;
            }
        }

        [Injector]
        static readonly Injector _injector = new Injector();
        [Depender]
        static readonly Depender _depender = new Depender();

        readonly Modules.Message.Emitter<T> _emitter;
        public Emitter(Modules.Message.Emitter<T> emitter) { _emitter = emitter; }
        public void Emit(in T message) => _emitter.Emit(message);
    }

    public readonly struct Receiver<T> : IInjectable where T : struct, IMessage
    {
        sealed class Injector : Injector<Receiver<T>>
        {
            public override Result<Receiver<T>> Inject(MemberInfo member, World world) => new Receiver<T>(world.Messages().Receiver<T>());
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                yield return new Read(typeof(T));
                foreach (var dependency in world.Dependers().Dependencies<T>()) yield return dependency;
            }
        }

        [Injector]
        static readonly Injector _injector = new Injector();
        [Depender]
        static readonly Depender _depender = new Depender();

        readonly Modules.Message.Receiver<T> _receiver;
        public Receiver(Modules.Message.Receiver<T> receiver) { _receiver = receiver; }
        public bool TryPop(out T message) => _receiver.TryPop(out message);
    }

    public readonly struct Reaction<T> : IInjectable where T : struct, IMessage
    {
        sealed class Injector : Injector<Reaction<T>>
        {
            public override Result<Reaction<T>> Inject(MemberInfo member, World world) => new Reaction<T>(world.Messages().Reaction<T>());
        }

        sealed class Depender : IDepender
        {
            public IEnumerable<IDependency> Depend(MemberInfo member, World world)
            {
                yield return new React(typeof(T));
                foreach (var dependency in world.Dependers().Dependencies<T>()) yield return dependency;
            }
        }

        [Injector]
        static readonly Injector _injector = new Injector();
        [Depender]
        static readonly Depender _depender = new Depender();

        readonly Modules.Message.Reaction<T> _reaction;
        public Reaction(Modules.Message.Reaction<T> reaction) { _reaction = reaction; }
        public bool Has(Action reaction) => _reaction.Has(reaction);
        public bool Has(InAction<T> reaction) => _reaction.Has(reaction);
        public bool Add(Action reaction) => _reaction.Add(reaction);
        public bool Add(InAction<T> reaction) => _reaction.Add(reaction);
        public bool Remove(Action reaction) => _reaction.Remove(reaction);
        public bool Remove(InAction<T> reaction) => _reaction.Remove(reaction);
        public bool Clear() => _reaction.Clear();
    }
}
