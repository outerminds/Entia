using Entia.Core;
using Entia.Injection;
using Entia.Systems;
using System;
using System.Linq;
using System.Reflection;

namespace Entia.Injectors
{
    public sealed class System : Injector<ISystem>
    {
        public override Result<ISystem> Inject(in Context context) => context.Member switch
        {
            Type type when DefaultUtility.Default(type) is ISystem instance => type.InstanceFields()
                .Where(field => field.IsPublic)
                .Select(context, (field, state) => state.Inject(field).Do(value => field.SetValue(instance, value)))
                .All()
                .Return(instance),
            FieldInfo field => context.Inject<ISystem>(field.FieldType),
            _ => Result.Failure(),
        };
    }
}
