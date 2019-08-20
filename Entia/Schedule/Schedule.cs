using System.Linq;
using Entia.Core;
using Entia.Modules.Schedule;
using Entia.Schedulers;

namespace Entia.Schedule
{
    public readonly struct Context
    {
        public readonly object Instance;
        public readonly Controller Controller;
        public readonly World World;

        public Context(Controller controller, World world) : this(null, controller, world) { }
        public Context(object instance, Controller controller, World world)
        {
            Instance = instance;
            Controller = controller;
            World = world;
        }

        public Phase[] Schedule(object instance) => World.Container.Get<IScheduler>(instance.GetType())
            .SelectMany(this, (scheduler, state) => scheduler.Schedule(state.With(instance)))
            .ToArray();

        public Context With(object instance = null) => new Context(instance ?? Instance, Controller, World);
    }

    public static class Extensions
    {
        public static Phase[] Schedule(this World world, object instance, Controller controller) =>
            new Context(controller, world).Schedule(instance);
        public static void Add<T>(this Container container, Scheduler<T> scheduler) =>
            container.Add<T, IScheduler>(scheduler);
    }
}