using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Json.Converters;

namespace Entia.Json
{
    [Flags]
    public enum Features
    {
        None = 0,
        All = ~0,
        Reference = 1 << 0,
        Abstract = 1 << 1,
    }

    public enum Formats { Compact = 0, Indented = 1 }

    public static class FeaturesExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(this Features features, Features others) => (features & others) == others;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAny(this Features features, Features others) => (features & others) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNone(this Features features, Features others) => !features.HasAny(others);
    }

    public sealed class Settings
    {
        public static readonly Settings Default = new Settings(Features.None, Formats.Compact);

        public readonly Features Features;
        public readonly Formats Format;
        public readonly TypeMap<object, IConverter> Converters;

        Settings(Features features, Formats formats, params IConverter[] converters)
        {
            Features = features;
            Format = formats;
            Converters = new TypeMap<object, IConverter>(converters.Select(converter => (converter.Type, converter)));
        }

        public IConverter Converter(Type type, IConverter @default = null, IConverter @override = null) =>
            @override ??
            (Converters.TryGet(type, out var converter) ? converter : default) ??
            @default ??
            Json.Converters.Converter.Default(type);
        public IConverter Converter<T>(IConverter @default = null, IConverter @override = null) =>
            @override ??
            (Converters.TryGet<T>(out var converter) ? converter : default) ??
            @default ??
            Json.Converters.Converter.Default<T>();

        public Settings With(Features? features = null, Formats? format = null, IConverter[] converters = null) =>
            new Settings(features ?? Features, format ?? Format, converters ?? Converters.Values.ToArray());
    }
}