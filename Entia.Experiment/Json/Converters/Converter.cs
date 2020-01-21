using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Entia.Core;

namespace Entia.Json.Converters
{
    [Implementation(typeof(Node), typeof(ConcreteNode))]
    [Implementation(typeof(Type), typeof(AbstractType))]
    [Implementation(typeof(DateTime), typeof(ConcreteDateTime))]
    [Implementation(typeof(TimeSpan), typeof(ConcreteTimeSpan))]
    [Implementation(typeof(Guid), typeof(ConcreteGuid))]
    [Implementation(typeof(Array), typeof(Providers.Array))]
    [Implementation(typeof(List<>), typeof(Providers.List))]
    [Implementation(typeof(IDictionary<,>), typeof(Providers.Dictionary))]
    [Implementation(typeof(IDictionary), typeof(AbstractDictionary))]
    [Implementation(typeof(IEnumerable), typeof(Providers.Enumerable))]
    [Implementation(typeof(ISerializable), typeof(AbstractSerializable))]
    public interface IConverter : ITrait
    {
        bool CanConvert(TypeData type);
        Node Convert(in ConvertToContext context);
        object Instantiate(in ConvertFromContext context);
        void Initialize(ref object instance, in ConvertFromContext context);
    }

    public abstract class Converter<T> : IConverter
    {
        public virtual bool CanConvert(TypeData type) => true;
        public abstract Node Convert(in T instance, in ConvertToContext context);
        public abstract T Instantiate(in ConvertFromContext context);
        public virtual void Initialize(ref T instance, in ConvertFromContext context) { }

        Node IConverter.Convert(in ConvertToContext context) =>
            context.Instance is T casted ? Convert(casted, context) : Node.Null;
        object IConverter.Instantiate(in ConvertFromContext context) => Instantiate(context);
        void IConverter.Initialize(ref object instance, in ConvertFromContext context)
        {
            if (instance is T casted)
            {
                Initialize(ref casted, context);
                instance = casted;
            }
        }
    }
}