using Entia.Dependers;
using System;

namespace Entia.Dependables
{
    public interface IDependable { }
    public interface IDependable<T> where T : IDepender, new() { }

    public interface IDepend : IDependable { }
    [Generics]
    public interface IDepend<TOn> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3, TOn4> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3, TOn4, TOn5> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3, TOn4, TOn5, TOn6> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3, TOn4, TOn5, TOn6, TOn7> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3, TOn4, TOn5, TOn6, TOn7, TOn8> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3, TOn4, TOn5, TOn6, TOn7, TOn8, TOn9> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3, TOn4, TOn5, TOn6, TOn7, TOn8, TOn9, TOn10> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3, TOn4, TOn5, TOn6, TOn7, TOn8, TOn9, TOn10, TOn11> : IDepend { }
    [Generics]
    public interface IDepend<TOn1, TOn2, TOn3, TOn4, TOn5, TOn6, TOn7, TOn8, TOn9, TOn10, TOn11, TOn12> : IDepend { }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class MembersAttribute : Attribute, IDependable<Members> { }
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class GenericsAttribute : Attribute, IDependable<Generics> { }

    public sealed class Read<T> : IDependable<Dependers.Read<T>>, IDepend<T> { }
    public sealed class Write<T> : IDependable<Dependers.Write<T>>, IDepend<T> { }
    public sealed class Emit<T> : IDependable<Dependers.Emit<T>>, IDepend<T> { }
    public sealed class React<T> : IDependable<Dependers.React<T>>, IDepend<T> { }
}
