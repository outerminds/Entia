using Entia.Builders;
using Entia.Core;
using Entia.Modules.Build;
using Entia.Nodes;
using Entia.Phases;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed class Builders : IModule, IClearable, IEnumerable<IBuilder>
    {
        readonly World _world;
        readonly TypeMap<IBuildable, IBuilder> _defaults = new TypeMap<IBuildable, IBuilder>();
        readonly TypeMap<IBuildable, IBuilder> _builders = new TypeMap<IBuildable, IBuilder>();

        public Builders(World world) { _world = world; }

        public Result<IRunner> Build(Node node, Node root) => Get(node.Value.GetType()).Build(node, root, _world);
        public IBuilder Default<T>() where T : struct, IBuildable => _defaults.TryGet<T>(out var builder, false, false) ? builder : Default(typeof(T));
        public IBuilder Default(Type buildable) => _defaults.Default(buildable, typeof(IBuildable<>), typeof(BuilderAttribute), _ => new Default());
        public bool Has<T>() where T : struct, IBuildable => _builders.Has<T>(true, false);
        public bool Has(Type buildable) => _builders.Has(buildable, true, false);
        public IBuilder Get<T>() where T : struct, IBuildable => _builders.TryGet<T>(out var builder, true, false) ? builder : Default<T>();
        public IBuilder Get(Type buildable) => _builders.TryGet(buildable, out var builder, true, false) ? builder : Default(buildable);
        public bool Set<T>(IBuilder builder) where T : struct, IBuildable => _builders.Set<T>(builder);
        public bool Set(Type buildable, IBuilder builder) => _builders.Set(buildable, builder);
        public bool Remove<T>() where T : struct, IBuildable => _builders.Remove<T>(false, false);
        public bool Remove(Type buildable) => _builders.Remove(buildable, false, false);
        public bool Clear() => _defaults.Clear() | _builders.Clear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IBuilder> GetEnumerator() => _builders.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
