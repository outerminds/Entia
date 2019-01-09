using System;

namespace Entia.Core
{
	public static class Nullable
	{
		public static T? Value<T>(T value) where T : struct => value;
		public static T? Null<T>() where T : struct => null;
	}

	public static class NullableExtensions
	{
		public static bool TryValue<T>(in this T? source, out T value) where T : struct
		{
			if (source.HasValue)
			{
				value = source.Value;
				return true;
			}

			value = default;
			return false;
		}

		public static TOut Match<TIn, TOut>(in this TIn? source, Func<TIn, TOut> value, Func<TOut> @null) where TIn : struct =>
			source.HasValue ? value(source.Value) : @null();

		public static TOut Match<TIn, TOut>(in this TIn? source, Func<TIn, TOut> value, TOut @null) where TIn : struct =>
			source.HasValue ? value(source.Value) : @null;
	}
}
