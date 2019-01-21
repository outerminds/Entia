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
        public static Node System(Type system) => Of(system.Format(), new System(system));
        public static Node System<T>() where T : struct, ISystem => System(typeof(T));
        public static Node[] Systems(Assembly assembly) => assembly.GetTypes().Where(type => type.IsValueType && type.Is<ISystem>()).Select(System).ToArray();
        public static Node Resolve(Node node) => Of<Resolve>(nameof(Nodes.Resolve), node);

        public readonly string Name;
        public readonly IData Value;
        public readonly Node[] Children;

        int? _hash;

        Node(string name, IData value, params Node[] children)
        {
            Name = name;
            Value = value;
            Children = children;
        }

        public bool Equals(Node other)
        {
            if (ReferenceEquals(this, other)) return true;
            return
                GetHashCode() == other.GetHashCode() &&
                Name == other.Name &&
                EqualityComparer<IData>.Default.Equals(Value, other.Value) &&
                Children.SequenceEqual(other.Children);
        }
        public override bool Equals(object obj) => obj is Node node && Equals(node);
        public override int GetHashCode()
        {
            if (_hash.HasValue) return _hash.Value;
            _hash = Children.Aggregate(Name.GetHashCode() ^ EqualityComparer<IData>.Default.GetHashCode(Value), (hash, child) => hash ^ child.GetHashCode());
            return _hash.Value;
        }
        public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"[{string.Join(", ", Children.AsEnumerable())}]" : Name;
    }
}
