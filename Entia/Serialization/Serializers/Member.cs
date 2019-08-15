using System;
using System.Reflection;
using Entia.Core;
using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class Field<T, TValue> : IMember<T>
    {
        public delegate ref readonly TValue Getter(in T instance);

        public readonly Getter Get;
        public readonly Serializer<TValue> Serializer;

        public Field(Getter get, Serializer<TValue> serializer = null)
        {
            Get = get;
            Serializer = serializer;
        }

        public bool Serialize(in T instance, in SerializeContext context) => context.Serialize(Get(instance), Serializer);
        public bool Deserialize(ref T instance, in DeserializeContext context)
        {
            if (context.Deserialize(out TValue value, Serializer))
            {
                UnsafeUtility.Set(Get(instance), value);
                return true;
            }
            return false;
        }
    }

    public sealed class Property<T, TValue> : IMember<T>
    {
        public delegate TValue Getter(in T instance);
        public delegate void Setter(ref T instance, in TValue value);

        public readonly Getter Get;
        public readonly Setter Set;
        public readonly Serializer<TValue> Serializer;

        public Property(Getter get, Setter set, Serializer<TValue> serializer = null)
        {
            Get = get;
            Set = set;
            Serializer = serializer;
        }

        public bool Serialize(in T instance, in SerializeContext context) => context.Serialize(Get(instance), Serializer);
        public bool Deserialize(ref T instance, in DeserializeContext context)
        {
            if (context.Deserialize(out TValue value, Serializer))
            {
                Set(ref instance, value);
                return true;
            }
            return false;
        }
    }

    public sealed class Reflection : IMember
    {
        public readonly Type Type;
        public readonly Func<object, object> Get;
        public readonly Action<object, object> Set;
        public readonly MemberInfo Member;
        public readonly ISerializer Serializer;

        public Reflection(FieldInfo field, ISerializer serializer = null)
        {
            Type = field.FieldType;
            Get = field.GetValue;
            Set = field.SetValue;
            Member = field;
            Serializer = serializer;
        }

        public Reflection(PropertyInfo property, ISerializer serializer = null)
        {
            Type = property.PropertyType;
            Get = property.GetValue;
            Set = property.SetValue;
            Member = property;
            Serializer = serializer;
        }

        public bool Serialize(object instance, in SerializeContext context) => context.Serialize(Get(instance), Type, Serializer);
        public bool Deserialize(object instance, in DeserializeContext context)
        {
            if (context.Deserialize(out var value, Type, Serializer))
            {
                Set(instance, value);
                return true;
            }
            return false;
        }
    }

    public interface IMember
    {
        bool Serialize(object instance, in SerializeContext context);
        bool Deserialize(object instance, in DeserializeContext context);
    }

    public interface IMember<T>
    {
        bool Serialize(in T instance, in SerializeContext context);
        bool Deserialize(ref T instance, in DeserializeContext context);
    }
}