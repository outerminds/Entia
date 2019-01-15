using Entia.Core;
using Entia.Modules.Analysis;
using Entia.Modules.Build;
using Entia.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Nodes
{
    public sealed class Node : IEquatable<Node>
    {
        public interface IData : IAnalyzable, IBuildable { }

        public static Node Of(string name, IData value, params Node[] nodes) => new Node(name, value, nodes);
        public static Node Of<T>(string name, params Node[] nodes) where T : struct, IData => Of(name, default(T), nodes);
        public static Node Sequence(params Node[] nodes) => Sequence("", nodes);
        public static Node Sequence(string name, params Node[] nodes) => Of<Sequence>(name, nodes);
        public static Node Parallel(params Node[] nodes) => Parallel("", nodes);
        public static Node Parallel(string name, params Node[] nodes) => Of<Parallel>(name, nodes);
        public static Node Automatic(params Node[] nodes) => Automatic("", nodes);
        public static Node Automatic(string name, params Node[] nodes) => Of<Automatic>(name, nodes);
        public static Node System(Type system) => Of(system.Format(), new System { Type = system });
        public static Node System<T>() where T : struct, ISystem => System(typeof(T));
        public static Node[] Systems(Assembly assembly) => assembly.GetTypes().Where(type => type.IsValueType && type.Is<ISystem>()).Select(System).ToArray();
        public static Node Resolve() => Of(nameof(Nodes.Resolve), new Resolve());

        public readonly string Name;
        public readonly IData Value;
        public readonly Node[] Children;

        Node(string name, IData value, params Node[] children)
        {
            Name = name;
            Value = value;
            Children = children;
        }

        public bool Equals(Node other) => Name == other.Name && EqualityComparer<IData>.Default.Equals(Value, other.Value) && Children.SequenceEqual(other.Children);
        public override bool Equals(object obj) => obj is Node node && Equals(node);
        public override int GetHashCode() => Children.Aggregate(Name.GetHashCode() ^ EqualityComparer<IData>.Default.GetHashCode(Value), (hash, child) => hash ^ child.GetHashCode());
        public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"[{string.Join(", ", Children.AsEnumerable())}]" : Name;
    }
}
