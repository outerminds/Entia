using System.Collections.Generic;
using Entia.Modules.Serialization;

namespace Entia.Experiment
{
    public readonly struct SerializeContext
    {
        public readonly Writer Writer;
        public readonly Descriptors Descriptors;
        public readonly World World;
        public readonly Dictionary<object, int> References;

        public SerializeContext(Writer writer, Descriptors descriptors, World world, Dictionary<object, int> references)
        {
            Writer = writer;
            Descriptors = descriptors;
            World = world;
            References = references;
        }
    }

    public readonly struct DeserializeContext
    {
        public readonly Reader Reader;
        public readonly Descriptors Descriptors;
        public readonly World World;
        public readonly object[] References;

        public DeserializeContext(Reader reader, Descriptors descriptors, World world, object[] references)
        {
            Reader = reader;
            Descriptors = descriptors;
            World = world;
            References = references;
        }
    }

    public readonly struct CloneContext
    {
        public readonly World World;
        public readonly Descriptors Descriptors;
    }
}