using System;
using System.Numerics;
using Entia.Core;
using Entia.Experimental;
using Entia.Injectables;

namespace Entia.Experiment.SIMD
{
    public sealed class Group<T> where T : unmanaged
    {
        public sealed class Vector : IInjectable
        {
            public int Count => throw null;
            public ref Vector<T> this[int index] => throw null;
        }
    }

    public sealed class Group<T1, T2> where T1 : unmanaged where T2 : unmanaged
    {
        public readonly struct Components1
        {
            public ref Vector<T1> this[int index] => throw null;
        }

        public readonly struct Components2
        {
            public ref Vector<T2> this[int index] => throw null;
        }

        public sealed class Vector : IInjectable
        {
            public int Count => throw null;
            public Components1 Components1 => throw null;
            public Components2 Components2 => throw null;
        }
    }

    public readonly struct Vector<T> : IComponent where T : unmanaged
    {
        public static readonly int Count;
        public static readonly Vector<T> Zero;
        public static readonly Vector<T> One;

        public static implicit operator Vector<T>(T value) => new Vector<T>(value);
        public static Vector<T> operator +(in Vector<T> left, in Vector<T> right) =>
            new Vector<T>(left._vector + right._vector);
        public static Vector<T> operator -(in Vector<T> left, in Vector<T> right) =>
            new Vector<T>(left._vector - right._vector);
        public static Vector<T> operator *(in Vector<T> left, in Vector<T> right) =>
            new Vector<T>(left._vector * right._vector);
        public static Vector<T> operator /(in Vector<T> left, in Vector<T> right) =>
            new Vector<T>(left._vector / right._vector);

        readonly System.Numerics.Vector<T> _vector;

        public Vector(T value) : this(new System.Numerics.Vector<T>(value)) { }
        public Vector(in System.Numerics.Vector<T> vector) { _vector = vector; }

        public Vector<TCast> As<TCast>() where TCast : unmanaged
        {
            var vector = this;
            return UnsafeUtility.As<Vector<T>, Vector<TCast>>(ref vector);
        }

        public void CopyTo<TTarget>(TTarget[] array, int index) where TTarget : unmanaged => throw null;
    }

    public static class Vector
    {
        public static Vector<T> Create<T>(T value) where T : unmanaged => new Vector<T>(value);
        public static Vector<T> Create<T>(T[] values, int index) where T : unmanaged => throw null;

        public static void Add<T>(ref this Vector<T> vector, in Vector<float> values) where T : unmanaged =>
            vector = (vector.As<float>() + values).As<T>();
        public static void Add<T>(ref this Vector<float> vector, in Vector<T> values) where T : unmanaged =>
            vector += values.As<float>();

        public static void Subtract<T>(ref this Vector<T> vector, in Vector<float> values) where T : unmanaged =>
            vector = (vector.As<float>() - values).As<T>();
        public static void Subtract<T>(ref this Vector<float> vector, in Vector<T> values) where T : unmanaged =>
            vector -= values.As<float>();

        public static void Multiply<T>(ref this Vector<T> vector, in Vector<float> values) where T : unmanaged =>
            vector = (vector.As<float>() * values).As<T>();
        public static void Multiply<T>(ref this Vector<float> vector, in Vector<T> values) where T : unmanaged =>
            vector *= values.As<float>();

        public static void Divide<T>(ref this Vector<T> vector, in Vector<float> values) where T : unmanaged =>
            vector = (vector.As<float>() / values).As<T>();
        public static void Divide<T>(ref this Vector<float> vector, in Vector<T> values) where T : unmanaged =>
            vector /= values.As<float>();
    }

    namespace Phases
    {
        public struct Run : IMessage { }
    }

    public struct Transform : IComponent { public Vector2 Position; public float Angle; }
    public struct Velocity : IComponent { public Vector2 Position; public float Angle; }
    public struct Friction : IResource { public Vector2 Position; public float Angle; }

    public static class Test
    {
        public static Node UpdateVelocity() => Node.Sequence(
            Node.Inject((Group<Position, Velocity>.Vector group) =>
            Node.System<Phases.Run>.Run(() =>
            {
                for (var i = 0; i < group.Count; i++) group.Components1[i].Add(group.Components2[i].As<float>());
            })),

            Node.System<Phases.Run>.RunEach((ref Vector<Transform> transforms, ref Vector<Velocity> velocities) =>
                transforms.Add(velocities.As<float>())),

            // Give direct access to stores?
            Node.System<Phases.Run>.RunEach((Entity[] entities, Transform[] transforms, Velocity[] velocities, int count) =>
            {
                var index = 0;
                for (; index < count; index += Vector<Transform>.Count)
                {
                    // var sum = Vector.As<float>(transforms, index) + Vector.As<float>(velocities, index);
                    // sum.CopyTo(transforms, index);
                }
                // deal with potential remaining transforms if 'count % Vector<Transform>.Count != 0'
            }),
            Node.System<Phases.Run>.RunEach((Transform[] transforms, Velocity[] velocities, int count) =>
            {
                var index = 0;
                for (; index < count; index += Vector<Transform>.Count)
                {
                    // var sum = Vector.As<float>(transforms, index) + Vector.As<float>(velocities, index);
                    // sum.CopyTo(transforms, index);
                }
                // deal with potential remaining transforms if 'count % Vector<Transform>.Count != 0'
            }),

            Node.System<Phases.Run>.RunEach((ref Transform transform) => transform.Angle %= (float)Math.PI * 2f),


            Node.Inject((Resource<Friction>.Read physics, Resource<Time>.Read time, Group<Velocity>.Vector velocities) =>
            Node.System<Phases.Run>.Run(() =>
            {
                var friction = Vector.Create(physics.Value);
                friction.Multiply(time.Value.Delta);
                var inverse = Vector<float>.One;
                inverse.Subtract(friction);
                for (var i = 0; i < velocities.Count; i++) velocities[i].Multiply(inverse);
            }))
        );
    }
}