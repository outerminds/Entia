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
            if (result.TryValue(out var casted)) return Clone(casted, type, world).Box();
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
                        var cloneType = TypeUtility.GetData(clone.GetType());
                        var elementType = TypeUtility.GetData(cloneType.Element);
                        if (elementType.IsPlain) return clone;

                        var cloners = world.Cloners();
                        for (int i = 0; i < clone.Length; i++)
                        {
                            var result = cloners.Clone(clone.GetValue(i), elementType);
                            if (result.TryValue(out var item)) clone.SetValue(item, i);
                            else return result;
                        }
                        return clone;
                    }
                default:
                    {
                        // NOTE: use the visible type to skip shallow cloning
                        var clone = type.Type.IsValueType ? instance : CloneUtility.Shallow(instance);
                        var cloners = world.Cloners();
                        // NOTE: use the runtime type to iterate on fields
                        var cloneType = TypeUtility.GetData(clone.GetType());
                        // NOTE: the visible type might've been a reference type but if the runtime type is plain, return early
                        if (cloneType.IsPlain) return clone;

                        for (int i = 0; i < cloneType.InstanceFields.Length; i++)
                        {
                            var field = cloneType.InstanceFields[i];
                            var fieldType = TypeUtility.GetData(field.FieldType);
                            if (fieldType.IsPlain) continue;

                            var result = cloners.Clone(field.GetValue(clone), fieldType);
                            if (result.TryValue(out var value)) field.SetValue(clone, value);
                            else return result;
                        }
                        return clone;
                    }
            }
        }
    }

    [AttributeUsage(ModuleUtility.AttributeUsage)]
    public sealed class ClonerAttribute : PreserveAttribute { }
}