using Entia.Serializers;

namespace Entia.Serializables
{
    public interface ISerializable { }
    public interface ISerializable<T> where T : ISerializer, new() { }
}