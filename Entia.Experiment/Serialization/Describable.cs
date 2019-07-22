namespace Entia.Experiment
{
    public interface IDescribable { }
    public interface IDescribable<T> where T : IDescriptor, new() { }
}