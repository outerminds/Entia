using Entia.Builders;
using Entia.Core;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Nodes;
using Entia.Phases;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed class Builders : IModule, IEnumerable<IBuilder>
    {
        readonly World _world;
        readonly TypeMap<IBuildable, IBuilder> _defaults = new TypeMap<IBuildable, IBuilder>();
        readonly TypeMap<IBuildable, IBuilder> _builders = new TypeMap<IBuildable, IBuilder>();

        public Builders(World world) { _world = world; }

        public Option<Runner<T>> Build<T>(Node node, Controller controller) where T : struct, IPhase => Get(node.Value.GetType()).Build<T>(node, controller, _world);
        public IBuilder Default<T>() where T : struct, IBuildable => _defaults.TryGet<T>(out var builder) ? builder : Default(typeof(T));
        public IBuilder Default(Type buildable) => _defaults.Default(buildable, typeof(IBuildable<>), typeof(BuilderAttribute), () => new Default());
        public bool Has<T>() where T : struct, IBuildable => _builders.Has<T>(true);
        public bool Has(Type buildable) => _builders.Has(buildable, true);
        public IBuilder Get<T>() where T : struct, IBuildable => _builders.TryGet<T>(out var builder, true) ? builder : Default<T>();
        public IBuilder Get(Type buildable) => _builders.TryGet(buildable, out var builder, true) ? builder : Default(buildable);
        public bool Set<T>(IBuilder builder) where T : struct, IBuildable => _builders.Set<T>(builder);
        public bool Set(Type buildable, IBuilder builder) => _builders.Set(buildable, builder);
        public bool Remove<T>() where T : struct, IBuildable => _builders.Remove<T>();
        public bool Remove(Type buildable) => _builders.Remove(buildable);
        public bool Clear() => _defaults.Clear() | _builders.Clear();

        public IEnumerator<IBuilder> GetEnumerator() => _builders.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
