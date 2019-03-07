using System;
using System.Linq;
using Entia.Messages;
using Entia.Modules;
using FsCheck;
using Entia.Core;
using Entia.Modules.Message;

namespace Entia.Test
{
    public sealed class CloneComponent<T> : Action<World, Model> where T : IComponent
    {
        readonly Depth _depth;
        Entity _source;
        Entity _target;
        IComponent[] _sources;
        IComponent[] _targets;
        IComponent[] _clones;
        Type[] _missing;
        OnAdd[] _onAdd;
        bool _success;

        public CloneComponent(Depth depth = Depth.Shallow) { _depth = depth; }

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            _source = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            if (value.Components().Has<T>(_source))
            {
                _target = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
                return true;
            }
            return false;
        }
        public override void Do(World value, Model model)
        {
            _sources = value.Components().Get(_source).Where(component => component.Is<T>()).ToArray();
            _targets = value.Components().Get(_target).Where(component => component.Is<T>()).ToArray();
            _missing = _sources.Select(component => component.GetType()).Except(_targets.Select(component => component.GetType())).ToArray();

            var receiver = value.Messages().Receiver<OnAdd>();
            {
                _success = value.Components().Clone<T>(_source, _target, _depth);
                _clones = value.Components().Get(_target).Where(component => component.Is<T>()).ToArray();
                foreach (var component in _clones) model.Components[_target][component.GetType()] = component;
            }
            _onAdd = receiver.Pop().ToArray();
            value.Messages().Remove(receiver);
        }
        public override Property Check(World value, Model model) =>
            _success.Label("success")
            .And((_sources.Length <= _clones.Length).Label("sources.Length <= clones.Length"))
            .And((_targets.Length <= _clones.Length).Label("targets.Length <= clones.Length"))
            .And((_clones.Length == _targets.Length + _missing.Length).Label("clones.Length == targets.Length + missing.Length"))
            .And((_missing.Length == _onAdd.Length).Label("missing.Length == onAdd.Length"))
            .And((_onAdd.All(message => _missing.Contains(message.Component.Type))).Label("onAdd.All(missing.Contains())"))
            .And(_sources.All(source => value.Components().Has(_target, source.GetType())).Label("sources.All(Has(target, source.GetType()))"));
        public override string ToString() => $"{GetType().Format()}({_source}, {_target}, {_depth})";
    }

    public sealed class CloneComponent : Action<World, Model>
    {
        readonly Type _type;
        readonly Depth _depth;
        Entity _source;
        Entity _target;
        IComponent[] _sources;
        IComponent[] _targets;
        IComponent[] _clones;
        Type[] _missing;
        OnAdd[] _onAdd;
        bool _success;

        public CloneComponent(Type type, Depth depth = Depth.Shallow)
        {
            _type = type;
            _depth = depth;
        }

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            _source = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            if (value.Components().Has(_source, _type))
            {
                _target = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
                return true;
            }
            return false;
        }
        public override void Do(World value, Model model)
        {
            bool Is(IComponent component) => TypeUtility.Is(component, _type, true, true);

            _sources = value.Components().Get(_source).Where(Is).ToArray();
            _targets = value.Components().Get(_target).Where(Is).ToArray();
            _missing = _sources.Select(component => component.GetType()).Except(_targets.Select(component => component.GetType())).ToArray();

            var receiver = value.Messages().Receiver<OnAdd>();
            {
                _success = value.Components().Clone(_source, _target, _type, _depth);
                _clones = value.Components().Get(_target).Where(Is).ToArray();
                foreach (var component in _clones) model.Components[_target][component.GetType()] = component;
            }
            _onAdd = receiver.Pop().ToArray();
            value.Messages().Remove(receiver);
        }
        public override Property Check(World value, Model model) =>
            _success.Label("success")
            .And((_sources.Length <= _clones.Length).Label("sources.Length <= clones.Length"))
            .And((_targets.Length <= _clones.Length).Label("targets.Length <= clones.Length"))
            .And((_clones.Length == _targets.Length + _missing.Length).Label("clones.Length == targets.Length + missing.Length"))
            .And((_missing.Length == _onAdd.Length).Label("missing.Length == onAdd.Length"))
            .And((_onAdd.All(message => _missing.Contains(message.Component.Type))).Label("onAdd.All(missing.Contains())"))
            .And(_sources.All(source => value.Components().Has(_target, source.GetType())).Label("sources.All(Has(target, source.GetType()))"));
        public override string ToString() => $"{GetType().Format()}({_source}, {_target}, {_depth})";
    }

    public sealed class CloneComponents : Action<World, Model>
    {
        readonly Depth _depth;
        Entity _source;
        Entity _target;
        IComponent[] _sources;
        IComponent[] _targets;
        IComponent[] _clones;
        Type[] _missing;
        OnAdd[] _onAdd;
        bool _success;

        public CloneComponents(Depth depth = Depth.Shallow) { _depth = depth; }

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
            _missing = _sources.Select(component => component.GetType()).Except(_targets.Select(component => component.GetType())).ToArray();

            var receiver = value.Messages().Receiver<OnAdd>();
            {
                _success = value.Components().Clone(_source, _target, _depth);
                _clones = value.Components().Get(_target).ToArray();
                foreach (var component in _clones) model.Components[_target][component.GetType()] = component;
            }
            _onAdd = receiver.Pop().ToArray();
            value.Messages().Remove(receiver);
        }
        public override Property Check(World value, Model model) =>
            _success.Label("success")
            .And((_sources.Length <= _clones.Length).Label("sources.Length <= clones.Length"))
            .And((_targets.Length <= _clones.Length).Label("targets.Length <= clones.Length"))
            .And((_clones.Length == _targets.Length + _missing.Length).Label("clones.Length == targets.Length + missing.Length"))
            .And((_missing.Length == _onAdd.Length).Label("missing.Length == onAdd.Length"))
            .And((_onAdd.All(message => _missing.Contains(message.Component.Type))).Label("onAdd.All(missing.Contains())"))
            .And(_sources.All(source => value.Components().Has(_target, source.GetType())).Label("sources.All(Has(target, source.GetType()))"));
        public override string ToString() => $"{GetType().Format()}({_source}, {_target}, {_depth})";
    }
}