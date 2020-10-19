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
        /// <summary>
        /// Enables <see cref="Node"/>s to refer to another <see cref="Node"/> that represents the same
        /// value rather than converting it for each of its occurrence using the format:
        /// <code>{ "$i": identifier, "$v": value }</code> for the referee
        /// <code>{ "$r": identifier }</code> for the reference.
        /// </summary>
        /// <remarks>
        /// This feature also makes it possible to convert values with circular references.
        /// Note that this can make the json output to be incompatible with other json parsers.
        /// </remarks>
        Reference = 1 << 0,
        /// <summary>
        /// Enables <see cref="Node"/>s to represent abstract values such as interfaces or base
        /// classes using the format:
        /// <code>{ "$t": type, "$v": value }</code>
        /// Without this feature, any value that has a visible type that differs from it actual type
        /// will be converted to <see cref="Node.Null"/>.
        /// </summary>
        /// <remarks>
        /// Note that this can make the json output to be incompatible with other json parsers.
        /// </remarks>
        Abstract = 1 << 1,
    }

    /// <summary>
    /// Instructs the json generator to generate in a given style.
    /// </summary>
    public enum Formats
    {
        /// <summary>
        /// Generates json in the most compact way possible, stripping all unnecessary characters
        /// such as spaces.
        /// </summary>
        Compact = 0,
        /// <summary>
        /// Generates json in a human readable way by adding spaces and indentation.
        /// </summary>
        Indented = 1
    }

    public static class FeaturesExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAll(this Features features, Features others) => (features & others) == others;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAny(this Features features, Features others) => (features & others) != 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNone(this Features features, Features others) => !features.HasAny(others);
    }

    /// <summary>
    /// Data structure that allows to alter the parsing, conversion and generation behaviors
    /// of the library.
    /// Specifically, this is where <see cref="IConverter"/> instances can be registered to
    /// be used during conversion.
    /// </summary>
    public sealed class Settings
    {
        public static readonly Settings Default = new Settings(Features.None, Formats.Compact);

        public readonly Features Features;
        public readonly Formats Format;
        public readonly TypeMap<object, IConverter> Converters;

        public Settings(Features features, Formats formats, params IConverter[] converters)
        {
            Features = features;
            Format = formats;
            Converters = new TypeMap<object, IConverter>(converters.Select(converter => (converter.Type, converter)));
        }

        public IConverter Converter(Type type, IConverter @default = null, IConverter @override = null) =>
            @override ?? Converters.Get(type, out _, true, false) ?? @default ?? Json.Converters.Converter.Default(type);
        public IConverter Converter<T>(IConverter @default = null, IConverter @override = null) =>
            @override ?? Converters.Get<T>(out _, true, false) ?? @default ?? Json.Converters.Converter.Default<T>();

        public Settings With(Features? features = null, Formats? format = null, IConverter[] converters = null) =>
            new Settings(features ?? Features, format ?? Format, converters ?? Converters.Values.ToArray());
    }
}