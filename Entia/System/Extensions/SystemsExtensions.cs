using System;
using Entia.Core;
using Entia.Modules;
using Entia.Phases;

namespace Entia.Experimental.Modules
{
    public static class SystemsExtensions
    {
        public static Runner<TPhase, Action> Schedule<TPhase>(in this Runner<Action> runner) where TPhase : struct, IPhase =>
            runner.Schedule<TPhase>(run => (in TPhase _) => run());

        public static Runner<Action> Run(this Systems _, Action run) => new Runner<Action>(run);
        public static Runner<InAction<T>> Run<T>(this Systems _, InAction<T> run) => new Runner<InAction<T>>(run);

        // public static Runner React<TMessage>(this Systems systems, InAction<TMessage> run) where TMessage : struct, IMessage
        // {
        //     var messages = systems.World.Messages();
        //     var reaction = messages.Reaction<TMessage>();
        //     var runner = systems.Run(run).Schedule<React<TMessage>>(run => (in React<TMessage> phase) => run(phase.Message));
        //     return systems.All(
        //         systems.Run(() => reaction.Add(run)).Schedule<React.Initialize>(),
        //         systems.Run(() => reaction.Remove(run)).Schedule<React.Dispose>());
        // }

        // public static Runner All(this Systems _, params Runner[] runners)
        // {

        // }

        /*

        struct Position : IComponent { public float X, Y; }
        struct Velocity : IComponent { public float X, Y; }

        public static Runner Motion(World world)
        {
            var group1 = world.Groups().Get(...);
            var group2 = world.Groups().Get(...);
            return world.Systems().Run(() =>
            {
                foreach (var item1 in group1)
                {
                    foreach (var item2 in group2)
                    {

                    }
                }
            });
        }

        public static System Motion(World world)
        {
            var systems = world.Systems();
            systems.All(
                systems.Schedule<Initialize>(systems.Run(() => {...})),
                systems.Run(() => {...}).Schedule<Dispose>(),
                systems.Run(() => new { Index: 0 }, state => { state.Index++; }),
            );
        }

        public static System All(this Systems module, params System[] systems)
        {
             merge type maps
        }

        public static System<Run, Action> Run<T>(this Systems module, Func<T> initialize, Func<T, T> run, Action<T> dispose = null)
        {

        }
        public static System<Run, Action> Run<T>(this Systems module, Func<T> initialize, Action<T> run, Action<T> dispose = null)
        {

        }
        public static System<Run, Action> Run<T1...T7>(this Systems module, RefAction<T1...T7> run) where T1...T7 : struct, IResource
        {

        }

        public static System RunEach<T1...T7>(this Systems module, RefAction<T1...T7> run, params QueryableAttribute[] queries) where T1...T7 : struct, IComponent
        {

        }

        public static System<React<T>, InAction<T>> React<T>(this Systems module, InAction<T> run) where T : struct, IMessage
        {
            var messages = module.World.Messages();
            var reaction = messages.Reaction<T>();
        }

        public static System ReactEach<T, T1...T6>(this Systems module, InRefAction<T, T1...T6> run, params QueryableAttribute[] queries) where T : struct, IMessage, where T1...T6 : struct, IComponent
        {

        }
        */
    }
}