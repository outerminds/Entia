using Entia.Core;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Modules.Query;
using Entia.Queriers;
using Entia.Queryables;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Entia.Experiment
{
    public unsafe static class QuerierTest
    {
        public delegate ref T Return<T>(void* input);
        public delegate IntPtr Return(IntPtr input);

        public static class Cache<T>
        {
            public static readonly Return<T> Return;

            static Cache()
            {
                var @return = new Return(_ => _);
                UnsafeUtility.Reinterpret(ref @return, ref Return);
            }
        }

        struct Query : Queryables.IQueryable
        {
            public Write<Position> Position;
            public Read<Velocity> Velocity;
        }

        public unsafe static void Run()
        {
            var array = new float[] { 1, 2, 3, 4, 5, 6, 7 };
            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var pointer1 = (float*)handle.AddrOfPinnedObject();
            var pointer2 = GCHandle.ToIntPtr(handle);
            var pointer3 = UnsafeUtility.ToPointer(ref array[0]);

            var value1 = pointer1[0];
            var value2 = pointer1[1];
            var value3 = pointer1[2];

            var p = (IntPtr)pointer3;
            var value4 = 0f;
            var value5 = 0f;
            var value6 = 0f;
            UnsafeUtility.Reinterpret(p, ref value4);
            UnsafeUtility.Reinterpret(p + 4, ref value5);
            UnsafeUtility.Reinterpret(p + 8, ref value6);

            ref var value7 = ref Cache<float>.Return(pointer3);

            var world = new World();
            var entities = world.Entities();
            var components = world.Components();
            var queriers = world.Queriers();
            var groups = world.Groups();

            for (int i = 0; i < 100; i++)
            {
                var entity = entities.Create();
                components.Set(entity, new Position { X = 1, Y = 2, Z = 3 });
                components.Set(entity, new Velocity { X = 4, Y = 5, Z = 6 });
            }
            world.Resolve();

            var group = groups.Get(new Generic<Query>());
            foreach (ref readonly var item in group)
            {
                ref var position = ref item.Position.Value;
                ref readonly var velocity = ref item.Velocity.Value;
                position.X += velocity.X;
                position.Y += velocity.Y;
                position.Z += velocity.Z;
            }
        }
    }

    unsafe struct Field
    {
        public static readonly int Size = 16;//sizeof(void*) + sizeof(int);

        public void* Store;
        public int Index;
    }

    public sealed class Generic<T> : Querier<T> where T : struct, Queryables.IQueryable
    {
        static readonly FieldInfo[] _fields = typeof(T).GetFields(TypeUtility.Instance);
        static readonly int[] _components = _fields
            .Select(field => ComponentUtility.GetMetadata(field.FieldType.GetGenericArguments()[0]).Index)
            .ToArray();
        static readonly int _size = Field.Size * _fields.Length;

        public unsafe override bool TryQuery(Segment segment, World world, out Query<T> query)
        {
            query = new Query<T>(index =>
            {
                var pointer = stackalloc byte[_size];
                var current = (Field*)pointer;
                for (var i = 0; i < _components.Length; i++, current++)
                {
                    var component = _components[i];
                    var store = segment.Store(component);
                    current->Store = *(void**)UnsafeUtility.ToPointer(ref store);
                    current->Index = index;
                }

                var queryable = default(T);
                UnsafeUtility.Reinterpret(ref *pointer, ref queryable);
                return queryable;
            });
            return _components.All(segment.Mask.Has);
        }
    }
}