using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Json
{
    public static partial class Serialization
    {
        static readonly IEqualityComparer<object> _comparer = Equality.Comparer<object>(ReferenceEquals);

        public static Node Convert<T>(in T instance, Features features = Features.None, Container container = null) =>
            ToContext(features, container).Convert(instance);
        public static Node Convert(object instance, Type type, Features features = Features.None, Container container = null) =>
            ToContext(features, container).Convert(instance, type);

        static ConvertToContext ToContext(Features features, Container container = null) =>
            new ConvertToContext(features, container ?? new Container());
    }
}