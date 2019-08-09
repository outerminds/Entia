using System;
using System.Collections.Generic;
using System.Reflection;
using Entia.Core;
using Entia.Modules;
using static Entia.Experiment.Serializer;

namespace Entia.Experiment
{
    public interface IDescriptor
    {
        ISerializer Describe(Type type, World world);
    }

    public abstract class Descriptor<T> : IDescriptor
    {
        public abstract Serializer<T> Describe(Type type, World world);
        ISerializer IDescriptor.Describe(Type type, World world) => Describe(type, world);
    }

    [AttributeUsage(ModuleUtility.AttributeUsage, Inherited = true, AllowMultiple = false)]
    public sealed class DescriptorAttribute : PreserveAttribute { }

    public sealed class Default : IDescriptor
    {
        public ISerializer Describe(Type type, World world)
        {
            var data = TypeUtility.GetData(type);
            switch (data.Code)
            {
                case TypeCode.Boolean: return Blittable.Object<bool>();
                case TypeCode.Byte: return Blittable.Object<byte>();
                case TypeCode.Char: return Blittable.Object<char>();
                case TypeCode.DateTime: return Blittable.Object<DateTime>();
                case TypeCode.Decimal: return Blittable.Object<decimal>();
                case TypeCode.Double: return Blittable.Object<double>();
                case TypeCode.Int16: return Blittable.Object<short>();
                case TypeCode.Int32: return Blittable.Object<int>();
                case TypeCode.Int64: return Blittable.Object<long>();
                case TypeCode.SByte: return Blittable.Object<sbyte>();
                case TypeCode.Single: return Blittable.Object<float>();
                case TypeCode.UInt16: return Blittable.Object<ushort>();
                case TypeCode.UInt32: return Blittable.Object<uint>();
                case TypeCode.UInt64: return Blittable.Object<ulong>();
                case TypeCode.String: return String();
                default:
                    if (type.IsArray)
                    {
                        var descriptors = world.Descriptors();
                        var element = TypeUtility.GetData(data.Element);
                        switch (element.Code)
                        {
                            case TypeCode.Boolean: return Blittable.Array<bool>();
                            case TypeCode.Byte: return Blittable.Array<byte>();
                            case TypeCode.Char: return Blittable.Array<char>();
                            case TypeCode.DateTime: return Blittable.Array<DateTime>();
                            case TypeCode.Decimal: return Blittable.Array<decimal>();
                            case TypeCode.Double: return Blittable.Array<double>();
                            case TypeCode.Int16: return Blittable.Array<short>();
                            case TypeCode.Int32: return Blittable.Array<int>();
                            case TypeCode.Int64: return Blittable.Array<long>();
                            case TypeCode.SByte: return Blittable.Array<sbyte>();
                            case TypeCode.Single: return Blittable.Array<float>();
                            case TypeCode.UInt16: return Blittable.Array<ushort>();
                            case TypeCode.UInt32: return Blittable.Array<uint>();
                            case TypeCode.UInt64: return Blittable.Array<ulong>();
                            default:
                                if (element.Size is int size) return Blittable.Array(element, size);
                                else return Array(element);
                        }
                    }
                    else if (data.Size is int size) return Blittable.Object(data, size);
                    else if (type.Is<Type>()) return Serializer.Reflection.Type();
                    else if (type.Is<Assembly>()) return Serializer.Reflection.Assembly();
                    else if (type.Is<Module>()) return Serializer.Reflection.Module();
                    else if (type.Is<MethodInfo>()) return Serializer.Reflection.Method();
                    else if (type.Is<MemberInfo>()) return Serializer.Reflection.Member();
                    else if (type.Is<Delegate>()) return Delegate(type);
                    else if (type.Is(typeof(List<>), definition: true)) return List(data.Arguments[0]);
                    else if (type.Is(typeof(Dictionary<,>), definition: true)) return Dictionary(data.Arguments[0], data.Arguments[1]);
                    else return Object(type);
            }
        }
    }
}