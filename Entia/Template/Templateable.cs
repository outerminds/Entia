using Entia.Templaters;

namespace Entia.Templateables
{
	public interface ITemplateable<T> where T : ITemplater, new() { }
}
