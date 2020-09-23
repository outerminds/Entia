using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Json
{
    public static partial class Serialization
    {
        static readonly IEqualityComparer<object> _comparer = Equality.Comparer<object>(ReferenceEquals);

        public static string Serialize<T>(in T instance, Settings settings = null)
        {
            var context = new ToContext(settings ?? Settings.Default);
            var node = context.Convert(instance);
            return Generate(node, context);
        }

        public static string Serialize(object instance, Type type, Settings settings = null)
        {
            var context = new ToContext(settings ?? Settings.Default);
            var node = context.Convert(instance, type);
            return Generate(node, context);
        }

        public static Node Convert<T>(in T instance, Settings settings = null) =>
            new ToContext(settings ?? Settings.Default).Convert(instance);
        public static Node Convert(object instance, Type type, Settings settings = null) =>
            new ToContext(settings ?? Settings.Default).Convert(instance, type);
    }
}