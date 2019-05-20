using System.Collections.Generic;
using System.Linq;
using FsCheck;

namespace Entia.Test
{
    public static class PropertyUtility
    {
        public static Property All(params (bool test, string label)[] properties) => All(properties.AsEnumerable());

        public static Property All(IEnumerable<(bool test, string label)> properties) =>
            properties.Select(pair => pair.test.Label(pair.label)).Aggregate((result, current) => result.And(current));
    }
}