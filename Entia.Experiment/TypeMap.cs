using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Entia.Core;

namespace Entia.Experiment
{
    public static class TypeMapTest
    {
        public class Dictionary : Dictionary<int, object> { }
        [Serializable]
        public class Cyclic { public Cyclic A; }

        public static void Benchmark()
        {
            var dictionary = new Dictionary<Type, object>();
            var intDictionary = new Dictionary<int, object>();
            var concurrent = new ConcurrentDictionary<Type, object>();
            var map = new TypeMap<object, object>();
            var value = default(object);

            void Add<T>(T current)
            {
                intDictionary[map.Index<T>()] = concurrent[typeof(T)] = dictionary[typeof(T)] = current;
                map[typeof(T)] = current;
            }

            void AddKey(Type key, object current)
            {
                map.TryIndex(key, out var index);
                intDictionary[index] = concurrent[key] = dictionary[key] = current;
                map[key] = current;
            }

            map.TryGet<object>(out var value0); // Expected: null
            Add(byte.MaxValue);
            map.TryGet<IConvertible>(out var value1, false, true); // Expected: byte
            Add(sbyte.MaxValue);
            map.TryGet<ValueType>(out var value2, true, true); // Expected: byte
            Add(ushort.MaxValue);
            map.TryGet(typeof(IComparable<>), out var value3, false, true); // Expected: byte
            Add(short.MaxValue);
            map.TryGet(typeof(object), out var value4, false, false); // Expected: null
            Add(uint.MaxValue);
            Add(int.MaxValue);
            Add(ulong.MaxValue);
            Add(long.MaxValue);
            Add(float.MaxValue);
            Add(double.MaxValue);
            Add(decimal.MaxValue);
            Add(new Unit());
            Add(new Cyclic());
            map.TryGet<Cyclic>(out var value5); // Expected Cyclic
            Add(DateTime.MaxValue);
            map.TryGet(typeof(DateTime), out var value6); // Expected DateTime
            Add(TimeSpan.MaxValue);
            Add(dictionary);
            map.TryGet(typeof(IDictionary<,>), out var value7, true, false); // Expected null
            Add(intDictionary);
            map.TryGet(typeof(IDictionary<,>), out var value8, false, true); // Expected Dictionary<Type, object>
            Add(concurrent);
            map.TryGet(typeof(IDictionary<,>), out var value9, true, true); // Expected Dictionary<Type, object>
            Add(map);
            map.TryGet(typeof(TypeMap<,>), out var value10, true, true); // Expected TypeMap<object, object>
            map.TryGet(typeof(Dictionary), out var value11, true, false); // Expected Dictionary<int, object>

            foreach (var type in ReflectionUtility.AllTypes.Take(1000)) AddKey(type, new object());
            var index = map.Index<int>();
            map.Index<Action>();
            var array = map.Values.ToArray();

            void ArrayGet() => value = array[index];
            void TryGetI() => dictionary.TryGetValue(typeof(int), out value);
            void TryGetA() => dictionary.TryGetValue(typeof(Action), out value);
            void ITryGetI() => intDictionary.TryGetValue(map.Index<int>(), out value);
            void ITryGetA() => intDictionary.TryGetValue(map.Index<Action>(), out value);
            void ConcurrentGetI() => concurrent.TryGetValue(typeof(int), out value);
            void ConcurrentGetA() => concurrent.TryGetValue(typeof(Action), out value);
            void MapGetTI() => value = map.Get<int>(out _);
            void MapGetTA() => value = map.Get<Action>(out _);
            void MapGetIndex() => value = map[index];
            void MapTryGetI() => map.TryGet(typeof(int), out value);
            void MapTryGetTI() => map.TryGet<int>(out value);
            void MapTryGetA() => map.TryGet(typeof(Action), out value);
            void MapTryGetTA() => map.TryGet<Action>(out value);

            for (int i = 0; i < 500; i++)
            {
                Test.Measure(ArrayGet, new Action[]
                {
                    TryGetI,
                    TryGetA,
                    ITryGetI,
                    ITryGetA,
                    ConcurrentGetI,
                    ConcurrentGetA,
                    MapGetTI,
                    MapGetTA,
                    MapGetIndex,
                    MapTryGetI,
                    MapTryGetA,
                    MapTryGetTI,
                    MapTryGetTA,
                }, 50_000);
            }
        }
    }
}