using System;
using System.Diagnostics;
using Entia.Core;
using Entia.Experimental.Scheduling;
using Entia.Injectables;
using Entia.Modules;
using static Entia.Experimental.Node;

namespace Entia.Experimental
{
    public static class Systemz
    {
        public struct OnAwake : IMessage { }
        public struct OnStart : IMessage { }
        public struct OnUpdate : IMessage { }
        public struct OnDestroy : IMessage { }

        public struct OnFreeze : IMessage { }

        public struct Time : IResource { public TimeSpan Current, Delta; }
        public struct Position : IComponent { public float X, Y; }
        public struct Velocity : IComponent { public float X, Y; }
        public struct IsFrozen : IComponent { public TimeSpan Duration; }
        public struct Motion : IComponent { public float Speed, Jump, Acceleration; }
        public struct Physics : IComponent { public float Drag, Mass, Gravity; }
        public struct Input : IComponent { public float Direction; public bool Jump; }

        public static readonly Node Node = Sequence(Move, Melt, UpdateTime, UpdateVelocity, UpdateInput);

        public static Node Move() =>
            When<OnUpdate>.RunEach((ref Position position, ref Velocity velocity) =>
            {
                position.X += velocity.X;
                position.Y += velocity.Y;
            }, Filter.None<IsFrozen>());

        public static Node Melt() =>
            With((Resource<Time> time, Components<IsFrozen> areFrozen) =>
            When<OnUpdate>.RunEach((Entity entity, ref IsFrozen isFrozen) =>
            {
                isFrozen.Duration -= time.Value.Delta;
                if (isFrozen.Duration <= TimeSpan.Zero) areFrozen.Remove(entity);
            }));

        public static Node UpdateTime()
        {
            var watch = new Stopwatch();
            return Sequence(
                When<OnAwake>.Run(watch.Start),
                When<OnDestroy>.Run(watch.Stop),
                When<OnUpdate>.Run((ref Time time) =>
                {
                    time.Delta = watch.Elapsed - time.Current;
                    time.Current = watch.Elapsed;
                }));
        }

        public static Node UpdateVelocity() =>
            With((Resource<Time> time) =>
            When<OnUpdate>.RunEach((ref Velocity velocity, ref Motion motion, ref Physics physics, ref Input input) =>
            {
                var delta = (float)time.Value.Delta.TotalSeconds;
                var drag = 1f - physics.Drag * delta;
                velocity.X *= drag;
                velocity.Y *= drag;

                var move = input.Direction * motion.Acceleration / physics.Mass;
                velocity.X += move * delta;

                if (input.Jump) velocity.Y += motion.Jump / physics.Mass;
                velocity.Y += physics.Gravity * delta;

                // Clamp horizontal velocity.
                if (velocity.X < -motion.Speed)
                    velocity.X = -motion.Speed;
                if (velocity.X > motion.Speed)
                    velocity.X = motion.Speed;
            }));

        public static Node UpdateInput()
        {
            var horizontal = 0f;
            var jump = false;
            return Sequence(
                When<OnUpdate>.Run(() => { horizontal++; jump = !jump; }),
                When<OnUpdate>.RunEach((ref Input input) => (input.Jump, input.Direction) = (jump, horizontal)),
                When<OnUpdate>.Run(() => { horizontal--; jump = !jump; })
            );
        }
    }
}