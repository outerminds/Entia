using Entia.Core.Documentation;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using System;

namespace Entia.Queryables
{
    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class IncludeAttribute : Attribute
    {
        public readonly States States;
        public IncludeAttribute(States states = States.Enabled) { States = states; }
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class QueryAttribute : Attribute, IQuerier
    {
        public readonly States? Include;
        public readonly Type Queryable;
        public QueryAttribute(Type queryable) { Queryable = queryable; }
        public QueryAttribute(States include, Type queryable)
        {
            Include = include;
            Queryable = queryable;
        }
        public bool TryQuery(in Context context, out Query query) =>
            context.World.Queriers().TryQuery(Queryable, context.With(Include), out query);
    }

    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class AllAttribute : Attribute, IQuerier
    {
        public readonly States? Include;
        public readonly Type[] Components;

        public AllAttribute(params Type[] components) { Components = components; }
        public AllAttribute(States include, params Type[] components)
        {
            Include = include;
            Components = components;
        }

        public bool TryQuery(in Context context, out Query query)
        {
            var current = context.With(Include);
            var all = new Metadata[Components.Length];
            query = Query.Empty;
            for (int i = 0; i < Components.Length; i++)
            {
                if (QueryUtility.TryMatch(current.Segment.Mask, Components[i], current, out var metadata))
                    all[i] = metadata;
                else
                    return false;
            }

            query = new Query(query.Fill, all);
            return true;
        }
    }

    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class AnyAttribute : Attribute, IQuerier
    {
        public readonly States? Include;
        public readonly Type[] Components;

        public AnyAttribute(params Type[] components) { Components = components; }
        public AnyAttribute(States include, params Type[] components)
        {
            Include = include;
            Components = components;
        }

        public bool TryQuery(in Context context, out Query query)
        {
            var current = context.With(Include);
            query = Query.Empty;
            for (int i = 0; i < Components.Length; i++)
            {
                if (QueryUtility.TryMatch(current.Segment.Mask, Components[i], current, out var metadata))
                {
                    query = new Query(query.Fill, metadata);
                    return true;
                }
            }
            return false;
        }
    }

    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Method)]
    public sealed class NoneAttribute : Attribute, IQuerier
    {
        public readonly States? Include;
        public readonly Type[] Components;

        public NoneAttribute(params Type[] components) { Components = components; }
        public NoneAttribute(States include, params Type[] components)
        {
            Include = include;
            Components = components;
        }

        public bool TryQuery(in Context context, out Query query)
        {
            var current = context.With(Include);
            query = Query.Empty;
            for (int i = 0; i < Components.Length; i++)
            {
                if (QueryUtility.TryMatch(current.Segment.Mask, Components[i], current, out var metadata))
                    return false;
            }
            return true;
        }
    }
}
