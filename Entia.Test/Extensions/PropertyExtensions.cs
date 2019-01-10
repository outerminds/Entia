using System;
using System.Linq;
using System.Threading.Tasks;
using FsCheck;

namespace Entia.Test
{
    public static class PropertyExtensions
    {
        public static void Check(this Property property, string name, int count = 100, int size = 100, bool @throw = false) =>
            property.Check(new Configuration { Name = name, MaxNbOfTest = count, EndSize = size, Runner = new Runner(count, @throw) });

        public static void Check(this Property property, string name, int parallel, int count = 100, int size = 100, bool @throw = false)
        {
            var master = new MasterRunner(parallel);
            Enumerable.Range(0, parallel)
                .AsParallel()
                .Select(i => new SlaveRunner(i, count / parallel, @throw, master))
                .ForAll(slave => property.Check(new Configuration { Name = name, MaxNbOfTest = count / parallel, EndSize = size, Runner = slave }));
        }
    }
}
