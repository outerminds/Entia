using System;
using System.Linq;
using Entia.Core;
using Entia.Check;
using static Entia.Check.Generator;

namespace Entia.Experiment.Check
{
    public static class GeneratorTests
    {
        public static void CharacterTests()
        {
            var failures1 = Letter.Check("Letter", char.IsLetter);
            var failures2 = Digit.Check("Digit", char.IsDigit);
            var failures3 = ASCII.Check("ASCII", value => value < 128);
        }

        public static void StringTests()
        {
            var failures1 = Letter.String(Range(100)).Check("Letter", value => value.All(char.IsLetter));
            var failures2 = Digit.String(Range(100)).Check("Digit", value => value.All(char.IsDigit));
            var failures3 = ASCII.String(Range(100)).Check("ASCII", value => value.All(value => value < 128));
        }

        static Failure<T>[] Check<T>(this Generator<T> generator, string name, Func<T, bool> prove) =>
            generator.Prove(name, prove).Log(name).Check();
    }
}