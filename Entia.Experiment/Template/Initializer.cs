using Entia.Core;
using System;
using System.Reflection;

namespace Entia.Initializers
{
    public interface IInitializer
    {
        Result<Unit> Initialize(object instance, object[] instances);
    }

    public abstract class Initializer<T> : IInitializer
    {
        public abstract Result<Unit> Initialize(T instance, object[] instances);
        Result<Unit> IInitializer.Initialize(object instance, object[] instances)
        {
            var result = Result.Cast<T>(instance);
            if (result.TryValue(out var casted)) return Initialize(casted, instances);
            return result.Fail();
        }
    }

    public sealed class Identity : IInitializer
    {
        public Result<Unit> Initialize(object instance, object[] instances) => Result.Success();
    }

    public sealed class Array : Initializer<System.Array>
    {
        public readonly (int index, int reference)[] Items;

        public Array(params (int index, int reference)[] items) { Items = items; }

        public override Result<Unit> Initialize(System.Array instance, object[] instances)
        {
            try
            {
                for (int i = 0; i < Items.Length; i++)
                {
                    var (index, reference) = Items[i];
                    instance.SetValue(instances[reference], index);
                }
                return Result.Success();
            }
            catch (Exception exception) { return Result.Exception(exception); }
        }
    }

    public sealed class Object : IInitializer
    {
        public readonly (FieldInfo field, int reference)[] Fields;
        public readonly (PropertyInfo property, int reference)[] Properties;

        public Object((FieldInfo field, int reference)[] fields = null, (PropertyInfo property, int reference)[] properties = null)
        {
            Fields = fields ?? System.Array.Empty<(FieldInfo, int)>();
            Properties = properties ?? System.Array.Empty<(PropertyInfo, int)>();
        }

        public Result<Unit> Initialize(object instance, object[] instances)
        {
            try
            {
                for (int i = 0; i < Fields.Length; i++)
                {
                    var (field, reference) = Fields[i];
                    field.SetValue(instance, instances[reference]);
                }
                for (int i = 0; i < Properties.Length; i++)
                {
                    var (property, reference) = Properties[i];
                    property.SetValue(instance, instances[reference]);
                }
                return Result.Success();
            }
            catch (Exception exception) { return Result.Exception(exception); }
        }
    }
}
