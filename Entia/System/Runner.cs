using System;
using Entia.Core;
using System.Diagnostics;
using Entia.Injectables;
using System.Linq;
using static Entia.Experimental.Runner;
using Entia.Dependencies;
using System.Collections.Generic;
using Entia.Modules.Component;

namespace Entia.Experimental
{
    public static class Systems
    {
        public struct OnAwake : IMessage { }
        public struct OnStart : IMessage { }
        public struct OnUpdate : IMessage { }
        public struct OnDestroy : IMessage { }

        public struct Time : IResource { public TimeSpan Current, Delta; }
        public struct Position : IComponent { public float X, Y; }
        public struct Velocity : IComponent { public float X, Y; }
        public struct IsFrozen : IComponent { public TimeSpan Duration; }
        public struct Motion : IComponent { public float Speed, Jump, Acceleration; }
        public struct Physics : IComponent { public float Drag, Mass, Gravity; }
        public struct Input : IComponent { public float Direction; public bool Jump; }

        public static Runner Move() =>
            When<OnUpdate>.RunEach((ref Position position, ref Velocity velocity) =>
            {
                position.X += velocity.X;
                position.Y += velocity.Y;
            }, Filter.None<IsFrozen>());

        public static Runner Melt() =>
            With((Resource<Time> time, Components<IsFrozen> areFrozen) =>
            When<OnUpdate>.RunEach((Entity entity, ref IsFrozen isFrozen) =>
            {
                isFrozen.Duration -= time.Value.Delta;
                if (isFrozen.Duration <= TimeSpan.Zero) areFrozen.Remove(entity);
            }));

        public static Runner UpdateTime()
        {
            var watch = new Stopwatch();
            return All(
                When<OnAwake>.Run(watch.Start),
                When<OnDestroy>.Run(watch.Stop),
                When<OnUpdate>.Run((ref Time time) =>
                {
                    time.Delta = watch.Elapsed - time.Current;
                    time.Current = watch.Elapsed;
                }));
        }

        public static Runner UpdateVelocity() =>
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

        public static Runner UpdateInput()
        {
            var horizontal = 0f;
            var jump = false;
            return All(
                When<OnUpdate>.Run(() => { horizontal++; jump = !jump; }),
                When<OnUpdate>.RunEach((ref Input input) => (input.Jump, input.Direction) = (jump, horizontal)),
                When<OnUpdate>.Run(() => { horizontal--; jump = !jump; })
            );
        }
    }

    public readonly struct Filter
    {
        public static Filter All(params Filter[] filters) =>
            filters.Length == 0 ? True :
            filters.Length == 1 ? filters[0] :
            new Filter(segment =>
            {
                foreach (var filter in filters)
                {
                    if (filter.Match(segment)) continue;
                    return false;
                }
                return true;
            });
        public static Filter All<T>(params Filter[] filters) where T : IComponent =>
            All(filters.Prepend(Has<T>()));
        public static Filter All<T1, T2>(params Filter[] filters) where T1 : IComponent where T2 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>()));
        public static Filter All<T1, T2, T3>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>()));
        public static Filter All<T1, T2, T3, T4>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>()));
        public static Filter All<T1, T2, T3, T4, T5>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>()));
        public static Filter All<T1, T2, T3, T4, T5, T6>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>()));
        public static Filter All<T1, T2, T3, T4, T5, T6, T7>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent where T7 : IComponent =>
            All(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>(), Has<T7>()));

        public static Filter Any(params Filter[] filters) =>
            filters.Length == 0 ? False :
            filters.Length == 1 ? filters[0] :
            new Filter(segment =>
            {
                foreach (var filter in filters) if (filter.Match(segment)) return true;
                return false;
            });
        public static Filter Any<T>(params Filter[] filters) where T : IComponent =>
            Any(filters.Prepend(Has<T>()));
        public static Filter Any<T1, T2>(params Filter[] filters) where T1 : IComponent where T2 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>()));
        public static Filter Any<T1, T2, T3>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>()));
        public static Filter Any<T1, T2, T3, T4>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>()));
        public static Filter Any<T1, T2, T3, T4, T5>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>()));
        public static Filter Any<T1, T2, T3, T4, T5, T6>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>()));
        public static Filter Any<T1, T2, T3, T4, T5, T6, T7>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent where T7 : IComponent =>
            Any(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>(), Has<T7>()));

        public static Filter None(params Filter[] filters) => Not(Any(filters));
        public static Filter None<T>(params Filter[] filters) where T : IComponent =>
            None(filters.Prepend(Has<T>()));
        public static Filter None<T1, T2>(params Filter[] filters) where T1 : IComponent where T2 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>()));
        public static Filter None<T1, T2, T3>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>()));
        public static Filter None<T1, T2, T3, T4>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>()));
        public static Filter None<T1, T2, T3, T4, T5>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>()));
        public static Filter None<T1, T2, T3, T4, T5, T6>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>()));
        public static Filter None<T1, T2, T3, T4, T5, T6, T7>(params Filter[] filters) where T1 : IComponent where T2 : IComponent where T3 : IComponent where T4 : IComponent where T5 : IComponent where T6 : IComponent where T7 : IComponent =>
            None(filters.Prepend(Has<T1>(), Has<T2>(), Has<T3>(), Has<T4>(), Has<T5>(), Has<T6>(), Has<T7>()));

        public static Filter Not(Filter query) => new Filter(segment => !query.Match(segment));

        static Filter Has<T>() where T : IComponent =>
            ComponentUtility.TryGetConcreteMask<T>(out var mask) ?
            new Filter(segment => segment.Mask.HasAny(mask)) : False;

        public static readonly Filter True = new Filter(_ => true);
        public static readonly Filter False = new Filter(_ => false);

        public readonly Func<Segment, bool> Match;
        public Filter(Func<Segment, bool> match) { Match = match; }
    }

    public readonly struct Phase
    {
        public readonly Type Message;
        public readonly Delegate Run;
        public readonly IDependency[] Dependencies;

        public Phase(Type message, Delegate run, IEnumerable<IDependency> dependencies) :
            this(message, run, dependencies.ToArray())
        { }

        public Phase(Type message, Delegate run, params IDependency[] dependencies)
        {
            Message = message;
            Run = run;
            Dependencies = dependencies;
        }
        public Phase With(params IDependency[] dependencies) => new Phase(Message, Run, dependencies);
    }

    public delegate Result<Phase[]> Schedule(World world);

    public readonly partial struct Runner
    {
        public static partial class When<TMessage> where TMessage : struct, IMessage
        {
            static Result<Phase[]> Schedule(InAction<TMessage> run, params IDependency[] dependencies) =>
                new[] { new Phase(typeof(TMessage), run, dependencies.Prepend(new React(typeof(TMessage)))) };
        }

        public static Runner All(params Runner[] runners) => new Runner(world => runners
            .Select(runner => runner.Schedule(world))
            .All()
            .Map(phases => phases.SelectMany(_ => _)
                .GroupBy(phase => phase.Message)
                .Select(group => new Phase(
                    group.Key,
                    group.Select(phase => phase.Run).Aggregate(Delegate.Combine),
                    group.SelectMany(phase => phase.Dependencies)))
                .ToArray()));

        public readonly Schedule Schedule;
        public Runner(Schedule schedule) { Schedule = schedule; }
    }
}