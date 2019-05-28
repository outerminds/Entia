using Entia.Core;
using Entia.Modules;
using Entia.Systems;
using System;
using System.Linq;
using System.Reflection;

namespace Entia.Injectors
{
    public sealed class System : Injector<ISystem>
    {
        public override Result<ISystem> Inject(MemberInfo member, World world)
        {
            switch (member)
            {
                case Type type:
                    var injectors = world.Injectors();
                    return Result
                        .Cast<ISystem>(DefaultUtility.Default(type))
                        .Bind(instance => type.InstanceFields()
                            .Where(field => field.IsPublic)
                            .Select(field => injectors.Inject(field).Do(current => field.SetValue(instance, current)))
                            .All()
                            .Return(instance));
                case FieldInfo field: return Inject(field.FieldType, world);
                default: return Failure.Empty;
            }
        }
    }
}
