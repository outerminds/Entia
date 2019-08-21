using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Experiment
{
    public static class TypeMapTest
    {
        [Serializable]
        public class Cyclic { public Cyclic A; }

        public static void Benchmark()
        {
            var dictionary = new Dictionary<Type, object>();
            var intDictionary = new Dictionary<int, object>();
            var concurrent = new ConcurrentDictionary<Type, object>();
            var map = new TypeMap<object, object>();
            var index = map.Index<int>();
            var value = default(object);
            void Add<T>(T current)
            {
                intDictionary[map.Index<T>()] = concurrent[typeof(T)] = dictionary[typeof(T)] = current; map[typeof(T)] = current;
            }
            Add(byte.MaxValue);
            Add(sbyte.MaxValue);
            Add(ushort.MaxValue);
            Add(short.MaxValue);
            Add(uint.MaxValue);
            Add(int.MaxValue);
            Add(ulong.MaxValue);
            Add(long.MaxValue);
            Add(float.MaxValue);
            Add(double.MaxValue);
            Add(decimal.MaxValue);
            Add(new object());
            Add(new Unit());
            Add(new Cyclic());
            Add(DateTime.MaxValue);
            Add(TimeSpan.MaxValue);
            Add(dictionary);
            Add(intDictionary);
            Add(concurrent);
            Add(map);
            var array = map.Values.ToArray();

            void ArrayGet() => value = array[index];
            void DictionaryIndexer<T>() => value = dictionary[typeof(T)];
            void DictionaryTryGet<T>() => dictionary.TryGetValue(typeof(T), out value);
            void IntDictionaryIndexer<T>() => value = intDictionary[map.Index<T>()];
            void IntDictionaryTryGet<T>() => intDictionary.TryGetValue(map.Index<T>(), out value);
            void IntDictionaryTryGetIndex() => intDictionary.TryGetValue(index, out value);
            void ConcurrentIndexer<T>() => value = concurrent[typeof(T)];
            void ConcurrentTryGet<T>() => concurrent.TryGetValue(typeof(T), out value);
            void MapIndexer<T>() => value = map[typeof(T)];
            void MapGet<T>() => value = map.Get<T>(out _);
            void MapGetIndex() => value = map[index];
            void MapTryGet<T>() => map.TryGet(typeof(T), out value);
            void MapTryGetT<T>() => map.TryGet<T>(out value);
            void MapTryGetIndex() => map.TryGet(index, out value);

            while (true)
            {
                Test.Measure(DictionaryIndexer<int>, new Action[]
                {
                    ArrayGet,
                    DictionaryTryGet<int>,
                    DictionaryTryGet<Action>,
                    IntDictionaryIndexer<int>,
                    IntDictionaryTryGet<int>,
                    IntDictionaryTryGet<Action>,
                    IntDictionaryTryGetIndex,
                    ConcurrentIndexer<int>,
                    ConcurrentTryGet<int>,
                    ConcurrentTryGet<Action>,
                    MapIndexer<int>,
                    MapGet<int>,
                    MapGet<Action>,
                    MapGetIndex,
                    MapTryGet<int>,
                    MapTryGet<Action>,
                    MapTryGetT<int>,
                    MapTryGetT<Action>,
                    MapTryGetIndex
                }, 100_000);
                Console.WriteLine();
            }
        }
    }
}