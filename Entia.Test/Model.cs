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
        public readonly Dictionary<Entity, HashSet<Type>> Tags = new Dictionary<Entity, HashSet<Type>>();
        public readonly HashSet<IGroup_OLD> Groups = new HashSet<IGroup_OLD>();

        public Model(int seed) { Random = new Random(seed); }
    }
}