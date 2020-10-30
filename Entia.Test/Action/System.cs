using Entia.Experimental.Scheduling;
using Entia.Modules;
using Entia.Core;
using static Entia.Experimental.Node;
using static Entia.Experimental.Filter;
using FsCheck;
using System.Collections.Generic;
using Entia.Injectables;
using System.Linq;
using System.Threading;

namespace Entia.Test
{
    public sealed class RunSystem<T1, T2> : Action<World, Model> where T1 : struct, IMessage where T2 : struct, IMessage
    {
        int _iterations;
        int _parallel;
        int _counter1;
        int _counter2;
        Result<Unit> _result;

        public override bool Pre(World value, Model model)
        {
            _iterations = model.Random.Next(0, 10);
            _parallel = model.Random.Next(0, 10);
            return true;
        }

        public override void Do(World value, Model model)
        {
            var run1 = System<T1>.Run(() => _counter1++);
            var nodes1 = new[]
            {
                System<T1>.Run(() => _counter1++),
                System<T1>.Run((in T1 _) => _counter1++),
                Sequence(
                    System<T1>.Run((ref Tests.ResourceA _) => _counter1++),
                    System<T1>.Run((in T1 _, ref Tests.ResourceA __) => _counter1++),
                    run1
                ),
                Inject((Emitter<T1> _, Reaction<T1> __, World ___, AllComponents.Read ____) => run1),
                Lazy(() => run1),
            };
            nodes1.Shuffle(model.Random);

            var run2 = System<T2>.Run(() => Interlocked.Increment(ref _counter2));
            var nodes2 = new[]
            {
                System<T2>.Run(() => Interlocked.Increment(ref _counter2)),
                Sequence(
                    System<T2>.Run((in T2 _) => Interlocked.Increment(ref _counter2)),
                    System<T2>.Run((ref Tests.ResourceA _) => Interlocked.Increment(ref _counter2)),
                    run2
                ),
                System<T2>.Run((in T2 _, ref Tests.ResourceA __, ref Tests.ResourceB ___) => Interlocked.Increment(ref _counter2)),
                Inject((Tests.Injectable _) => run2),
                Lazy(() => run2),
                Inject((Resource<Tests.ResourceA>.Read _, AllEntities __) => Parallel(Enumerable.Repeat(run2, _parallel).ToArray()))
            };
            nodes2.Shuffle(model.Random);

            var node = Sequence(Sequence(nodes1), Parallel(nodes2));
            var messages = value.Messages();
            _result = value.Schedule(node).Use(_ =>
            {
                for (int i = 0; i < _iterations; i++)
                {
                    messages.Emit<T1>();
                    messages.Emit<T2>();
                    messages.Emit(typeof(T2));
                    messages.Emit(typeof(T1));
                }
            });
        }

        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool tests, string label)> Tests()
            {
                yield return (_result.IsSuccess(), "Result.IsSuccess()");
                yield return (_counter1 == 7 * 2 * _iterations, "counter1");
                yield return (_counter2 == (7 + _parallel) * 2 * _iterations, "counter2");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_counter1}, {_counter2}, {_iterations}, {_parallel}, {_result})";
    }

    public sealed class RunEachSystem<TMessage1, TMessage2, TComponent1, TComponent2> : Action<World, Model>
        where TMessage1 : struct, IMessage where TMessage2 : struct, IMessage
        where TComponent1 : struct, IComponent where TComponent2 : struct, IComponent
    {
        int _counter1;
        int _counter2;
        int _counter3;
        int _counter4;
        int _counter5;
        int _counter6;
        Entity[] _entities1;
        Entity[] _entities2;
        Entity[] _entities3;
        Entity[] _entities4;
        Entity[] _entities5;
        Entity[] _entities6;
        Result<Unit> _result;

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            var components = value.Components();
            _entities1 = entities.Where(entity => components.Has<TComponent1>(entity)).ToArray();
            _entities2 = entities.Where(entity => components.Has<TComponent2>(entity)).ToArray();
            _entities3 = entities.Where(entity => components.Has<TComponent1>(entity) && components.Has<TComponent2>(entity)).ToArray();
            _entities4 = entities.Where(entity => components.Has<TComponent1>(entity) && !components.Has<TComponent2>(entity)).ToArray();
            _entities5 = entities.Where(entity => components.Has<TComponent1>(entity) || components.Has<TComponent2>(entity)).ToArray();
            _entities6 = entities.Where(entity => !components.Has<TComponent1>(entity) && !components.Has<TComponent2>(entity)).ToArray();
            return true;
        }

        public override void Do(World value, Model model)
        {
            var nodes = new[]
            {
                System<TMessage1>.RunEach((ref TComponent1 _) => _counter1++),
                System<TMessage1>.RunEach((in TMessage1 _, ref TComponent2 __) => _counter2++),
                System<TMessage1>.RunEach((ref TComponent1 _, ref TComponent2 __) => _counter3++),
                System<TMessage2>.RunEach((ref TComponent1 _) => _counter4++, None(Has<TComponent2>())),
                System<TMessage2>.RunEach((in TMessage2 _) => _counter5++, Any(Has<TComponent1>(), Has<TComponent2>())),
                System<TMessage2>.RunEach(() => _counter6++, None(Has<TComponent1>(), Has<TComponent2>())),
            };
            nodes.Shuffle(model.Random);

            var node = Sequence(nodes);
            var messages = value.Messages();
            _result = value.Schedule(node).Use(_ =>
            {
                messages.Emit(typeof(TMessage1));
                messages.Emit(typeof(TMessage2));
                messages.Emit<TMessage2>();
                messages.Emit<TMessage1>();
            });
        }

        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool tests, string label)> Tests()
            {
                yield return (_result.IsSuccess(), "Result.IsSuccess()");
                yield return (_counter1 == _entities1.Length * 2, "counter1");
                yield return (_counter2 == _entities2.Length * 2, "counter2");
                yield return (_counter3 == _entities3.Length * 2, "counter3");
                yield return (_counter4 == _entities4.Length * 2, "counter4");
                yield return (_counter5 == _entities5.Length * 2, "counter5");
                yield return (_counter6 == _entities6.Length * 2, "counter6");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_counter1}, {_counter2}, {_counter3}, {_counter4}, {_counter5}, {_counter6}, {_result})";
    }
}