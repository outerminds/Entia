using Entia.Injectors;

namespace Entia.Injectables
{
	public interface IInjectable { }
	public interface IInjectable<T> : IInjectable where T : IInjector, new() { }
}
