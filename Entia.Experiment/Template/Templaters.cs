using Entia.Core;
using Entia.Modules.Template;
using Entia.Templateables;
using Entia.Templaters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed class Templaters : IModule, IEnumerable<ITemplater>
    {
        readonly World _world;
        readonly TypeMap<object, ITemplater> _defaults = new TypeMap<object, ITemplater>();
        readonly TypeMap<object, ITemplater> _templaters = new TypeMap<object, ITemplater>();

        public Templaters(World world) { _world = world; }

        public Result<Template<T>> Template<T>(T value) => Template<T>(value, typeof(T));

        public Result<Template<T>> Template<T>(object value, Type type)
        {
            var context = new Context(value, type);
            return Template(context).Map(context, (_, state) => new Template<T>(state.Pairs.ToArray()));
        }

        public Result<Reference> Template(in Context context)
        {
            var key = new Context.Key(context.Value);
            if (context.Indices.TryGetValue(key, out var index)) return new Reference(index, context.Pairs[index]);

            index = context.Indices.Count;
            context.Indices[key] = index;
            context.Pairs.Add(default);

            return Get(context.Type)
                .Template(new Context(index, context), _world)
                .Map((context, index), (pair, state) => new Reference(state.index, state.context.Pairs[state.index] = pair));
        }

        public ITemplater Default<T>() => _defaults.TryGet<T>(out var templater, false, false) ? templater : Default(typeof(T));
        public ITemplater Default(Type type) => _defaults.Default(type, typeof(ITemplateable<>), typeof(TemplaterAttribute), _ => new Default());
        public ITemplater Get<T>() => _templaters.TryGet<T>(out var templater, true, false) ? templater : Default<T>();
        public ITemplater Get(Type type) => _templaters.TryGet(type, out var templater, true, false) ? templater : Default(type);
        public bool Set<T>(ITemplater templater, bool fallback = false) =>
            fallback ? _defaults.Set<T>(templater) : _templaters.Set<T>(templater);
        public bool Set(Type type, ITemplater templater, bool fallback = false) =>
            fallback ? _defaults.Set(type, templater) : _templaters.Set(type, templater);
        public bool Has<T>() => _templaters.Has<T>(true, false) || _defaults.Has<T>(true, false);
        public bool Has(Type type) => _templaters.Has(type, true, false) || _defaults.Has(type, true, false);
        public bool Remove<T>() => _templaters.Remove<T>(false, false) || _defaults.Remove<T>(false, false);
        public bool Remove(Type type) => _templaters.Remove(type, false, false) || _templaters.Remove(type, false, false);
        public bool Clear() => _templaters.Clear() | _defaults.Clear();
        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<ITemplater> GetEnumerator() => _templaters.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
