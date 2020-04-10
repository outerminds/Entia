using System;
using Entia.Core;
using System.Linq;
using Entia.Modules.Component;

namespace Entia.Experimental
{
    public readonly struct Filter
    {
        public static Filter All(params Filter[] filters) =>
            filters.Length == 0 ? True :
            filters.Length == 1 ? filters[0] :
            new Filter(segment =>
            {
                foreach (var filter in filters)
                {
                    if (filter.Matches(segment)) continue;
                    return false;
                }
                return true;
            });
        public static Filter All<T>(params Filter[] filters) where T : IComponent =>
            All(filters.Prepend(Has<T>()));
        public static Filter All<T1, T2>(params Filter[] filters) where T1 : IComponent where T2 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>()));
        public static Filter All<T1, T2, T3>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>()));
        public static Filter All<T1, T2, T3, T4>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>()));
        public static Filter All<T1, T2, T3, T4, T5>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>()));
        public static Filter All<T1, T2, T3, T4, T5, T6>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>()));
        public static Filter All<T1, T2, T3, T4, T5, T6, T7>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent where T7 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>(), Has<T7>()));

        public static Filter Any(params Filter[] filters) =>
            filters.Length == 0 ? False :
            filters.Length == 1 ? filters[0] :
            new Filter(segment =>
            {
                foreach (var filter in filters) if (filter.Matches(segment)) return true;
                return false;
            });
        public static Filter Any<T>(params Filter[] filters) where T : IComponent =>
            Any(filters.Prepend(Has<T>()));
        public static Filter Any<T1, T2>(params Filter[] filters) where T1 : IComponent where T2 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>()));
        public static Filter Any<T1, T2, T3>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>()));
        public static Filter Any<T1, T2, T3, T4>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>()));
        public static Filter Any<T1, T2, T3, T4, T5>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>()));
        public static Filter Any<T1, T2, T3, T4, T5, T6>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>()));
        public static Filter Any<T1, T2, T3, T4, T5, T6, T7>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent where T7 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>(), Has<T7>()));

        public static Filter None(params Filter[] filters) => Not(Any(filters));
        public static Filter None<T>(params Filter[] filters) where T : IComponent =>
            None(filters.Prepend(Has<T>()));
        public static Filter None<T1, T2>(params Filter[] filters) where T1 : IComponent where T2 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>()));
        public static Filter None<T1, T2, T3>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>()));
        public static Filter None<T1, T2, T3, T4>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>()));
        public static Filter None<T1, T2, T3, T4, T5>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>()));
        public static Filter None<T1, T2, T3, T4, T5, T6>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>()));
        public static Filter None<T1, T2, T3, T4, T5, T6, T7>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent where T7 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>(), Has<T7>()));

        public static Filter Not(Filter filter) => new Filter(segment => !filter.Matches(segment));

        static Filter Has<T>() where T : IComponent =>
            ComponentUtility.TryGetConcreteMask<T>(out var mask) ?
            new Filter(segment => segment.Mask.HasAny(mask)) : False;

        public static readonly Filter True = new Filter(_ => true);
        public static readonly Filter False = new Filter(_ => false);

        public readonly Func<Segment, bool> Matches;
        public Filter(Func<Segment, bool> matches) { Matches = matches; }
    }
}