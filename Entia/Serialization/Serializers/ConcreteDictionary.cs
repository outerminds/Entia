using System.Collections.Generic;
using Entia.Serialization;

namespace Entia.Serializers
{
    public sealed class ConcreteDictionary<TKey, TValue> : Serializer<Dictionary<TKey, TValue>>
    {
        public readonly Serializer<TKey[]> Keys;
        public readonly Serializer<TValue[]> Values;

        public ConcreteDictionary() { }
        public ConcreteDictionary(Serializer<TKey[]> keys = null, Serializer<TValue[]> values = null)
        {
            Keys = keys;
            Values = values;
        }

        public override bool Serialize(in Dictionary<TKey, TValue> instance, in SerializeContext context)
        {
            var keys = new TKey[instance.Count];
            var values = new TValue[instance.Count];
            var index = 0;
            foreach (var pair in instance) { keys[index] = pair.Key; values[index] = pair.Value; index++; }
            return context.Serialize(keys, Keys) && context.Serialize(values, Values);
        }

        public override bool Instantiate(out Dictionary<TKey, TValue> instance, in DeserializeContext context)
        {
            instance = new Dictionary<TKey, TValue>();
            return true;
        }

        public override bool Initialize(ref Dictionary<TKey, TValue> instance, in DeserializeContext context)
        {
            if (context.Deserialize(out TKey[] keys) &&
                context.Deserialize(out TValue[] values) &&
                keys.Length == values.Length)
            {
                for (int i = 0; i < keys.Length; i++) instance.Add(keys[i], values[i]);
                return true;
            }
            return false;
        }
    }
}