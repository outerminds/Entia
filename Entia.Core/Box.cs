using System;
using Entia.Core.Documentation;

namespace Entia.Core
{
    public interface IBox
    {
        object Value { get; set; }
        Type Type { get; }

        bool CopyTo(IBox box);
    }

    public sealed class Box<T> : IBox
    {
        public readonly struct Read
        {
            public ref readonly T Value => ref _box.Value;
            readonly Box<T> _box;
            public Read(Box<T> box) { _box = box; }
        }

        public static implicit operator Read(Box<T> box) => new Read(box);

        public T Value;

        object IBox.Value
        {
            get => Value;
            set => Value = value is T casted ? casted : default;
        }
        Type IBox.Type => typeof(T);

        public bool CopyTo(IBox box)
        {
            if (box is Box<T> casted)
            {
                casted.Value = Value;
                return true;
            }

            return false;
        }
    }
}
