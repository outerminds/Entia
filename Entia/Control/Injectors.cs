using Entia.Core;
using Entia.Injection;
using Entia.Modules;
using Entia.Systems;
using System;
using System.Linq;
using System.Reflection;

namespace Entia.Injectors
{
    public sealed class System : Injector<ISystem>
    {
        public override Result<ISystem> Inject(in Context context)
        {
            switch (context.Member)
            {
                case Type type when DefaultUtility.Default(type) is ISystem instance:
                    return type.InstanceFields()
                        .Where(field => field.IsPublic)
                        .Select(context, (field, state) => state.Inject(field).Do(value => field.SetValue(instance, value)))
                        .All()
                        .Return(instance);
                case FieldInfo field: return context.Inject<ISystem>(field.FieldType);
                default: return Failure.Empty;
            }
        }
    }
}
