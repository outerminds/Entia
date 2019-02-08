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
    /// <summary>
    /// Tag interface that all nodes must implement.
    /// </summary>
    public interface INode : IAnalyzable, IBuildable { }
    public interface IWrapper : INode { }
    public interface IAtomic : INode { }

    public sealed class Node : IEquatable<Node>
    {
        public static Node Of(string name, INode value, params Node[] nodes) => new Node(name, value, nodes);
        public static Node Of<T>(string name, params Node[] nodes) where T : struct, INode => Of(name, default(T), nodes);
        public static Node Sequence(params Node[] nodes) => Sequence("", nodes);
        public static Node Sequence(string name, params Node[] nodes) => Of<Sequence>(name, nodes);
        public static Node Parallel(params Node[] nodes) => Parallel("", nodes);
        public static Node Parallel(string name, params Node[] nodes) => Of<Parallel>(name, nodes);
        public static Node Automatic(params Node[] nodes) => Automatic("", nodes);
        public static Node Automatic(string name, params Node[] nodes) => Of<Automatic>(name, nodes);
        public static Node System(Type system) => Of(system.Format(), new System(system));
        public static Node System<T>() where T : struct, ISystem => System(typeof(T));
        public static Node[] Systems(Assembly assembly) => assembly.GetTypes().Where(type => type.IsValueType && type.Is<ISystem>()).Select(System).ToArray();

        public readonly string Name;
        public readonly INode Value;
        public readonly Node[] Children;

        int? _hash;

        Node(string name, INode value, params Node[] children)
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
                (Name, Value) == (other.Name, other.Value) &&
                Children.SequenceEqual(other.Children);
        }
        public override bool Equals(object obj) => obj is Node node && Equals(node);
        public override int GetHashCode()
        {
            if (_hash is int hash) return hash;
            hash = (Name, Value).GetHashCode() ^ ArrayUtility.GetHashCode(Children);
            _hash = hash;
            return hash;
        }
        public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"[{string.Join(", ", Children.AsEnumerable())}]" : Name;
    }
}
