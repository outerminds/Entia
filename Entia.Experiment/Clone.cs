// using System;
// using Entia.Cloners;
// using Entia.Core;
// using Entia.Modules;
// using Entia.Modules.Component;

// namespace Entia.Cloneables
// {
//     public interface ICloneable { }
//     public interface ICloneable<T> where T : ICloner { }
// }

// namespace Entia.Cloners
// {
//     public interface ICloner
//     {
//         object Clone(object instance, World world);
//     }

//     public abstract class Cloner<T> : ICloner
//     {
//         public abstract T Clone(T instance, World world);
//         object ICloner.Clone(object instance, World world) => instance is T casted ? Clone(casted, world) : CloneUtility.Shallow(instance);
//     }

//     public sealed class Default : ICloner
//     {
//         public object Clone(object instance, World world) => CloneUtility.Shallow(instance);
//     }

//     [AttributeUsage(ModuleUtility.AttributeUsage, Inherited = true, AllowMultiple = false)]
//     public sealed class ClonerAttribute : Attribute { }

//     sealed class Cloner : Cloner<Components>
//     {
//         public override Components Clone(Components instance, World world)
//         {
//             // NOTE: resolving ensures that the transient operations are completed
//             instance.Resolve();

//             // TODO: BUG: if entities/messages are not cloned, these reference will be broken
//             var messages = world.Messages();

//             var segments = new Segment[instance._segments.Length];
//             for (int i = 0; i < segments.Length; i++) segments[i] = instance._segments[i].Clone();

//             // NOTE: setting segments by index will work since no entity is in the special 'created/destroyed' segments
//             var data = instance._data.Clone();
//             for (int i = 0; i < data.count; i++)
//             {
//                 ref var datum = ref data.items[i];
//                 if (datum.IsValid) datum.Segment = segments[datum.Segment.Index];
//             }

//             // NOTE: many component operations assume that the delegates are initialized
//             var delegates = new Delegates[instance._delegates.Length];
//             for (int i = 0; i < delegates.Length; i++)
//             {
//                 ref readonly var current = ref instance._delegates[i];
//                 if (current.IsValid) delegates[i] = current.Clone(messages);
//             }

//             return new Components(messages, data, segments, delegates);
//         }
//     }
// }