using Entia.Experimental.Scheduling;
using Entia.Modules;
using Entia.Core;
using static Entia.Experimental.Node;
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
        int _counter3;
        Result<Unit> _result;

        public override bool Pre(World value, Model model)
        {
            _iterations = model.Random.Next(0, 10);
            _parallel = model.Random.Next(0, 10);
            return true;
        }

        public override void Do(World value, Model model)
        {
            var run1 = When<T1>.Run(() => _counter1++);
            var nodes1 = new[]
            {
                When<T1>.Run(() => _counter1++),
                When<T1>.Run((in T1 _) => _counter1++),
                Sequence(
                    When<T1>.Run((ref Test.ResourceA _) => _counter1++),
                    When<T1>.Run((in T1 _, ref Test.ResourceA __) => _counter1++),
                    run1
                ),
                With((Emitter<T1> _, Reaction<T1> __, World ___, AllComponents.Read ____) => run1),
                With(() => run1),
            };
            nodes1.Shuffle(model.Random);

            var run2 = When<T2>.Run(() => Interlocked.Increment(ref _counter2));
            var nodes2 = new[]
            {
                When<T2>.Run(() => Interlocked.Increment(ref _counter2)),
                Sequence(
                    When<T2>.Run((in T2 _) => Interlocked.Increment(ref _counter2)),
                    When<T2>.Run((ref Test.ResourceA _) => Interlocked.Increment(ref _counter2)),
                    run2
                ),
                When<T2>.Run((in T2 _, ref Test.ResourceA __, ref Test.ResourceB ___) => Interlocked.Increment(ref _counter2)),
                With((Test.Injectable _) => run2),
                Lazy(_ => run2),
                With((Resource<Test.ResourceA>.Read _, AllEntities __) => Parallel(Enumerable.Repeat(run2, _parallel).ToArray()))
            };
            nodes2.Shuffle(model.Random);

            var nodes3 = new[]
            {
                When<T1, T1>.Run(() => _counter3++),
                When<T2, T2>.Run((in T2 _, in T2 __) => _counter3++),
                When<T1, T2>.Run(() => _counter3++),
                When<T1, T2>.Run((in T2 _) => _counter3++),
                When<T1, T2>.Run((in T1 _, in T2 __) => _counter3++),
                When<T1, T2>.Run((ref Test.ResourceB _) => _counter3++),
                When<T1, T2>.Run((in T1 _, in T2 __, ref Test.ResourceC<string> ___) => _counter3++)
            };
            nodes3.Shuffle(model.Random);

            var node = Sequence(Sequence(nodes1), Parallel(nodes2), Sequence(nodes3));
            var messages = value.Messages();
            _result = value.Schedule(node).Use(() =>
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
                yield return (_counter3 == 7 * 2 * _iterations, "counter3");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_counter1}, {_counter2}, {_counter3}, {_iterations}, {_parallel}, {_result})";
    }
}