using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Experiment
{
    public sealed class ConcreteDictionary : Serializer<IDictionary>
    {
        public readonly Type Key;
        public readonly Type Value;
        public ConcreteDictionary(Type key, Type value) { Key = key; Value = value; }

        static IDictionary Instantiate(Type key, Type value, int capacity = 0) =>
            Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(key, value), capacity) as IDictionary;

        public override bool Serialize(in IDictionary instance, in SerializeContext context)
        {
            context.Writer.Write(instance.Count);
            foreach (var key in instance.Keys)
            {
                if (context.Descriptors.Serialize(key, Key, context) &&
                    context.Descriptors.Serialize(instance[key], Value, context))
                    continue;
                return false;
            }
            return true;
        }

        public override bool Instantiate(out IDictionary instance, in DeserializeContext context)
        {
            instance = Instantiate(Key, Value);
            return true;
        }

        public override bool Initialize(ref IDictionary instance, in DeserializeContext context)
        {
            if (context.Reader.Read(out int count))
            {
                for (int i = 0; i < count; i++)
                {
                    if (context.Descriptors.Deserialize(out var key, Key, context) &&
                        context.Descriptors.Deserialize(out var value, Value, context))
                        instance.Add(key, value);
                    else return false;
                }
                return true;
            }
            return false;
        }
    }

    public sealed class ConcreteDictionary<TKey, TValue> : Serializer<Dictionary<TKey, TValue>>
    {
        public override bool Serialize(in Dictionary<TKey, TValue> instance, in SerializeContext context)
        {
            var keys = new TKey[instance.Count];
            var values = new TValue[instance.Count];
            var index = 0;
            foreach (var pair in instance) { keys[index] = pair.Key; values[index] = pair.Value; index++; }
            return context.Descriptors.Serialize(keys, context) && context.Descriptors.Serialize(values, context);
        }

        public override bool Instantiate(out Dictionary<TKey, TValue> instance, in DeserializeContext context)
        {
            instance = new Dictionary<TKey, TValue>();
            return true;
        }

        public override bool Initialize(ref Dictionary<TKey, TValue> instance, in DeserializeContext context)
        {
            if (context.Descriptors.Deserialize(out TKey[] keys, context) &&
                context.Descriptors.Deserialize(out TValue[] values, context) &&
                keys.Length == values.Length)
            {
                for (int i = 0; i < keys.Length; i++) instance.Add(keys[i], values[i]);
                return true;
            }
            return false;
        }
    }
}