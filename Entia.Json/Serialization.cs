using System;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Json
{
    /// <summary>
    /// Main module. Defines parsing, conversion and generation operations.
    /// </summary>
    public static partial class Serialization
    {
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

        public static Result<T> Deserialize<T>(string json, Settings settings = null)
        {
            var context = new FromContext(settings ?? Settings.Default, new Dictionary<uint, object>());
            var result = Parse(json, context);
            if (result.TryValue(out var node)) return context.Convert<T>(node);
            return result.Fail();
        }

        public static Result<object> Deserialize(string json, Type type, Settings settings = null)
        {
            var context = new FromContext(settings ?? Settings.Default, new Dictionary<uint, object>());
            var result = Parse(json, context);
            if (result.TryValue(out var node)) return context.Convert(node, type);
            return result.Fail();
        }

        public static Node Convert<T>(in T instance, Settings settings = null) =>
            new ToContext(settings ?? Settings.Default).Convert(instance);
        public static Node Convert(object instance, Type type, Settings settings = null) =>
            new ToContext(settings ?? Settings.Default).Convert(instance, type);
        public static T Instantiate<T>(Node node, Settings settings = null) =>
            new FromContext(settings ?? Settings.Default, new Dictionary<uint, object>()).Convert<T>(node);
        public static object Instantiate(Node node, Type type, Settings settings = null) =>
            new FromContext(settings ?? Settings.Default, new Dictionary<uint, object>()).Convert(node, type);
        public static string Generate(Node node, Settings settings = null) =>
            Generate(node, new ToContext(settings ?? Settings.Default));
        public static Result<Node> Parse(string text, Settings settings = null) =>
            Parse(text, new FromContext(settings ?? Settings.Default, new Dictionary<uint, object>()));
    }
}