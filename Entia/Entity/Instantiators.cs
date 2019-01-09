using Entia.Core;
using Entia.Modules;

namespace Entia.Instantiators
{
	public sealed class Entity : IInstantiator
	{
		public readonly World World;
		public Entity(World world) { World = world; }
		public Result<object> Instantiate(object[] instances) => World.Entities().Create();
	}
}
