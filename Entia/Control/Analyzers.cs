using Entia.Core;
using Entia.Dependencies;
using Entia.Modules;
using Entia.Nodes;
using System;
using System.Linq;

namespace Entia.Analyzers
{
	public sealed class System : Analyzer<Nodes.System>
	{
		static IDependency[] Dependencies(Type type, World world) => world.Dependers().Dependencies(type);

		public override Result<IDependency[]> Analyze(Nodes.System data, Node node, Node root, World world)
		{
			var dependencies = Dependencies(data.Type, world);
			var emits = dependencies.Emits().ToArray();
			if (emits.Length > 0)
			{
				var direct = root.Family()
					.Select(child => Option.Cast<Nodes.System>(child.Value).Map(system => (child, system)))
					.Choose()
					.Select(pair => (pair.child, dependencies: Dependencies(pair.system.Type, world)))
					.ToArray();
				dependencies = direct
					.Where(pair => pair.dependencies.Reacts().Intersect(emits).Any())
					.SelectMany(pair => pair.dependencies)
					.Prepend(dependencies)
					.Distinct()
					.ToArray();
			}

			return dependencies;
		}
	}

	public sealed class Parallel : Analyzer<Nodes.Parallel>
	{
		Result<Unit> Unknown(Node node, IDependency[] dependencies) =>
			dependencies.OfType<Unknown>().Select(_ => Result.Failure($"'{node}' has unknown dependencies.")).All();

		Result<Unit> WriteWrite((Node node, IDependency[] dependencies) left, (Node node, IDependency[] dependencies) right) =>
			left.dependencies.Writes()
				.Intersect(right.dependencies.Writes())
				.Select(type => Result.Failure($"'{left.node}' and '{right.node}' both have a write dependency on type '{type.FullFormat()}'."))
				.All();

		Result<Unit> WriteRead((Node node, IDependency[] dependencies) left, (Node node, IDependency[] dependencies) right) =>
			left.dependencies.Writes()
				.Intersect(right.dependencies.Reads())
				.Select(type => Result.Failure($"'{left.node}' has a write dependency on type '{type.FullFormat()}' and '{right.node}' reads from it."))
				.All();

		public override Result<IDependency[]> Analyze(Nodes.Parallel data, Node node, Node root, World world) =>
			node.Children.Select(child => world.Analyzers().Analyze(child, root).Map(dependencies => (child, dependencies))).All().Bind(children =>
			{
				var combinations = children.Combinations(2).ToArray();
				var unknown = children.Select(pair => Unknown(pair.child, pair.dependencies)).All();
				var writeWrite = combinations.Select(pairs => WriteWrite(pairs[0], pairs[1])).All();
				var writeRead = combinations.Select(pairs => WriteRead(pairs[0], pairs[1])).All();
				var readWrite = combinations.Select(pairs => WriteRead(pairs[1], pairs[0])).All();

				return Result.All(unknown, writeWrite, writeRead, readWrite)
					.Map(__ => children.SelectMany(pair => pair.dependencies)
					.ToArray());
			});
	}
}
