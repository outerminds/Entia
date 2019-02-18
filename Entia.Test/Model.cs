using Entia.Modules.Group;
using System;
using System.Collections.Generic;

namespace Entia.Test
{
    public class Model
    {
        public readonly Random Random;
        public readonly HashSet<Entity> Entities = new HashSet<Entity>();
        public readonly Dictionary<Entity, Dictionary<Type, IComponent>> Components = new Dictionary<Entity, Dictionary<Type, IComponent>>();

        public Model(int seed) { Random = new Random(seed); }
    }
}