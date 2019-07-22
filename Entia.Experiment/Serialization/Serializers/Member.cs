using System;
using System.Reflection;
using Entia.Core;

namespace Entia.Experiment
{
    public sealed class Field<T, TValue> : IMember<T>
    {
        public delegate ref readonly TValue Getter(in T instance);

        public readonly Getter Get;
        public readonly Serializer<TValue> Value;

        public Field(Getter get, Serializer<TValue> value)
        {
            Get = get;
            Value = value;
        }

        public bool Serialize(in T instance, in SerializeContext context) =>
            Value.Serialize(Get(instance), context);
        public bool Deserialize(in T instance, in DeserializeContext context)
        {
            if (Value.Deserialize(out var value, context))
            {
                UnsafeUtility.Set(Get(instance), value);
                return true;
            }
            return false;
        }
        public bool Clone(in T instance, ref T clone, in CloneContext context)
        {
            if (Value.Clone(Get(instance), out var value, context))
            {
                UnsafeUtility.Set(Get(clone), value);
                return true;
            }
            return false;
        }
    }

    public sealed class Reflection : IMember
    {
        public readonly Func<object, object> Get;
        public readonly Action<object, object> Set;
        public readonly ISerializer Value;

        public Reflection(FieldInfo field, ISerializer value)
        {
            Get = field.GetValue;
            Set = field.SetValue;
            Value = value;
        }

        public Reflection(PropertyInfo property, ISerializer value)
        {
            Get = property.GetValue;
            Set = property.SetValue;
            Value = value;
        }

        public bool Serialize(object instance, in SerializeContext context) =>
            Value.Serialize(Get(instance), context);

        public bool Deserialize(object instance, in DeserializeContext context)
        {
            if (Value.Deserialize(out var value, context))
            {
                Set(instance, value);
                return true;
            }
            return false;
        }

        public bool Clone(object instance, ref object clone, in CloneContext context)
        {
            if (Value.Clone(Get(instance), out var value, context))
            {
                Set(clone, value);
                return true;
            }
            return false;
        }
    }

    public sealed class Reflection<T> : IMember<T>
    {
        public readonly Func<object, object> Get;
        public readonly Action<object, object> Set;
        public readonly ISerializer Serializer;

        public Reflection(FieldInfo field, ISerializer serializer)
        {
            Get = field.GetValue;
            Set = field.SetValue;
            Serializer = serializer;
        }

        public Reflection(PropertyInfo property, ISerializer serializer)
        {
            Get = property.GetValue;
            Set = property.SetValue;
            Serializer = serializer;
        }

        public bool Serialize(in T instance, in SerializeContext context) =>
            Serializer.Serialize(Get(instance), context);

        public bool Deserialize(in T instance, in DeserializeContext context)
        {
            if (Serializer.Deserialize(out var value, context))
            {
                Set(instance, value);
                return true;
            }
            return false;
        }

        public bool Clone(in T instance, ref T clone, in CloneContext context)
        {
            if (Serializer.Clone(Get(instance), out var value, context))
            {
                Set(clone, value);
                return true;
            }
            return false;
        }
    }

    public interface IMember
    {
        bool Serialize(object instance, in SerializeContext context);
        bool Deserialize(object instance, in DeserializeContext context);
        bool Clone(object instance, ref object clone, in CloneContext context);
    }

    public interface IMember<T>
    {
        bool Serialize(in T instance, in SerializeContext context);
        bool Deserialize(in T instance, in DeserializeContext context);
        bool Clone(in T instance, ref T clone, in CloneContext context);
    }
}