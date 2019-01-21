using Entia.Core;
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
        public bool TryQuery(Segment segment, World world, out Query query) => world.Queriers().Get(Queryable).TryQuery(segment, world, out query);
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class AllAttribute : Attribute, IQuerier
    {
        public readonly Metadata[] Types;
        public readonly BitMask Mask;

        public AllAttribute(params Type[] components) { ComponentUtility.ToMetadataAndMask(components, out Types, out Mask); }

        public bool TryQuery(Segment segment, World world, out Query query)
        {
            if (segment.Mask.HasAll(Mask))
            {
                query = new Query(Types);
                return true;
            }

            query = default;
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class AnyAttribute : Attribute, IQuerier
    {
        public readonly Metadata[] Types;
        public readonly BitMask Mask;

        public AnyAttribute(params Type[] components) { ComponentUtility.ToMetadataAndMask(components, out Types, out Mask); }

        public bool TryQuery(Segment segment, World world, out Query query)
        {
            if (segment.Mask.HasAny(Mask))
            {
                query = new Query(Types);
                return true;
            }

            query = default;
            return false;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class NoneAttribute : Attribute, IQuerier
    {
        public readonly Metadata[] Types;
        public readonly BitMask Mask;

        public NoneAttribute(params Type[] components) { ComponentUtility.ToMetadataAndMask(components, out Types, out Mask); }

        public bool TryQuery(Segment segment, World world, out Query query)
        {
            if (segment.Mask.HasNone(Mask))
            {
                query = new Query(Types);
                return true;
            }

            query = default;
            return false;
        }
    }
}
