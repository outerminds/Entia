using System.Linq;
using Entia.Core;
using Entia.Modules.Schedule;
using Entia.Schedulables;
using Entia.Schedulers;

namespace Entia.Schedule
{
    public readonly struct Context
    {
        public readonly ISchedulable Instance;
        public readonly Controller Controller;
        public readonly World World;

        public Context(Controller controller, World world) : this(null, controller, world) { }
        public Context(ISchedulable instance, Controller controller, World world)
        {
            Instance = instance;
            Controller = controller;
            World = world;
        }

        public Phase[] Schedule(ISchedulable instance) => World.Container.Get<IScheduler>(instance.GetType())
            .SelectMany(this, (scheduler, state) => scheduler.Schedule(state.With(instance)))
            .ToArray();

        public Context With(ISchedulable instance = null) => new Context(instance ?? Instance, Controller, World);
    }

    public static class Extensions
    {
        public static Phase[] Schedule(this World world, ISchedulable instance, Controller controller) =>
            new Context(controller, world).Schedule(instance);
        public static void Add<T>(this Container container, Scheduler<T> scheduler) where T : ISchedulable =>
            container.Add<T, IScheduler>(scheduler);
    }
}