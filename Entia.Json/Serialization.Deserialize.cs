using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Json
{
    public static partial class Serialization
    {
        public static Result<T> Deserialize<T>(string json, Settings settings = null)
        {
            var context = new FromContext(settings ?? Settings.Default, new Dictionary<uint, object>());
            var result = Parse(json, context);
            if (result.TryValue(out var node)) return context.Convert<T>(node);
            return result.Fail();
        }

        public static Result<object> Deserialize(string json, Type type, Settings settings)
        {
            var context = new FromContext(settings ?? Settings.Default, new Dictionary<uint, object>());
            var result = Parse(json, context);
            if (result.TryValue(out var node)) return context.Convert(node, type);
            return result.Fail();
        }

        public static T Instantiate<T>(Node node, Settings settings = null) =>
            new FromContext(settings ?? Settings.Default, new Dictionary<uint, object>()).Convert<T>(node);
        public static object Instantiate(Node node, Type type, Settings settings = null) =>
            new FromContext(settings ?? Settings.Default, new Dictionary<uint, object>()).Convert(node, type);
    }
}