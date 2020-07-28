using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Json
{
    public static partial class Serialization
    {
        public static Result<T> Deserialize<T>(string json, Features features = Features.None, Container container = null)
        {
            var result = Parse(json, features, out var references);
            if (result.TryValue(out var node)) return FromContext(references, container).Convert<T>(node);
            return result.AsFailure();
        }

        public static Result<object> Deserialize(string json, Type type, Features features = Features.None, Container container = null)
        {
            var result = Parse(json, features, out var references);
            if (result.TryValue(out var node)) return FromContext(references, container).Convert(node, type);
            return result.AsFailure();
        }

        public static T Instantiate<T>(Node node, Container container = null) =>
            FromContext(null, container).Convert<T>(node);
        public static object Instantiate(Node node, Type type, Container container = null) =>
            FromContext(null, container).Convert(node, type);

        static ConvertFromContext FromContext(Dictionary<uint, object> references = null, Container container = null) =>
            new ConvertFromContext(references ?? new Dictionary<uint, object>(), container ?? new Container());
    }
}