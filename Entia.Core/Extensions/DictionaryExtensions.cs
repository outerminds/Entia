using System;
using System.Collections.Generic;

namespace Entia.Core
{
	public static class DictionaryExtensions
	{
		public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, in TKey key, Func<TValue> provide) =>
			dictionary.TryGetValue(key, out var value) ? value :
			dictionary[key] = provide();
	}
}
