using Entia.Queriers;

namespace Entia.Queryables
{
	public interface IQueryable { }
	public interface IQueryable<T> : IQueryable where T : IQuerier, new() { }
}