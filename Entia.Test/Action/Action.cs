using Entia.Core;
using FsCheck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Entia.Test
{
    public abstract class Action<T, TModel>
    {
        public virtual bool Pre(T value, TModel model) => true;
        public virtual void Post(T value, TModel model) { }
        public abstract void Do(T value, TModel model);
        public virtual Property Check(T value, TModel model) => true.ToProperty();
    }

    public static class Action
    {
        public sealed class Sequence<T, TModel> : ISized, IFormatted
        {
            int ISized.Size => Actions.Length;

            public readonly Func<int, (T, TModel)> Initial;
            public readonly Func<T, TModel, Property> Check;
            public readonly Action<T, TModel>[] Actions;
            public readonly List<Action<T, TModel>> Did = new List<Action<T, TModel>>();

            public Sequence(Func<int, (T, TModel)> initial, Func<T, TModel, Property> check, params Action<T, TModel>[] actions)
            {
                Initial = initial;
                Check = check;
                Actions = actions;
            }

            public override string ToString() => Format(Entia.Test.Format.Types.Detailed);
            public string Format(Format.Types format)
            {
                var header = $"{GetType().Format()} {{ Total Count: {Actions.Length}, Reduced Count: {Did.Count} }}";
                switch (format)
                {
                    case Entia.Test.Format.Types.Detailed:
                        var groups = string.Join(Environment.NewLine, Did
                            .GroupBy(action => action.GetType().Format())
                            .Select(group => (type: group.Key, count: group.Count()))
                            .OrderByDescending(pair => pair.count)
                            .Select(pair => $"-> {pair.type}: {pair.count}"));
                        var actions = string.Join(Environment.NewLine, Did.Select(action => $"-> {action}"));
                        return
$@"
{header}
Types: 
{groups}

Actions:
{actions}
";
                    default:
                        return
$@"
{header}
";
                }
            }
        }

        public static Sequence<T, TModel> Clone<T, TModel>(this Sequence<T, TModel> sequence) =>
            new Sequence<T, TModel>(sequence.Initial, sequence.Check, Clone(sequence.Actions));

        public static T[] Clone<T>(T[] array) => array.Select(CloneUtility.Shallow).ToArray();

        public static Property ToProperty<T, TModel>(this Arbitrary<Sequence<T, TModel>> arbitrary) =>
            Prop.ForAll(arbitrary, Arb.Default.DoNotSizeInt32().WithoutShrink(), (sequence, seed) => sequence.ToProperty(seed.Item));

        public static Property ToProperty<T, TModel>(this Sequence<T, TModel> sequence, int seed)
        {
            var (value, model) = sequence.Initial(seed);
            var property = Prop.OfTestable(true);

            foreach (var action in Clone(sequence.Actions))
            {
                if (action.Pre(value, model))
                {
                    sequence.Did.Add(action);
                    action.Do(value, model);
                    property = property.And(action.Check(value, model).Label($"-> {action}"));
                    action.Post(value, model);
                }
            }

            return property.And(sequence.Check(value, model));
        }

        public static Arbitrary<Sequence<T, TModel>> ToArbitrary<T, TModel>(this Gen<Sequence<T, TModel>> generator, bool shrink = true) => shrink ?
            Arb.From(generator, sequence =>
                Arb.Shrink(sequence.Actions)
                    .Select(actions => new Sequence<T, TModel>(sequence.Initial, sequence.Check, actions))) :
            Arb.From(generator);

        public static Gen<Sequence<T, TModel>> ToSequence<T, TModel>(this Gen<Action<T, TModel>> generator, Func<int, (T, TModel)> initial, Func<T, TModel, Property> check = null)
        {
            check = check ?? ((_, __) => true.ToProperty());
            return Gen.ArrayOf(generator).Select(actions => new Sequence<T, TModel>(initial, check, actions));
        }

        public static Action<T, TModel> ToAction<T, TModel>(this Action<T, TModel> action) => action;
    }
}
