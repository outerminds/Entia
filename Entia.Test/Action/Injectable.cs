using System;
using System.Linq;
using System.Reflection;
using Entia.Core;
using Entia.Injection;
using Entia.Injectables;
using FsCheck;

namespace Entia.Test
{
    public sealed class Inject : Action<World, Model>
    {
        readonly Type[] _types;
        Result<object[]> _result;

        public Inject(params Type[] types) { _types = types; }

        public override void Do(World value, Model model) => _result = _types.Select(value.Inject).All();

        public override Property Check(World value, Model model) =>
            _result.IsSuccess().Label("result.IsSuccess()")
            .And((_result.TryValue(out var current) && current.Some().SequenceEqual(current)).Label("result.TryValue()"));
    }

    public sealed class Inject<T> : Action<World, Model> where T : IInjectable
    {
        readonly MemberInfo _member;
        Result<T> _resultT;
        Result<object> _result;

        public Inject(MemberInfo member = null) { _member = member; }

        public override void Do(World value, Model model)
        {
            _resultT = value.Inject<T>(_member);
            _result = value.Inject(typeof(T), _member);
        }

        public override Property Check(World value, Model model) =>
            _resultT.IsSuccess().Label("resultT.IsSuccess()")
            .And((_resultT.TryValue(out var currentT) && currentT is T).Label("resultT.TryValue()"))
            .And(_result.IsSuccess().Label("result.IsSuccess()"))
            .And((_result.TryValue(out var current) && current is T).Label("result.TryValue()"));
    }
}