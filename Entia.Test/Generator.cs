using FsCheck;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Test
{
	public static class Generator
	{
		public static Gen<T> Wrap<T, U>(this Gen<T> generator, Func<T, Gen<U>> map) =>
			generator.SelectMany(value => map(value).Select(_ => value));

		public static Gen<T> Do<T>(this Gen<T> generator, Action<T> action) =>
			generator.Select(value => { action(value); return value; });

		public static Gen<object> Do(System.Action action) => Default<object>().Do(_ => action());

		public static Gen<T> Lazy<T>(Func<Gen<T>> provider) => Gen.Constant<object>(null).SelectMany(_ => provider());
		public static Gen<int> Integer() => Gen.Choose(0, int.MaxValue);
		public static Gen<float> Float() => Gen.Two(Integer()).Select(pair => pair.Item1 / (float)pair.Item2);
		public static Gen<char> Character() => Gen.Choose(char.MinValue, char.MaxValue).Select(value => (char)value);
		public static Gen<string> String() => Gen.ArrayOf(Character()).Select(array => new string(array));
		public static Gen<Type> MakeType(Type type, Gen<Type[]> parameters) => parameters.Select(types => type.MakeGenericType(types));
		public static Gen<T> Activate<T>(this Gen<Type> generator, Gen<object[]> arguments) =>
			generator.SelectMany(type => arguments.Select(values => (T)Activator.CreateInstance(type, values)));

		public static Gen<TTo> Cast<TFrom, TTo>(this Gen<TFrom> generator) => generator.Select(value => (TTo)(object)value);
		public static Gen<object> Box<T>(this Gen<T> generator) => generator.Cast<T, object>();
		public static Gen<T> Default<T>() => Gen.Constant<T>(default);

		public static Gen<T> Frequency<T>(IEnumerable<(int, Gen<T>)> generators) =>
			Gen.Frequency(generators.Select(pair => Tuple.Create(pair.Item1, pair.Item2)));
		public static Gen<T> Frequency<T>(params (int, Gen<T>)[] generators) => Frequency(generators.AsEnumerable());
	}
}
