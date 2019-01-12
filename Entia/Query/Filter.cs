using Entia.Core;
using System;
using System.Linq;

namespace Entia.Modules.Query
{
    public readonly struct Filter : IEquatable<Filter>
    {
        public static readonly Filter Empty = new Filter(null, null);

        public static Filter operator |(Filter a, Filter b) => new Filter(a.All | b.All, a.None | b.None, a.Types.Concat(b.Types).Distinct().ToArray());
        public static Filter operator &(Filter a, Filter b) => new Filter(a.All & b.All, a.None & b.None, a.Types.Concat(b.Types).Distinct().ToArray());
        public static Filter operator ~(Filter filter) => new Filter(filter.None, filter.All, filter.Types);

        public readonly BitMask All;
        public readonly BitMask None;
        public readonly Type[] Types;

        public Filter(BitMask all = null, BitMask none = null, params Type[] types)
        {
            All = all ?? new BitMask();
            None = none ?? new BitMask();
            Types = types;
        }

        public bool Equals(Filter other) => All.Equals(other.All) && None.Equals(other.None) && Types.SequenceEqual(other.Types);
        public override bool Equals(object obj) => obj is Filter filter && Equals(filter);
        public override int GetHashCode()
        {
            var hash = All.GetHashCode() ^ None.GetHashCode();
            for (var i = 0; i < Types.Length; i++) hash ^= Types[i].GetHashCode();
            return hash;
        }
    }
}
