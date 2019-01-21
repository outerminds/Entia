using Entia.Dependables;
using Entia.Injectors;

namespace Entia.Injectables
{
    public interface IInjectable : IDependable { }
    public interface IInjectable<T> : IInjectable where T : IInjector, new() { }
}
