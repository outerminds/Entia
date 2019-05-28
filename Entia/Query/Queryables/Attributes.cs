﻿using Entia.Components;
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
    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class IncludeAttribute : Attribute
    {
        public readonly States States;
        public IncludeAttribute(States states = States.Enabled) { States = states; }
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field)]
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
        public bool TryQuery(in Context context, out Query query) => context.World.Queriers().TryQuery(Queryable, context, out query, Include);
    }

    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field)]
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
            var include = Include ?? context.Include;
            var all = new Metadata[Components.Length];
            query = Query.Empty;
            for (int i = 0; i < Components.Length; i++)
            {
                if (QueryUtility.TryMatch(context.Segment.Mask, Components[i], context.World, include, out var metadata))
                    all[i] = metadata;
                else
                    return false;
            }

            query = new Query(query.Fill, all);
            return true;
        }
    }

    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field)]
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
            var include = Include ?? context.Include;
            query = Query.Empty;
            for (int i = 0; i < Components.Length; i++)
            {
                if (QueryUtility.TryMatch(context.Segment.Mask, Components[i], context.World, include, out var metadata))
                {
                    query = new Query(query.Fill, metadata);
                    return true;
                }
            }
            return false;
        }
    }

    [ThreadSafe]
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field)]
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
            var include = Include ?? context.Include;
            query = Query.Empty;
            for (int i = 0; i < Components.Length; i++)
            {
                if (QueryUtility.TryMatch(context.Segment.Mask, Components[i], context.World, include, out var metadata))
                    return false;
            }
            return true;
        }
    }
}
