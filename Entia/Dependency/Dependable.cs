using Entia.Dependers;
using System;

namespace Entia.Dependables
{
    public interface IDependable { }
    public interface IDependable<T> where T : IDepender, new() { }
}
