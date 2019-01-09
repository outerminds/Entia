using Entia.Modules.Query;

namespace Entia.Queriers
{
	public sealed class Entity : Querier<Entia.Entity>
	{
		public override Query<Entia.Entity> Query(World world) => new Query<Entia.Entity>(
			Filter.Empty,
			Modules.Query.Query.True.Fits,
			(Entia.Entity entity, out Entia.Entity value) => { value = entity; return true; });
	}
}
