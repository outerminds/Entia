using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Entia.Core;
using Entia.Experiment.Resolvables;

namespace Entia.Experiment.Injectables
{
    public readonly struct Defer
    {
        readonly Modules.Resolvers _resolvers;

        public Defer(Modules.Resolvers resolvers) { _resolvers = resolvers; }

        public void Do<T>(T state, Action<T> action) => _resolvers.Defer(new Do<T>(state, action));

        public void Set<T>(T[] array, T value, int index) => Do((array, value, index), state => state.array[state.index] = state.value);
        public void Set<T>(List<T> list, T value, int index) => Do((list, value, index), state => state.list[state.index] = state.value);
        public void Set<T>(IList<T> list, T value, int index) => Do((list, value, index), state => state.list[state.index] = state.value);
        public void Set<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value) => Do((dictionary, key, value), state => state.dictionary[state.key] = state.value);

        public void Add<T>(List<T> list, T value) => Do((list, value), state => state.list.Add(state.value));
        public void Add<T>(IList<T> list, T value) => Do((list, value), state => state.list.Add(state.value));

        public void Push<T>(Stack<T> stack, T value) => Do((stack, value), state => state.stack.Push(state.value));
        public void Pop<T>(Stack<T> stack, Action<T> action) => Do((stack, action), state => state.action(state.stack.Pop()));
        public void TryPop<T>(Stack<T> stack, Action<T> action) => Do((@this: this, stack, action), state => { if (state.stack.Count > 0) state.@this.Pop(state.stack, state.action); });
    }
}