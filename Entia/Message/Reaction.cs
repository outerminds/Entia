﻿using Entia.Core;
using Entia.Core.Documentation;
using Entia.Serializables;
using Entia.Serializers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Entia.Modules.Message
{
    [ThreadSafe]
    public interface IReaction : IEnumerable<System.Delegate>
    {
        System.Type Type { get; }
        System.Delegate React { get; }
        bool Add(System.Delegate reaction);
        bool Remove(System.Delegate reaction);
        bool Clear();
    }

    [ThreadSafe]
    public sealed class Reaction<T> : IReaction, ISerializable<Reaction<T>.Serializer> where T : struct, IMessage
    {
        static readonly InAction<T> _empty = (in T _) => { };

        sealed class Serializer : Serializer<Reaction<T>>
        {
            public override bool Serialize(in Reaction<T> instance, TypeData dynamic, TypeData @static, in WriteContext context) =>
                context.Serializers.Serialize(instance._reaction, context);

            public override bool Instantiate(out Reaction<T> instance, TypeData dynamic, TypeData @static, in ReadContext context)
            {
                instance = new Reaction<T>();
                return true;
            }

            public override bool Deserialize(ref Reaction<T> instance, TypeData dynamic, TypeData @static, in ReadContext context)
            {
                var success = context.Serializers.Deserialize(out InAction<T> reaction, context);
                instance._reaction = reaction;
                return success;
            }
        }

        public InAction<T> React
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _reaction;
        }

        System.Delegate IReaction.React => _reaction;
        System.Type IReaction.Type => typeof(T);

        event InAction<T> _reaction = _empty;

        public bool Add(InAction<T> reaction)
        {
            // NOTE: do not use 'Concurrent.Mutate' to reduce generic nesting
            var before = _reaction;
            _reaction += reaction;
            return before != _reaction;
        }
        public bool Remove(InAction<T> reaction)
        {
            // NOTE: do not use 'Concurrent.Mutate' to reduce generic nesting
            var before = _reaction;
            _reaction -= reaction;
            return before != _reaction;
        }
        public bool Clear() => _reaction != Concurrent.Mutate(ref _reaction, _empty);

        public IEnumerator<System.Delegate> GetEnumerator() =>
            _reaction.GetInvocationList().Cast<System.Delegate>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        bool IReaction.Add(System.Delegate reaction) => reaction is InAction<T> action && Add(action);
        bool IReaction.Remove(System.Delegate reaction) => reaction is InAction<T> action && Remove(action);
    }
}
