using System;
using System.Runtime.CompilerServices;
using Entia.Core;

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
        public static readonly Settings Default = new Settings(Features.None, Formats.Compact, new Container());

        public readonly Features Features;
        public readonly Formats Format;
        public readonly Container Container;

        Settings(Features features, Formats formats, Container container)
        {
            Features = features;
            Format = formats;
            Container = container;
        }

        public Settings With(Features? features = null, Formats? format = null, Container container = null) =>
            new Settings(features ?? Features, format ?? Format, container ?? Container);
    }
}