using Entia.Core;
using Entia.Json.Converters;

namespace Entia.Json
{
    public static class ContainerExtensions
    {
        public static void Add<T>(this Container container, Converter<T> converter) => container.Add<T, IConverter>(converter);
    }
}