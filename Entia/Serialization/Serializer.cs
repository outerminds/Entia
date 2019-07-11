using System;
using System.Runtime.Serialization;
using Entia.Core;
using Entia.Modules;

namespace Entia.Serializers
{
    public interface ISerializer
    {
        bool Serialize(object instance, TypeData dynamic, TypeData @static, in WriteContext context);
        bool Instantiate(out object instance, TypeData dynamic, TypeData @static, in ReadContext context);
        bool Deserialize(ref object instance, TypeData dynamic, TypeData @static, in ReadContext context);
    }

    public abstract class Serializer<T> : ISerializer
    {
        static readonly Default _default = new Default();

        public abstract bool Serialize(in T instance, TypeData dynamic, TypeData @static, in WriteContext context);
        bool ISerializer.Serialize(object instance, TypeData dynamic, TypeData @static, in WriteContext context) =>
            instance is T casted && Serialize(casted, dynamic, @static, context);

        public virtual bool Instantiate(out T instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            if (_default.Instantiate(out var value, dynamic, @static, context) && value is T casted)
            {
                instance = casted;
                return true;
            }
            instance = default;
            return false;
        }
        bool ISerializer.Instantiate(out object instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            if (Instantiate(out var value, dynamic, @static, context))
            {
                instance = value;
                return true;
            }
            instance = default;
            return false;
        }

        public abstract bool Deserialize(ref T instance, TypeData dynamic, TypeData @static, in ReadContext context);
        bool ISerializer.Deserialize(ref object instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            if (instance is T casted && Deserialize(ref casted, dynamic, @static, context))
            {
                instance = casted;
                return true;
            }
            return false;
        }
    }

    [AttributeUsage(ModuleUtility.AttributeUsage, Inherited = true, AllowMultiple = false)]
    public sealed class SerializerAttribute : PreserveAttribute { }

    public sealed class Default : ISerializer
    {
        public unsafe bool Serialize(object instance, TypeData dynamic, TypeData @static, in WriteContext context)
        {
            switch (dynamic.Code)
            {
                case TypeCode.Boolean: context.Writer.Write((bool)instance); return true;
                case TypeCode.Byte: context.Writer.Write((byte)instance); return true;
                case TypeCode.Char: context.Writer.Write((char)instance); return true;
                case TypeCode.Decimal: context.Writer.Write((decimal)instance); return true;
                case TypeCode.Double: context.Writer.Write((double)instance); return true;
                case TypeCode.Int16: context.Writer.Write((short)instance); return true;
                case TypeCode.Int32: context.Writer.Write((int)instance); return true;
                case TypeCode.Int64: context.Writer.Write((long)instance); return true;
                case TypeCode.SByte: context.Writer.Write((sbyte)instance); return true;
                case TypeCode.Single: context.Writer.Write((float)instance); return true;
                case TypeCode.String: context.Writer.Write((string)instance); return true;
                case TypeCode.UInt16: context.Writer.Write((ushort)instance); return true;
                case TypeCode.UInt32: context.Writer.Write((uint)instance); return true;
                case TypeCode.UInt64: context.Writer.Write((ulong)instance); return true;
                case TypeCode.DateTime: context.Writer.Write((DateTime)instance); return true;
                default:
                    var fields = dynamic.InstanceFields;
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        var value = field.GetValue(instance);
                        context.Serializers.Serialize(value, field.FieldType, context);
                    }
                    return true;
            }
        }

        public bool Instantiate(out object instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            switch (dynamic.Code)
            {
                case TypeCode.Boolean: { context.Reader.Read(out bool value); instance = value; return true; }
                case TypeCode.Byte: { context.Reader.Read(out byte value); instance = value; return true; }
                case TypeCode.Char: { context.Reader.Read(out char value); instance = value; return true; }
                case TypeCode.Decimal: { context.Reader.Read(out decimal value); instance = value; return true; }
                case TypeCode.Double: { context.Reader.Read(out double value); instance = value; return true; }
                case TypeCode.Int16: { context.Reader.Read(out short value); instance = value; return true; }
                case TypeCode.Int32: { context.Reader.Read(out int value); instance = value; return true; }
                case TypeCode.Int64: { context.Reader.Read(out long value); instance = value; return true; }
                case TypeCode.SByte: { context.Reader.Read(out sbyte value); instance = value; return true; }
                case TypeCode.Single: { context.Reader.Read(out float value); instance = value; return true; }
                case TypeCode.String: { context.Reader.Read(out string value); instance = value; return true; }
                case TypeCode.UInt16: { context.Reader.Read(out ushort value); instance = value; return true; }
                case TypeCode.UInt32: { context.Reader.Read(out uint value); instance = value; return true; }
                case TypeCode.UInt64: { context.Reader.Read(out ulong value); instance = value; return true; }
                case TypeCode.DateTime: { context.Reader.Read(out DateTime value); instance = value; return true; }
                default:
                    if (@static.Type.IsValueType) { instance = @static.Default; return true; }
                    else if (dynamic.Type.IsValueType) { instance = CloneUtility.Shallow(dynamic.Default); return true; }
                    else { instance = FormatterServices.GetUninitializedObject(dynamic); return true; }
            }
        }

        public bool Deserialize(ref object instance, TypeData dynamic, TypeData @static, in ReadContext context)
        {
            switch (dynamic.Code)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.String:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.DateTime: return true;
                default:
                    var fields = dynamic.InstanceFields;
                    var success = true;
                    if (fields.Length > 0)
                    {
                        for (int i = 0; i < fields.Length; i++)
                        {
                            var field = fields[i];
                            if (context.Serializers.Deserialize(out var value, field.FieldType, context))
                                field.SetValue(instance, value);
                            else
                                success = false;
                        }
                    }
                    return success;
            }
        }
    }
}