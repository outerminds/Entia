using System.Collections.Generic;
using System.Linq;
using Entia.Modules;

namespace Entia.Experiment
{
    public static class ComponentTest
    {
        struct Boba : IComponent
        {
            public Dictionary<string, List<int>> A;
            public Dictionary<object, int> B;
        }

        public static void Run()
        {
            var world = new World();
            var entities = world.Entities();
            var components = world.Components();

            var source = entities.Create();
            var key = new object();
            components.Set(source, new Boba
            {
                A = new Dictionary<string, List<int>> { { "Fett", new List<int> { 1, 2, 3 } } },
                B = new Dictionary<object, int> { { key, 5 } }
            });
            var target = entities.Create();
            components.Clone(source, target, Depth.Deep);
            ref var a = ref components.Get<Boba>(source);
            ref var b = ref components.Get<Boba>(target);
            var c = a.A == b.A;
            var d = a.A["Fett"] == b.A["Fett"];
            var e = a.A["Fett"].SequenceEqual(b.A["Fett"]);
            var f = a.B == b.B;
            var g = a.B[key] == b.B[key];
        }
    }
}