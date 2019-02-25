using System;
using System.Linq;
using Entia.Messages;
using Entia.Modules;
using FsCheck;
using Entia.Core;
using Entia.Modules.Message;

namespace Entia.Test
{
    public sealed class TrimComponents : Action<World, Model>
    {
        Entity _source;
        Entity _target;
        IComponent[] _sources;
        IComponent[] _targets;
        IComponent[] _trimmed;
        Type[] _exceeding;
        OnRemove[] _onRemove;

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            _source = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            _target = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            return true;
        }
        public override void Do(World value, Model model)
        {
            _sources = value.Components().Get(_source).ToArray();
            _targets = value.Components().Get(_target).ToArray();
            _exceeding = _targets.Select(component => component.GetType()).Except(_sources.Select(component => component.GetType())).ToArray();

            var receiver = value.Messages().Receiver<OnRemove>();
            {
                value.Components().Trim(_source, _target);
                _trimmed = value.Components().Get(_target).ToArray();
                foreach (var type in _exceeding) model.Components[_target].Remove(type);
            }
            _onRemove = receiver.Pop().ToArray();
            value.Messages().Remove(receiver);
        }
        public override Property Check(World value, Model model) =>
            (_sources.Length >= _trimmed.Length).Label("sources.Length == trimmed.Length")
            .And((_targets.Length >= _trimmed.Length).Label("targets.Length <= trimmed.Length"))
            .And((_trimmed.Length == _targets.Length - _exceeding.Length).Label("trimmed.Length == targets.Length - exceeding.Length"))
            .And((_exceeding.Length == _onRemove.Length).Label("missing.Length == onRemove.Length"))
            .And((_onRemove.All(message => _exceeding.Contains(message.Component.Type))).Label("onRemove.All(exceeding.Contains())"))
            .And((value.Components().Count(_source) >= value.Components().Count(_target)).Label("Count(source) >= Count(target)"))
            .And(_trimmed.All(trimmed => value.Components().Has(_source, trimmed.GetType())).Label("trimmed.All(Has(source, trimmed.GetType()))"))
            .And(_exceeding.All(exceeding => value.Components().Has(_source, exceeding).Not()).Label("exceeding.All(Has(source).Not())"))
            .And(_exceeding.All(exceeding => value.Components().Has(_target, exceeding).Not()).Label("exceeding.All(Has(target).Not())"));
        public override string ToString() => $"{GetType().Format()}({_source}, {_target})";
    }
}