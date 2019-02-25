using Entia.Cloners;

namespace Entia.Cloneable
{
    public interface ICloneable<T> where T : ICloner, new() { }
}