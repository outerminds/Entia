using Entia.Core;
using System;
using System.Linq;

namespace Entia.Modules
{
    public static class ModuleUtility
    {
        public static TValue Default<TKey, TValue>(this Concurrent<TypeMap<TKey, TValue>> map, Type type, Type definition, Type attribute, Func<TValue> @default = null)
            where TKey : class where TValue : class =>
            map.ReadValueOrWrite(type,
                (type, definition, attribute, @default),
                state => Default<TValue>(state.type, state.definition, state.attribute).Or(() => state.@default?.Invoke()));

        public static TValue Default<TKey, TValue>(this TypeMap<TKey, TValue> map, Type type, Type definition, Type attribute, Func<TValue> @default = null)
            where TKey : class where TValue : class =>
            map.TryGet(type, out var value, false) ? value :
            map[type] = Default<TValue>(type, definition, attribute).Or(() => @default?.Invoke());

        public static TValue Default<TKey, TValue>(this TypeMap<TKey, TValue> map, Type type, Type definition, Type attribute, Type @default)
            where TKey : class where TValue : class =>
            map.Default(
                type, definition, attribute,
                @default.IsGenericType ?
                    new Func<TValue>(() => Activator.CreateInstance(@default.MakeGenericType(type)) as TValue) :
                    new Func<TValue>(() => Activator.CreateInstance(@default) as TValue));

        public static Result<T> Default<T>(Type type, Type definition = null, Type attribute = null) where T : class
        {
            var result = Result.Failure().AsResult<T>();

            if (attribute != null && result.IsFailure())
            {
                result = TypeUtility.GetFields(type, TypeUtility.Static)
                    .Where(field => field.FieldType.Is<T>() && field.GetCustomAttributes(true).Any(current => current.GetType().Is(attribute)))
                    .Select(field => Result.Try(field.GetValue, default(object)).Cast<T>())
                    .Any();
            }

            if (attribute != null && result.IsFailure())
            {
                result = type.GetProperties(TypeUtility.Static)
                    .Where(property => property.PropertyType.Is<T>() && property.GetCustomAttributes(true).Any(current => current.GetType().Is(attribute)))
                    .Select(property => Result.Try(property.GetValue, default(object)).Cast<T>())
                    .Any();
            }

            if (definition != null && result.IsFailure())
            {
                result = type.Hierarchy()
                    .Where(child => child.IsGenericType && child.GetGenericTypeDefinition() == definition)
                    .SelectMany(child => child.GetGenericArguments().Select(argument => Result.Try(Activator.CreateInstance, argument).Cast<T>()))
                    .Any();
            }

            return result;
        }
    }
}
