using Entia.Core;
using Entia.Initializers;
using Entia.Instantiators;
using Entia.Modules;
using Entia.Modules.Template;
using System.Linq;

namespace Entia.Templaters
{
	public sealed class Entity : Templater<Entia.Entity>
	{
		public override Result<IInitializer> Initializer(Entia.Entity value, Context context, World world)
		{
			var tags = world.Tags().Get(value).ToArray();
			var result = world.Components().Get(value)
				.Select(component => world.Templaters().Template(component, context).Map(element => element.Reference))
				.All();
			if (result.TryFailure(out var failure)) return failure;
			if (result.TryValue(out var components)) return new Initializers.Entity(tags, components, world);
			return Result.Failure();
		}

		public override Result<IInstantiator> Instantiator(Entia.Entity value, Context context, World world) => new Instantiators.Entity(world);
	}
}
