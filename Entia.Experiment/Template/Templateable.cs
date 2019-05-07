using Entia.Templaters;

namespace Entia.Templateables
{
    public interface ITemplateable { }
    public interface ITemplateable<T> : ITemplateable where T : ITemplater, new() { }
}
