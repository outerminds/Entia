using Entia.Core;
using Entia.Core.Documentation;
using Entia.Dependencies;
using Entia.Dependers;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Entia.Queryables
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class QueryAttribute : Attribute, IQuerier
    {
        public readonly Type Queryable;
        public QueryAttribute(Type queryable) { Queryable = queryable; }
        public bool TryQuery(Segment segment, World world) => world.Queriers().Get(Queryable).TryQuery(segment, world);
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    [ThreadSafe]
    public sealed class AllAttribute : Attribute, IQuerier
    {
        public readonly Type[] Components;
        readonly BitMask[] _masks;

        public AllAttribute(params Type[] components)
        {
            Components = components.Where(ComponentUtility.IsValid).ToArray();
            _masks = Components.Select(ComponentUtility.GetConcrete).ToArray();
        }

        public bool TryQuery(Segment segment, World world)
        {
            for (int i = 0; i < _masks.Length; i++) if (segment.Mask.HasNone(_masks[i])) return false;
            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    [ThreadSafe]
    public sealed class AnyAttribute : Attribute, IQuerier
    {
        public readonly Type[] Components;
        readonly BitMask[] _masks;

        public AnyAttribute(params Type[] components)
        {
            Components = components.Where(ComponentUtility.IsValid).ToArray();
            _masks = Components.Select(ComponentUtility.GetConcrete).ToArray();
        }

        public bool TryQuery(Segment segment, World world)
        {
            for (int i = 0; i < _masks.Length; i++) if (segment.Mask.HasAny(_masks[i])) return true;
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    [ThreadSafe]
    public sealed class NoneAttribute : Attribute, IQuerier
    {
        public readonly Type[] Components;
        readonly BitMask[] _masks;

        public NoneAttribute(params Type[] components)
        {
            Components = components.Where(ComponentUtility.IsValid).ToArray();
            _masks = Components.Select(ComponentUtility.GetConcrete).ToArray();
        }

        public bool TryQuery(Segment segment, World world)
        {
            for (int i = 0; i < _masks.Length; i++) if (segment.Mask.HasAny(_masks[i])) return false;
            return true;
        }
    }
}
