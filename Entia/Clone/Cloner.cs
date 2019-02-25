using System;
using Entia.Core;
using Entia.Modules;

namespace Entia.Cloners
{
    public interface ICloner
    {
        Result<object> Clone(object instance, TypeData type, World world);
    }

    public abstract class Cloner<T> : ICloner
    {
        public abstract Result<T> Clone(T instance, TypeData type, World world);
        Result<object> ICloner.Clone(object instance, TypeData type, World world)
        {
            var result = Result.Cast<T>(instance);
            if (result.TryValue(out var casted)) return Clone(casted, type, world);
            return result.Box();
        }
    }

    public sealed class Default : ICloner
    {
        public Result<object> Clone(object instance, TypeData type, World world)
        {
            if (type.IsPlain || TypeUtility.IsPrimitive(instance)) return instance;

            switch (instance)
            {
                case Array array:
                    {
                        var clone = array.Clone() as Array;
                        var element = TypeUtility.GetData(array.GetType().GetElementType());
                        if (element.IsPlain) return clone;

                        var cloners = world.Cloners();
                        for (int i = 0; i < clone.Length; i++)
                        {
                            var result = cloners.Clone(clone.GetValue(i), element);
                            if (result.TryValue(out var item)) clone.SetValue(item, i);
                            else return result.AsFailure();
                        }
                        return clone;
                    }
                default:
                    {
                        var clone = CloneUtility.Shallow(instance);
                        var cloners = world.Cloners();
                        for (int i = 0; i < type.InstanceFields.Length; i++)
                        {
                            var field = type.InstanceFields[i];
                            var data = TypeUtility.GetData(field.FieldType);
                            if (data.IsPlain) continue;

                            var result = cloners.Clone(field.GetValue(clone), data);
                            if (result.TryValue(out var value)) field.SetValue(clone, value);
                            else return result.AsFailure();
                        }
                        return clone;
                    }
            }
        }
    }

    [AttributeUsage(ModuleUtility.AttributeUsage)]
    public sealed class ClonerAttribute : PreserveAttribute { }
}