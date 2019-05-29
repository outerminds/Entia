using System;
using System.Linq;
using Entia.Messages;
using Entia.Modules;
using FsCheck;
using Entia.Core;
using Entia.Modules.Message;
using System.Collections.Generic;

namespace Entia.Test
{
    public sealed class TrimComponents : Action<World, Model>
    {
        States _include;
        Entity _source;
        Entity _target;
        IComponent[] _sources;
        IComponent[] _targets;
        IComponent[] _trimmed;
        Type[] _exceeding;
        bool _success;
        OnRemove[] _onRemove;

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            var components = value.Components();
            if (entities.Count <= 0) return false;

            _include = model.Random.NextState();
            _source = model.Random.NextEntity(entities);
            _sources = components.Get(_source).ToArray();
            _target = model.Random.NextEntity(entities);
            _targets = components.Get(_target, _include).ToArray();
            _exceeding = _targets.Select(component => component.GetType()).Except(_sources.Select(component => component.GetType())).ToArray();
            return true;
        }
        public override void Do(World value, Model model)
        {
            var components = value.Components();
            var messages = value.Messages();
            using (var onRemove = messages.Receive<OnRemove>())
            {
                _success = components.Trim(_source, _target, _include);
                _trimmed = components.Get(_target, _include).ToArray();
                foreach (var type in _exceeding) model.Components[_target].Remove(type, _include);
                _onRemove = onRemove.Pop().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (_sources.Length >= _trimmed.Length, "sources.Length == trimmed.Length");
                yield return (_targets.Length >= _trimmed.Length, "targets.Length >= trimmed.Length");
                yield return (_trimmed.Length == _targets.Length - _exceeding.Length, "trimmed.Length == targets.Length - exceeding.Length");
                yield return (_exceeding.Length == _onRemove.Length, "missing.Length == onRemove.Length");
                yield return (_onRemove.All(message => message.Entity == _target && _exceeding.Contains(message.Component.Type)), "onRemove.All(exceeding.Contains())");
                yield return (components.Count(_source) >= components.Count(_target, _include), "Count(source) >= Count(target)");
                yield return (_trimmed.All(trimmed => components.Has(_source, trimmed.GetType())), "trimmed.All(Has(source, trimmed.GetType()))");
                yield return (_exceeding.All(exceeding => components.Has(_source, exceeding).Not()), "exceeding.All(Has(source).Not())");
                yield return (_exceeding.All(exceeding => components.Has(_target, exceeding, _include).Not()), "exceeding.All(Has(target).Not())");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_source}, {_target}, {_include}, {_success})";
    }
}