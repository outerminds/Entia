using Entia.Builders;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Build;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Phases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Nodes
{
    public readonly struct Sequence : INode
    {
        sealed class Runner : IRunner
        {
            public object Instance => Children;
            public readonly IRunner[] Children;
            public Runner(params IRunner[] children) { Children = children; }

            public IEnumerable<Type> Phases() => Children.SelectMany(child => child.Phases());
            public IEnumerable<Phase> Phases(Controller controller) => Children.SelectMany(child => child.Phases(controller));
            public Option<Runner<T>> Specialize<T>(Controller controller) where T : struct, IPhase
            {
                var children = (items: new Runner<T>[Children.Length], count: 0);
                foreach (var child in Children)
                    if (child.Specialize<T>(controller).TryValue(out var special)) children.Push(special);

                switch (children.count)
                {
                    case 0: return Option.None();
                    case 1: return children.items[0];
                    default:
                        var runners = children.ToArray();
                        return new Runner<T>((in T phase) => { for (int i = 0; i < runners.Length; i++) runners[i].Run(phase); });
                }
            }
        }

        sealed class Builder : IBuilder
        {
            public Result<IRunner> Build(Node node, Node root, World world) => node.Children.Length == 1 ?
                Result.Cast<Sequence>(node.Value).Bind(_ => world.Builders().Build(node.Children[0], root)) :
                Result.Cast<Sequence>(node.Value)
                    .Bind(_ => node.Children.Select(child => world.Builders().Build(child, root)).All())
                    .Map(children => new Runner(children))
                    .Cast<IRunner>();
        }

        [Builder]
        static readonly Builder _builder = new Builder();
    }
}
