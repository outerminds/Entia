using System;
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

        public static Filter Any(params Filter[] filters) =>
            filters.Length == 0 ? False :
            filters.Length == 1 ? filters[0] :
            new Filter(segment =>
            {
                foreach (var filter in filters) if (filter.Matches(segment)) return true;
                return false;
            });

        public static Filter None(params Filter[] filters) => Not(Any(filters));

        public static Filter Not(Filter filter) => new Filter(segment => !filter.Matches(segment));

        public static Filter Has<T>() where T : IComponent =>
            ComponentUtility.TryGetConcreteMask<T>(out var mask) ?
            new Filter(segment => segment.Mask.HasAny(mask)) : False;

        public static readonly Filter True = new Filter(_ => true);
        public static readonly Filter False = new Filter(_ => false);

        public readonly Func<Segment, bool> Matches;
        public Filter(Func<Segment, bool> matches) { Matches = matches; }
    }
}