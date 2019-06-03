using Entia.Analyzers;
using Entia.Core;
using Entia.Dependencies;
using Entia.Modules.Analysis;
using Entia.Nodes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed class Analyzers : IModule, IClearable, IEnumerable<IAnalyzer>
    {
        readonly World _world;
        readonly TypeMap<IAnalyzable, IAnalyzer> _defaults = new TypeMap<IAnalyzable, IAnalyzer>();
        readonly TypeMap<IAnalyzable, IAnalyzer> _analyzers = new TypeMap<IAnalyzable, IAnalyzer>();

        public Analyzers(World world) { _world = world; }

        public Result<IDependency[]> Analyze(Node node, Node root) => Get(node.Value.GetType()).Analyze(node, root, _world);
        public IAnalyzer Default<T>() where T : struct, IAnalyzable => _defaults.TryGet<T>(out var analyzer, false, false) ? analyzer : Default(typeof(T));
        public IAnalyzer Default(Type analyzable) => _defaults.Default(analyzable, typeof(IAnalyzable<>), typeof(AnalyzerAttribute), _ => new Default());
        public IAnalyzer Get<T>() where T : struct, IAnalyzable => _analyzers.TryGet<T>(out var analyzer, true, false) ? analyzer : Default<T>();
        public IAnalyzer Get(Type analyzable) => _analyzers.TryGet(analyzable, out var analyzer, true, false) ? analyzer : Default(analyzable);
        public bool Has<T>() where T : struct, IAnalyzable => _analyzers.Has<T>(true, false);
        public bool Has(Type analyzable) => _analyzers.Has(analyzable, true, false);
        public bool Set<T>(Analyzer<T> analyzer) where T : struct, IAnalyzable => _analyzers.Set<T>(analyzer);
        public bool Set(Type analyzable, IAnalyzer analyzer) => _analyzers.Set(analyzable, analyzer);
        public bool Remove<T>() where T : struct, IAnalyzable => _analyzers.Remove<T>(false, false);
        public bool Remove(Type analyzable) => _analyzers.Remove(analyzable, false, false);
        public bool Clear() => _defaults.Clear() | _analyzers.Clear();

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IAnalyzer> GetEnumerator() => _analyzers.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
