using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Json
{
    public static partial class Serialization
    {
        public static Result<T> Deserialize<T>(string json, Settings settings = null)
        {
            settings ??= Settings.Default;
            var result = Parse(json, settings, out var references);
            if (result.TryValue(out var node))
                return new ConvertFromContext(settings, references).Convert<T>(node);
            return result.AsFailure();
        }

        public static Result<object> Deserialize(string json, Type type, Settings settings)
        {
            settings ??= Settings.Default;
            var result = Parse(json, settings, out var references);
            if (result.TryValue(out var node))
                return new ConvertFromContext(settings, references).Convert(node, type);
            return result.AsFailure();
        }

        public static T Instantiate<T>(Node node, Settings settings = null) =>
            new ConvertFromContext(settings ?? Settings.Default, new Dictionary<uint, object>()).Convert<T>(node);
        public static object Instantiate(Node node, Type type, Settings settings = null) =>
            new ConvertFromContext(settings ?? Settings.Default, new Dictionary<uint, object>()).Convert(node, type);
    }
}