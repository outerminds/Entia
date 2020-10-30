using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Entia.Bench;
using Entia.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Entia.Json.Test
{
    public static class Benches
    {
        struct Wrapper<T> { public T Value; }

        [Serializable]
        sealed class Cyclic
        {
            public Cyclic This;
            public Cyclic() { This = this; }
        }

        public static void Run()
        {
            var withReference = Settings.Default.With(Features.Reference);
            var withAbstract = Settings.Default.With(Features.Abstract);
            var withAll = Settings.Default.With(Features.All);

            var jsonA = @"{""$t"":3}";
            var valueA = Serialization.Parse(jsonA, withAbstract).Or(Node.Null);
            var jsonB = Serialization.Generate(valueA);
            var jsonC = @"{""$t"":3,""$v"":5}";
            var valueC = Serialization.Parse(jsonC, withAbstract).Or(Node.Null);
            var jsonD = Serialization.Generate(valueC);
            var node1 = Serialization.Parse(@"""<a href=\""http://twitter.com\"" rel=\""nofollow\"">Twitter Web Client</a>""");

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.None,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            var twitterJson = File.ReadAllText(@"C:\Projects\C#\Entia\Entia.Json.Test\twitter.json");
            var twitterBytes = File.ReadAllBytes(@"C:\Projects\C#\Entia\Entia.Json.Test\twitter.json"); ;
            var twitterNode = Serialization.Parse(twitterJson).Or(Node.Null);
            var twitterObject = JsonConvert.DeserializeObject<JObject>(twitterJson, settings);

            void Test<T>(in T value, out string json, out Node node, out T instance, Settings settings = null)
            {
                json = Json.Serialization.Serialize(value, settings);
                node = Serialization.Parse(json, settings).Or(Node.Null);
                instance = Json.Serialization.Instantiate<T>(node, settings);
            }

            Test(new Cyclic(), out var json4, out var node4, out var value4, withAll);
            Test<object>(new Cyclic(), out var json4B, out var node4B, out var value4B, withAll);
            Test<object>(new Dictionary<object, object>
            {
                { 1, "2" },
                { "3", null },
                { 4, "5" },
                { DateTime.Now, TimeSpan.MaxValue }
            }, out var json5, out var node5, out var value5, withAbstract);
            Test(new Dictionary<int, int> { { 1, -1 }, { 2, -2 } }, out var json6, out var node6, out var value6);
            Test<object>(1, out var json7, out var node7, out var value7, withAbstract);

            var dictionary = new Dictionary<object, object>();
            dictionary[1] = dictionary;
            dictionary[1f] = dictionary;
            dictionary[1uL] = dictionary;
            dictionary[1d] = 1f;
            dictionary[typeof(int)] = typeof(int[]);
            Test(dictionary, out var json8, out var node8, out var value8, withAll);

            Test(new Stack<int>(new int[] { 1, 2, 3 }), out var json9, out var node9, out var value9);
            Test(new Queue<int>(new int[] { 1, 2, 3 }), out var json10, out var node10, out var value10);
            Test(new List<int>(new int[] { 1, 2, 3 }), out var json11, out var node11, out var value11);
            Test(new int[] { 1, 2, 3 }, out var json12, out var node12, out var value12);
            Test(new Queue(new object[] { 1, "2", 3L }), out var json13, out var node13, out var value13, withAbstract);
            Test(new SortedDictionary<int, int> { { 1, 2 }, { 3, 4 } }, out var json14, out var node14, out var value14);
            Test(new ConcurrentDictionary<int, int>(new[] { new KeyValuePair<int, int>(1, 2), new KeyValuePair<int, int>(3, 4) }), out var json15, out var node15, out var value15);
            Test(Features.Abstract, out var json16, out var node16, out var value16);
            Test(Null.Some(Features.Abstract), out var json17, out var node17, out var value17);
            Test(Null.None<Features>(), out var json18, out var node18, out var value18);
            Test(Option.None().AsOption<int>(), out var json19, out var node19, out var value19);
            Test(Option.Some(32), out var json20, out var node20, out var value20);

            var anonymous = new
            {
                a = Option.Some(32),
                b1 = Null.None<Features>(),
                b2 = Null.None<Features>(),
                b3 = Null.None<Features>(),
                c = Features.Abstract,
            };
            Test(anonymous, out var json21, out var node21, out var value21);
            Test<object>(anonymous, out var json22, out var node22, out var value22, withAll);
            Test(anonymous.GetType(), out var json23, out var node23, out var value23, withAbstract);
            Test(new Dictionary<string, object> { { "boba", "fett" } }, out var json24, out var node24, out var value24, withAll);
            Test(new BitArray(new byte[] { 0, 1, 2, 3, 4, 5 }), out var json25, out var node25, out var value25);

            void SerializeA<T>(T value)
            {
                var json = Json.Serialization.Serialize(new Wrapper<T> { Value = value }, withAll);
                value = Json.Serialization.Deserialize<Wrapper<T>>(json, withAll).OrDefault().Value;
            }

            void SerializeB<T>(T value)
            {
                var json = JsonConvert.SerializeObject(new Wrapper<T> { Value = value }, settings);
                value = JsonConvert.DeserializeObject<Wrapper<T>>(json, settings).Value;
            }

            string generated;
            void ParseA() => Serialization.Parse(twitterJson);
            void ParseB() => JsonConvert.DeserializeObject<JObject>(twitterJson, settings);
            void GenerateA() => generated = Serialization.Generate(twitterNode);
            void GenerateB() => generated = JsonConvert.SerializeObject(twitterObject, settings);
            void IntNumberA() => SerializeA(1);
            void ObjectNumberA() => SerializeA<object>(1);
            void CyclicA() => SerializeA(new Cyclic());
            void ObjectCyclicA() => SerializeA<object>(new Cyclic());
            void IntDictionaryA() => SerializeA(new Dictionary<int, int> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void IntObjectDictionaryA() => SerializeA(new Dictionary<int, object> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void ObjectIntDictionaryA() => SerializeA(new Dictionary<object, int> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void ObjectDictionaryA() => SerializeA(new Dictionary<object, object> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void LargeArrayA() => SerializeA(new ulong[256]);

            void IntNumberB() => SerializeB(1);
            void ObjectNumberB() => SerializeB<object>(1);
            void CyclicB() => SerializeB(new Cyclic());
            void ObjectCyclicB() => SerializeB<object>(new Cyclic());
            void IntDictionaryB() => SerializeB(new Dictionary<int, int> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void IntObjectDictionaryB() => SerializeB(new Dictionary<int, object> { { 1, TimeSpan.MaxValue }, { 2, TimeSpan.MaxValue }, { 3, TimeSpan.MaxValue } });
            void ObjectIntDictionaryB() => SerializeB(new Dictionary<object, int> { { 1, -1 }, { 2, -2 }, { 3, -3 } });
            void ObjectDictionaryB() => SerializeB(new Dictionary<object, object> { { DateTime.Now, TimeSpan.MaxValue }, { DateTime.Now, TimeSpan.MaxValue }, { DateTime.Now, TimeSpan.MaxValue } });
            void LargeArrayB() => SerializeB(new ulong[256]);

            while (true)
            {
                Bencher.Measure(ParseA, new Action[]
                {
                    ParseB,
                    GenerateA,
                    GenerateB,
                }, 100, 1);

                Console.WriteLine();
                Bencher.Measure(IntNumberA, new Action[]
                {
                    ObjectNumberA,
                    CyclicA,
                    ObjectCyclicA,
                    IntDictionaryA,
                    IntObjectDictionaryA,
                    ObjectIntDictionaryA,
                    ObjectDictionaryA,
                    LargeArrayA,

                    IntNumberB,
                    ObjectNumberB,
                    CyclicB,
                    ObjectCyclicB,
                    IntDictionaryB,
                    IntObjectDictionaryB,
                    ObjectIntDictionaryB,
                    ObjectDictionaryB,
                    LargeArrayB,

                }, 1000);
                Console.WriteLine();
            }
        }
    }
}