using Entia.Core;
using Entia.Modules.Control;
using Entia.Modules.Schedule;
using Entia.Schedulers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed class Schedulers : IModule, IEnumerable<IScheduler>
    {
        sealed class Comparer : IEqualityComparer<(ISchedulable, Controller)>
        {
            public bool Equals((ISchedulable, Controller) x, (ISchedulable, Controller) y) =>
                ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2);

            public int GetHashCode((ISchedulable, Controller) obj) => obj.GetHashCode();
        }

        readonly World _world;
        readonly TypeMap<ISchedulable, IScheduler> _schedulers = new TypeMap<ISchedulable, IScheduler>();
        readonly TypeMap<ISchedulable, IScheduler> _defaults = new TypeMap<ISchedulable, IScheduler>();
        readonly Dictionary<(ISchedulable, Controller), Phase[]> _phases = new Dictionary<(ISchedulable, Controller), Phase[]>(new Comparer());

        public Schedulers(World world) { _world = world; }

        public Phase[] Schedule(ISchedulable instance, Controller controller)
        {
            var key = (instance, controller);
            if (_phases.TryGetValue(key, out var phases)) return phases;

            var types = instance.GetType().Hierarchy().Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISchedulable<>)).ToArray();
            var schedulers = types.Select(Get).ToArray();
            phases = schedulers.SelectMany(scheduler => scheduler.Schedule(instance, controller, _world)).ToArray();
            return _phases[key] = phases;
        }

        public IScheduler Default<T>() where T : ISchedulable => _defaults.TryGet<T>(out var scheduler) ? scheduler : Default(typeof(T));
        public IScheduler Default(Type schedulable) => _defaults.Default(schedulable, typeof(ISchedulable<>), null, () => new Default());
        public IScheduler Get<T>() where T : ISchedulable => _schedulers.TryGet<T>(out var scheduler, true) ? scheduler : Default<T>();
        public IScheduler Get(Type schedulable) => _schedulers.TryGet(schedulable, out var scheduler, true) ? scheduler : Default(schedulable);
        public bool Set<T>(Scheduler<T> scheduler) where T : ISchedulable => _schedulers.Set<T>(scheduler);
        public bool Set(Type schedulable, IScheduler scheduler) => _schedulers.Set(schedulable, scheduler);
        public bool Has<T>() where T : ISchedulable => _schedulers.Has<T>(true);
        public bool Has(Type schedulable) => _schedulers.Has(schedulable, true);
        public bool Remove<T>() where T : ISchedulable => _schedulers.Remove<T>();
        public bool Remove(Type schedulable) => _schedulers.Remove(schedulable);
        public bool Clear() => _schedulers.Clear() | _defaults.Clear();
        public IEnumerator<IScheduler> GetEnumerator() => _schedulers.Values.Concat(_defaults.Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
