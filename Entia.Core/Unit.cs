using System;

namespace Entia.Core
{
    public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
    {
        public int CompareTo(Unit other) => 0;
        public bool Equals(Unit other) => true;
        public override bool Equals(object obj) => obj is Unit;
        public override int GetHashCode() => 0;
    }
}
