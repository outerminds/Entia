using System;
using Entia.Core;

namespace Entia.Json.Converters
{
    /// <summary>
    /// Data structure that represents a member of an object converter.
    /// </summary>
    /// <remarks>
    /// See <see cref="Converter.Object{T}"/>.
    /// </remarks>
    public sealed class Member<T>
    {
        public readonly string Name;
        public readonly string[] Aliases;
        public readonly Member.Convert<T> Convert;
        public readonly Member.Initialize<T> Initialize;
        public readonly Node Key;

        public Member(string name, string[] aliases, Member.Convert<T> convert, Member.Initialize<T> initialize)
        {
            Name = name;
            Aliases = aliases;
            Convert = convert;
            Initialize = initialize;
            Key = name;
        }
    }

    /// <summary>
    /// Module that exposes constructors for <see cref="Member{T}"/>.
    /// </summary>
    /// <remarks>
    /// See <see cref="Converter.Object{T}"/>.
    /// </remarks>
    public static class Member
    {
        public delegate bool Validate<T>(in T instance);
        public delegate Node Convert<T>(in T instance, in ToContext context);
        public delegate void Initialize<T>(ref T instance, in FromContext context);
        public delegate ref readonly TValue Get<T, TValue>(in T instance);
        public delegate TValue Getter<T, TValue>(in T instance);
        public delegate void Setter<T, TValue>(ref T instance, in TValue value);

        public static Member<T> Field<T, TValue>(string name, Get<T, TValue> get, Validate<TValue> validate = null, string[] aliases = null, Converter<TValue> converter = null)
        {
            validate ??= (in TValue _) => true;
            aliases ??= Array.Empty<string>();
            return new Member<T>(name, aliases,
                (in T instance, in ToContext context) =>
                {
                    ref readonly var value = ref get(instance);
                    if (validate(value)) return context.Convert(value, converter, converter);
                    return default;
                },
                (ref T instance, in FromContext context) =>
                {
                    var value = context.Convert<TValue>(context.Node, converter, converter);
                    if (validate(value)) UnsafeUtility.Set(get(instance), value);
                });
        }

        public static Member<T> Property<T, TValue>(string name, Getter<T, TValue> get, Setter<T, TValue> set, Validate<TValue> validate = null, string[] aliases = null, Converter<TValue> converter = null)
        {
            validate ??= (in TValue _) => true;
            aliases ??= Array.Empty<string>();
            return new Member<T>(name, aliases,
                (in T instance, in ToContext context) =>
                {
                    var value = get(instance);
                    if (validate(value)) return context.Convert(value, converter, converter);
                    return default;
                },
                (ref T instance, in FromContext context) =>
                {
                    var value = context.Convert<TValue>(context.Node, converter, converter);
                    if (validate(value)) set(ref instance, value);
                });
        }
    }
}