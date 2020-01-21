using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Core
{
    public delegate Result<TOut> Visit<TIn, TOut>(TIn value);
    public delegate Result<TOut> Visit<TIn, TState, TOut>(TIn value, in TState state);

    public sealed class Visitor<TIn, TState, TOut> : IEnumerable<(Type type, Visit<TIn, TState, TOut> visit)>
    {
        readonly TypeMap<TIn, Visit<TIn, TState, TOut>> _visits = new TypeMap<TIn, Visit<TIn, TState, TOut>>();

        public Result<TOut> Visit(TIn value, TState state) => Visit(value, value.GetType(), state);

        public Result<TOut> Visit(TIn value, Type type, TState state)
        {
            if (TryGet(value.GetType(), out var visit)) return visit(value, state);
            return Result.Failure($"Expected to find a visit for type '{type}'.");
        }

        public Result<TOut> Visit<T>(T value, TState state) where T : TIn
        {
            if (TryGet<T>(out var visit)) return visit(value, state);
            return Result.Failure($"Expected to find a visit for type '{typeof(T)}'.");
        }

        public bool Add<T>(Visit<T, TState, TOut> visit) where T : TIn =>
            _visits.Set<T>((TIn value, in TState state) => Result.Cast<T>(value).Bind(state, (casted, inner) => visit(casted, inner)));
        public bool Add(Type type, Visit<TIn, TState, TOut> visit) =>
            _visits.Set(type, visit);
        public bool TryGet<T>(out Visit<TIn, TState, TOut> visit) where T : TIn => _visits.TryGet<T>(out visit, true, false);
        public bool TryGet(Type type, out Visit<TIn, TState, TOut> visit) => _visits.TryGet(type, out visit, true, false);
        public bool Remove<T>() where T : TIn => _visits.Remove<T>();
        public bool Remove(Type type) => _visits.Remove(type);
        public bool Clear() => _visits.Clear();
        public TypeMap<TIn, Visit<TIn, TState, TOut>>.Enumerator GetEnumerator() => _visits.GetEnumerator();
        IEnumerator<(Type type, Visit<TIn, TState, TOut> visit)> IEnumerable<(Type type, Visit<TIn, TState, TOut> visit)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}