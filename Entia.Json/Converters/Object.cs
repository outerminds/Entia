using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Entia.Core;

namespace Entia.Json.Converters
{
    public sealed class DefaultObject : IConverter
    {
        sealed class Member
        {
            public readonly Node Name;
            public readonly Type Type;
            public readonly Func<object, object> Get;
            public readonly Action<object, object> Set;
            public IConverter Converter => _converter.Value;

            readonly Lazy<IConverter> _converter;

            public Member(FieldInfo field)
            {
                Name = Node.String(field.AutoProperty().Map(property => property.Name).Or(field.Name), Node.Tags.Plain);
                Type = field.FieldType;
                Get = field.GetValue;
                Set = field.SetValue;
                _converter = new Lazy<IConverter>(() => Converters.Converter.Default(field.FieldType));
            }
        }

        public Type Type { get; }

        readonly Func<object> _instantiate;
        readonly Member[] _members;
        readonly Dictionary<string, Member> _nameToMember = new Dictionary<string, Member>();

        public DefaultObject(Type type)
        {
            Type = type;
            _instantiate =
                type.IsAbstract ? new Func<object>(() => default) :
                type.DefaultInstance().TryValue(out var @default) ?
                    new Func<object>(() => CloneUtility.Shallow(@default)) :
                type.DefaultConstructor().TryValue(out var constructor) ?
                    new Func<object>(() => constructor.Invoke(Array.Empty<object>())) :
                new Func<object>(() => FormatterServices.GetUninitializedObject(type));
            _members = type.Fields(true, false).Select(field => new Member(field)).ToArray();
            _nameToMember = _members.ToDictionary(member => member.Name.AsString());
        }

        public Node Convert(in ToContext context)
        {
            var instance = context.Instance;
            var members = new Node[_members.Length * 2];
            for (int i = 0; i < _members.Length; i++)
            {
                var member = _members[i];
                members[i * 2] = member.Name;
                members[i * 2 + 1] = context.Convert(member.Get(instance), member.Type, member.Converter);
            }
            return Node.Object(members);
        }

        public void Initialize(ref object instance, in FromContext context)
        {
            var node = context.Node;
            foreach (var (key, value) in node.Members())
            {
                if (_nameToMember.TryGetValue(key, out var member))
                {
                    try { member.Set(instance, context.Convert(value, member.Type, member.Converter)); }
                    catch { }
                }
            }
        }

        public object Instantiate(in FromContext context) => _instantiate();
    }
}