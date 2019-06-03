using Entia.Modules.Component;
using Entia.Modules.Group;
using Entia.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Test
{
    public sealed class ComponentModel : IEnumerable<Type>
    {
        public int Count => Enabled.Count + Disabled.Count;

        public readonly HashSet<Type> Enabled = new HashSet<Type>();
        public readonly HashSet<Type> Disabled = new HashSet<Type>();

        public bool Add(Type concrete) => Enabled.Add(concrete);

        public bool Set(Type concrete) => Disabled.Contains(concrete) || Enabled.Add(concrete);

        public void Enable(Type @abstract)
        {
            foreach (var type in Disabled.ToArray())
            {
                if (type.Is(@abstract, true, true))
                {
                    Disabled.Remove(type);
                    Enabled.Add(type);
                }
            }
        }

        public void Disable(Type @abstract)
        {
            foreach (var type in Enabled.ToArray())
            {
                if (type.Is(@abstract, true, true))
                {
                    Enabled.Remove(type);
                    Disabled.Add(type);
                }
            }
        }

        public bool Contains(Type @abstract, States include = States.All)
        {
            foreach (var type in ToArray(include)) if (type.Is(@abstract, true, true)) return true;
            return false;
        }

        public void Remove(Type @abstract, States include = States.All)
        {
            foreach (var type in ToArray())
            {
                if (type.Is(@abstract, true, true))
                {
                    if (include.HasAny(States.Enabled)) Enabled.Remove(type);
                    if (include.HasAny(States.Disabled)) Disabled.Remove(type);
                }
            }
        }

        public void Clear(States include = States.All)
        {
            if (include.HasAny(States.Enabled)) Enabled.Clear();
            if (include.HasAny(States.Disabled)) Disabled.Clear();
        }

        public Type[] ToArray(States include = States.All)
        {
            var types = new List<Type>();
            if (include.HasAny(States.Enabled)) types.AddRange(Enabled);
            if (include.HasAny(States.Disabled)) types.AddRange(Disabled);
            return types.ToArray();
        }

        public IEnumerator<Type> GetEnumerator() => Enabled.Concat(Disabled).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class Model
    {
        public readonly Random Random;
        public readonly HashSet<Entity> Entities = new HashSet<Entity>();
        public readonly Dictionary<Entity, ComponentModel> Components = new Dictionary<Entity, ComponentModel>();

        public Model(int seed) { Random = new Random(seed); }
    }
}