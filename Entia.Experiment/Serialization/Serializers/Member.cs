using System;
using System.Reflection;
using Entia.Core;
using Entia.Experiment.Serializationz;

namespace Entia.Experiment
{
    public sealed class Field<T, TValue> : IMember<T>
    {
        public delegate ref readonly TValue Getter(in T instance);

        public readonly Getter Get;

        public Field(Getter get) { Get = get; }

        public bool Serialize(in T instance, in SerializeContext context) => context.Serialize(Get(instance));
        public bool Deserialize(ref T instance, in DeserializeContext context)
        {
            if (context.Deserialize(out TValue value))
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

        public Property(Getter get, Setter set) { Get = get; Set = set; }

        public bool Serialize(in T instance, in SerializeContext context) => context.Serialize(Get(instance));
        public bool Deserialize(ref T instance, in DeserializeContext context)
        {
            if (context.Deserialize(out TValue value))
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

        public Reflection(FieldInfo field)
        {
            Type = field.FieldType;
            Get = field.GetValue;
            Set = field.SetValue;
        }

        public Reflection(PropertyInfo property)
        {
            Type = property.PropertyType;
            Get = property.GetValue;
            Set = property.SetValue;
        }

        public bool Serialize(object instance, in SerializeContext context) =>
            context.Serialize(Get(instance), Type);

        public bool Deserialize(object instance, in DeserializeContext context)
        {
            if (context.Deserialize(out var value, Type))
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