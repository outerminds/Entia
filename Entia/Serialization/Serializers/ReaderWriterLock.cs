using System.Threading;
using Entia.Core;
using Entia.Modules;

namespace Entia.Serializers
{
    public sealed class ReaderWriterLock : Serializer<ReaderWriterLockSlim>
    {
        public override bool Serialize(in ReaderWriterLockSlim instance, TypeData dynamic, TypeData @static, in WriteContext context)
        {
            context.Writer.Write(instance.RecursionPolicy);
            return true;
        }

        public override bool Instantiate(out ReaderWriterLockSlim instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            var success = context.Reader.Read(out LockRecursionPolicy policy);
            instance = new ReaderWriterLockSlim(policy);
            return success;
        }

        public override bool Deserialize(ref ReaderWriterLockSlim instance, TypeData dynamic, TypeData @static, in ReadContext context) => true;
    }
}