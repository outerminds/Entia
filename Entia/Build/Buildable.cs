using Entia.Builders;

namespace Entia.Modules.Build
{
	public interface IBuildable { }
	public interface IBuildable<T> : IBuildable where T : IBuilder, new() { }
}
