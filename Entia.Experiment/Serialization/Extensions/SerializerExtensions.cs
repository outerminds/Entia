namespace Entia.Experiment
{
    public static class SerializerExtensions
    {
        public static bool Serialize<T>(this ISerializer serializer, in T instance, SerializeContext context)
        {
            if (serializer is Serializer<T> casted) return casted.Serialize(instance, context);
            return serializer.Serialize(instance, context);
        }

        public static bool Deserialize<T>(this ISerializer serializer, out T instance, DeserializeContext context)
        {
            if (serializer is Serializer<T> casted) return casted.Deserialize(out instance, context);
            else if (serializer.Deserialize(out var value, context))
            {
                instance = (T)value;
                return true;
            }
            else
            {
                instance = default;
                return false;
            }
        }

        public static bool Instantiate<T>(this ISerializer serializer, out T instance, DeserializeContext context)
        {
            if (serializer is Serializer<T> casted) return casted.Instantiate(out instance, context);
            else if (serializer.Instantiate(out var value, context))
            {
                instance = (T)value;
                return true;
            }
            else
            {
                instance = default;
                return false;
            }
        }

        public static bool Initialize<T>(this ISerializer serializer, ref T instance, DeserializeContext context)
        {
            if (serializer is Serializer<T> casted) return casted.Initialize(ref instance, context);

            object value = instance;
            if (serializer.Initialize(ref value, context))
            {
                instance = (T)value;
                return true;
            }
            else
            {
                instance = default;
                return false;
            }
        }

        public static bool Clone<T>(this ISerializer serializer, in T instance, out T clone, CloneContext context)
        {
            if (serializer is Serializer<T> casted) return casted.Clone(instance, out clone, context);
            else if (serializer.Clone(instance, out var value, context))
            {
                clone = (T)value;
                return true;
            }
            else
            {
                clone = default;
                return false;
            }
        }

        public static bool Deserialize(this ISerializer serializer, out object instance, DeserializeContext context) =>
            serializer.Instantiate(out instance, context) && serializer.Initialize(ref instance, context);

        public static bool Deserialize<T>(this Serializer<T> serializer, out T instance, DeserializeContext context) =>
            serializer.Instantiate(out instance, context) && serializer.Initialize(ref instance, context);
    }
}