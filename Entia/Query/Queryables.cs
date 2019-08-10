using Entia.Core;
using Entia.Queriers;

namespace Entia.Queryables
{
    public interface IQueryable : IImplementation<Dependers.Fields> { }
    public interface IQueryable<T> : IQueryable where T : IQuerier, new() { }
}