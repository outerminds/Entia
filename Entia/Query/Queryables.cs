using Entia.Dependables;
using Entia.Queriers;

namespace Entia.Queryables
{
    public interface IQueryable : IDependable<Dependers.Fields> { }
    public interface IQueryable<T> : IQueryable where T : IQuerier, new() { }
}