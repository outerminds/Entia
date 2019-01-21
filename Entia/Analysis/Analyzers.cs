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
    public sealed class Analyzers : IModule, IEnumerable<IAnalyzer>
    {
        readonly World _world;
        readonly TypeMap<IAnalyzable, IAnalyzer> _defaults = new TypeMap<IAnalyzable, IAnalyzer>();
        readonly TypeMap<IAnalyzable, IAnalyzer> _analyzers = new TypeMap<IAnalyzable, IAnalyzer>();
        readonly Dictionary<(Node node, Node root), Result<IDependency[]>> _analyses = new Dictionary<(Node node, Node root), Result<IDependency[]>>();

        public Analyzers(World world) { _world = world; }

        public Result<IDependency[]> Analyze(Node node, Node root) =>
            _analyses.TryGetValue((node, root), out var result) ? result :
            _analyses[(node, root)] = Get(node.Value.GetType()).Analyze(node, root, _world);

        public IAnalyzer Default<T>() where T : struct, IAnalyzable => _defaults.TryGet<T>(out var analyzer) ? analyzer : Default(typeof(T));
        public IAnalyzer Default(Type analyzable) => _defaults.Default(analyzable, typeof(IAnalyzable<>), typeof(AnalyzerAttribute), () => new Default());
        public IAnalyzer Get<T>() where T : struct, IAnalyzable => _analyzers.TryGet<T>(out var analyzer, true) ? analyzer : Default<T>();
        public IAnalyzer Get(Type analyzable) => _analyzers.TryGet(analyzable, out var analyzer, true) ? analyzer : Default(analyzable);
        public bool Has<T>() where T : struct, IAnalyzable => _analyzers.Has<T>(true);
        public bool Has(Type analyzable) => _analyzers.Has(analyzable, true);
        public bool Set<T>(Analyzer<T> analyzer) where T : struct, IAnalyzable => _analyzers.Set<T>(analyzer);
        public bool Set(Type analyzable, IAnalyzer analyzer) => _analyzers.Set(analyzable, analyzer);
        public bool Remove<T>() where T : struct, IAnalyzable => _analyzers.Remove<T>();
        public bool Remove(Type analyzable) => _analyzers.Remove(analyzable);
        public bool Clear()
        {
            var cleared = _defaults.Clear() | _analyzers.Clear() | _analyses.Count > 0;
            _analyses.Clear();
            return cleared;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<IAnalyzer> GetEnumerator() => _analyzers.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
