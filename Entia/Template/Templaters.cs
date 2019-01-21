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

        public Result<Template<T>> Template<T>(T value)
        {
            var context = new Context();
            return Template(value, context).Map(_ => new Template<T>(context.Pairs.ToArray()));
        }

        public Result<Element> Template(object value, Context context)
        {
            var key = new Context.Key(value);
            if (context.Indices.TryGetValue(key, out var reference))
                return new Element(reference, context.Pairs[reference].initializer, context.Pairs[reference].instantiator);

            reference = context.Indices.Count;
            context.Indices[key] = reference;
            context.Pairs.Add(default);

            var templater = Get(value?.GetType() ?? typeof(object));
            var result1 = templater.Instantiator(value, context, _world)
                .Do(instantiator => context.Pairs[reference] = (context.Pairs[reference].initializer, instantiator));
            var result2 = templater.Initializer(value, context, _world)
                .Do(initializer => context.Pairs[reference] = (initializer, context.Pairs[reference].instantiator));

            if (Result.All(result1, result2).TryFailure(out var failure)) return failure;
            return new Element(reference, context.Pairs[reference]);
        }

        public ITemplater Default<T>() => _defaults.TryGet<T>(out var templater) ? templater : Default(typeof(T));
        public ITemplater Default(Type type) => _defaults.Default(type, typeof(ITemplateable<>), typeof(TemplaterAttribute), () => new Default());
        public ITemplater Get<T>() => _templaters.TryGet<T>(out var templater, true) ? templater : Default<T>();
        public ITemplater Get(Type type) => _templaters.TryGet(type, out var templater, true) ? templater : Default(type);
        public bool Set<T>(Templater<T> templater) => _templaters.Set<T>(templater);
        public bool Set(Type type, ITemplater templater) => _templaters.Set(type, templater);
        public bool Has<T>() => _templaters.Has<T>(true);
        public bool Has(Type type) => _templaters.Has(type, true);
        public bool Remove<T>() => _templaters.Remove<T>();
        public bool Remove(Type type) => _templaters.Remove(type);
        public bool Clear() => _defaults.Clear() | _templaters.Clear();
        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<ITemplater> GetEnumerator() => _templaters.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
