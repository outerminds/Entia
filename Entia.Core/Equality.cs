using System;
using System.Collections.Generic;

namespace Entia.Core
{
    public static class Equality
    {
        sealed class FunctionComparer<T> : IEqualityComparer<T>
        {
            readonly Func<T, T, bool> _equals;
            readonly Func<T, int> _hash;

            public FunctionComparer(Func<T, T, bool> equals, Func<T, int> hash)
            {
                this._equals = equals;
                this._hash = hash;
            }

            public bool Equals(T x, T y) => _equals(x, y);
            public int GetHashCode(T obj) => _hash(obj);
        }

        public static IEqualityComparer<T> Comparer<T>(Func<T, T, bool> equals = null, Func<T, int> hash = null) =>
            new FunctionComparer<T>(
                equals ?? EqualityComparer<T>.Default.Equals,
                hash ?? EqualityComparer<T>.Default.GetHashCode);
    }
}