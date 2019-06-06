using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Entia.Core
{
    public static class Ref
    {
        public static Ref<T> Null<T>() => Ref<T>.Null;
        public static Ref<T> Dummy<T>() => From(ref Core.Dummy<T>.Value);
        public static Ref<T> From<T>(ref T reference) => new Ref<T>(ref reference);
        public static Ref<T> From<T>(T[] array, int index) => new Ref<T>(ref array[index]);
    }

    public unsafe readonly struct Ref<T> : IEquatable<Ref<T>>
    {
        public unsafe readonly struct Read
        {
            public static readonly Read Null = new Read(null);
            public static implicit operator Read(in Ref<T> reference) => new Read(reference._pointer);

            public bool IsNull => _pointer == null;
            public ref readonly T Value => ref Unsafe.AsRef<T>(_pointer);

            readonly void* _pointer;

            Read(void* pointer) { _pointer = pointer; }
            public Read(ref T value) : this(Unsafe.AsPointer(ref value)) { }
        }

        public static readonly Ref<T> Null = new Ref<T>(null);

        public bool IsNull => _pointer == null;
        public ref T Value => ref Unsafe.AsRef<T>(_pointer);

        readonly void* _pointer;

        Ref(void* pointer) { _pointer = pointer; }
        public Ref(ref T value) : this(Unsafe.AsPointer(ref value)) { }

        public bool Equals(Ref<T> other) => _pointer == other._pointer;
        public override bool Equals(object obj) => obj is Ref<T> reference && Equals(reference);
        public override int GetHashCode() => (int)(long)_pointer;
    }
}