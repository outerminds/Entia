/* DO NOT MODIFY: The content of this file has been generated by the script 'Node.With.csx'. */

using System;
using Entia.Core;
using Entia.Injectables;
using Entia.Injection;
using Entia.Dependency;

namespace Entia.Experimental
{
    public sealed partial class Node
    {
        public static Node With<T>(Func<T, Node> provide) where T : IInjectable => Lazy(world =>
        {
            var result1 = world.Inject<T>();
            if (result1.TryValue(out var value1))
                return provide(value1).Depend(new[] { world.Dependencies<T>() }.Flatten());
            else
                return Result.Failure(ArrayUtility.Concatenate(result1.Messages()));
        });
        public static Node With<T1, T2>(Func<T1, T2, Node> provide) where T1 : IInjectable where T2 : IInjectable => Lazy(world =>
        {
            var result1 = world.Inject<T1>(); var result2 = world.Inject<T2>();
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2))
                return provide(value1, value2).Depend(new[] { world.Dependencies<T1>(), world.Dependencies<T2>() }.Flatten());
            else
                return Result.Failure(ArrayUtility.Concatenate(result1.Messages(), result2.Messages()));
        });
        public static Node With<T1, T2, T3>(Func<T1, T2, T3, Node> provide) where T1 : IInjectable where T2 : IInjectable where T3 : IInjectable => Lazy(world =>
        {
            var result1 = world.Inject<T1>(); var result2 = world.Inject<T2>(); var result3 = world.Inject<T3>();
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3))
                return provide(value1, value2, value3).Depend(new[] { world.Dependencies<T1>(), world.Dependencies<T2>(), world.Dependencies<T3>() }.Flatten());
            else
                return Result.Failure(ArrayUtility.Concatenate(result1.Messages(), result2.Messages(), result3.Messages()));
        });
        public static Node With<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Node> provide) where T1 : IInjectable where T2 : IInjectable where T3 : IInjectable where T4 : IInjectable => Lazy(world =>
        {
            var result1 = world.Inject<T1>(); var result2 = world.Inject<T2>(); var result3 = world.Inject<T3>(); var result4 = world.Inject<T4>();
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3) && result4.TryValue(out var value4))
                return provide(value1, value2, value3, value4).Depend(new[] { world.Dependencies<T1>(), world.Dependencies<T2>(), world.Dependencies<T3>(), world.Dependencies<T4>() }.Flatten());
            else
                return Result.Failure(ArrayUtility.Concatenate(result1.Messages(), result2.Messages(), result3.Messages(), result4.Messages()));
        });
        public static Node With<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Node> provide) where T1 : IInjectable where T2 : IInjectable where T3 : IInjectable where T4 : IInjectable where T5 : IInjectable => Lazy(world =>
        {
            var result1 = world.Inject<T1>(); var result2 = world.Inject<T2>(); var result3 = world.Inject<T3>(); var result4 = world.Inject<T4>(); var result5 = world.Inject<T5>();
            if (result1.TryValue(out var value1) && result2.TryValue(out var value2) && result3.TryValue(out var value3) && result4.TryValue(out var value4) && result5.TryValue(out var value5))
                return provide(value1, value2, value3, value4, value5).Depend(new[] { world.Dependencies<T1>(), world.Dependencies<T2>(), world.Dependencies<T3>(), world.Dependencies<T4>(), world.Dependencies<T5>() }.Flatten());
            else
                return Result.Failure(ArrayUtility.Concatenate(result1.Messages(), result2.Messages(), result3.Messages(), result4.Messages(), result5.Messages()));
        });
    }
}