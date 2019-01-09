using Entia.Modules;

// NOTE: actions represent modifications to be made on an entity;
// their main use would be to initialize an entity on creation to prevent the overhead of moving the entity multiple times
namespace Entia.Experiment1.Actions
{
    public interface IAction
    {
        void Do(Entity entity, World world);
    }

    public static class Action
    {
        public static All<T1, T2> All<T1, T2>(in T1 action1, in T2 action2) where T1 : IAction where T2 : IAction =>
            new All<T1, T2>(action1, action2);

        public static Component.Set<T> Set<T>(in T component) where T : struct, IComponent => new Component.Set<T>(component);
        public static Component.Remove<T> Remove<T>(in T component) where T : struct, IComponent => new Component.Remove<T>();
    }

    public readonly struct All<T1, T2> : IAction where T1 : IAction where T2 : IAction
    {
        public readonly T1 Action1;
        public readonly T2 Action2;

        public All(in T1 action1, in T2 action2)
        {
            Action1 = action1;
            Action2 = action2;
        }

        void IAction.Do(Entity entity, World world)
        {
            Action1.Do(entity, world);
            Action2.Do(entity, world);
            Action.All(Action.Set(new Experiment.Position()), Action.Set(new Experiment.Velocity()));
        }
    }

    namespace Component
    {
        public readonly struct Set<T> : IAction where T : struct, IComponent
        {
            public readonly T Component;
            public Set(in T component) { Component = component; }
            void IAction.Do(Entity entity, World world) => world.Components().Set(entity, Component);
        }

        public readonly struct Remove<T> : IAction where T : struct, IComponent
        {
            void IAction.Do(Entity entity, World world) => world.Components().Remove<T>(entity);
        }
    }

}