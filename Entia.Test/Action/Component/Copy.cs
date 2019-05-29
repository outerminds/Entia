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
    public sealed class CopyComponent<T> : Action<World, Model> where T : IComponent
    {
        States _include;
        Entity _source;
        Entity _target;
        IComponent[] _sources;
        IComponent[] _targets;
        IComponent[] _copies;
        Type[] _missing;
        OnAdd[] _onAdd;
        bool _success;

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            var components = value.Components();
            if (entities.Count <= 0) return false;

            _include = model.Random.NextState();
            _source = model.Random.NextEntity(entities);
            _sources = components.Get(_source, _include).Where(Is).ToArray();
            _target = model.Random.NextEntity(entities);
            _targets = components.Get(_target).Where(Is).ToArray();
            _missing = _sources.Select(component => component.GetType()).Except(_targets.Select(component => component.GetType())).ToArray();
            return true;
        }
        public override void Do(World value, Model model)
        {
            var components = value.Components();
            var messages = value.Messages();
            using (var onAdd = messages.Receive<OnAdd>())
            {
                _success = components.Copy<T>(_source, _target, _include);
                _copies = components.Get(_target).Where(Is).ToArray();
                foreach (var component in _copies) model.Components[_target].Set(component.GetType());
                _onAdd = onAdd.Pop().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (_onAdd.All(message => message.Entity == _target && _missing.Contains(message.Component.Type)), "onAdd.All(missing.Contains())");

                if (_success)
                {
                    yield return (_sources.Length <= _copies.Length, "sources.Length <= copies.Length");
                    yield return (_targets.Length <= _copies.Length, "targets.Length <= copies.Length");
                    yield return (_copies.Length == _targets.Length + _missing.Length, "copies.Length == targets.Length + missing.Length");

                    yield return (_missing.Length == _onAdd.Length, "missing.Length == onAdd.Length");
                    yield return (_missing.All(type => components.Has(_target, type)), "missing.All(Has)");
                    yield return (_missing.All(type => components.TryGet(_target, type, out _)), "missing.All(TryGet)");
                    yield return (_missing.All(type => components.State(_target, type) == States.Enabled), "missing.All(Enabled)");
                    yield return (_missing.None(type => components.Enable(_target, type)), "missing.None(Enable)");

                    yield return (_sources.All(source => components.Has(_target, source.GetType())), "sources.All(Has(target, source.GetType()))");
                }
            }
        }
        public override string ToString() => $"{GetType().Format()}({_source}, {_target}, {_success})";

        bool Is(IComponent component) => component is T;
    }

    public sealed class CopyComponent : Action<World, Model>
    {
        readonly Type _type;
        States _include;
        Entity _source;
        Entity _target;
        IComponent[] _sources;
        IComponent[] _targets;
        IComponent[] _copies;
        Type[] _missing;
        OnAdd[] _onAdd;
        bool _success;

        public CopyComponent(Type type) { _type = type; }

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            if (entities.Count <= 0) return false;

            var components = value.Components();
            _include = model.Random.NextState();
            _source = model.Random.NextEntity(entities);
            _sources = components.Get(_source, _include).Where(Is).ToArray();
            _target = model.Random.NextEntity(entities);
            _targets = components.Get(_target).Where(Is).ToArray();
            _missing = _sources.Select(component => component.GetType()).Except(_targets.Select(component => component.GetType())).ToArray();
            return true;

        }
        public override void Do(World value, Model model)
        {
            var components = value.Components();
            var messages = value.Messages();
            using (var onAdd = messages.Receive<OnAdd>())
            {
                _success = components.Copy(_source, _target, _type, _include);
                _copies = components.Get(_target).Where(Is).ToArray();
                foreach (var component in _copies) model.Components[_target].Set(component.GetType());
                _onAdd = onAdd.Pop().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (_onAdd.All(message => message.Entity == _target && _missing.Contains(message.Component.Type)), "onAdd.All(missing.Contains())");

                if (_success)
                {
                    yield return (_sources.Length <= _copies.Length, "sources.Length <= copies.Length");
                    yield return (_targets.Length <= _copies.Length, "targets.Length <= copies.Length");
                    yield return (_copies.Length == _targets.Length + _missing.Length, "copies.Length == targets.Length + missing.Length");

                    yield return (_missing.Length == _onAdd.Length, "missing.Length == onAdd.Length");
                    yield return (_missing.All(type => components.Has(_target, type)), "missing.All(Has)");
                    yield return (_missing.All(type => components.TryGet(_target, type, out _)), "missing.All(TryGet)");
                    yield return (_missing.All(type => components.State(_target, type) == States.Enabled), "missing.All(Enabled)");
                    yield return (_missing.None(type => components.Enable(_target, type)), "missing.None(Enable)");

                    yield return (_sources.All(source => components.Has(_target, source.GetType())), "sources.All(Has(target, source.GetType()))");
                }
            }
        }
        public override string ToString() => $"{GetType().Format()}({_source}, {_target}, {_include}, {_success})";

        bool Is(IComponent component) => TypeUtility.Is(component, _type, true, true);
    }

    public sealed class CopyComponents : Action<World, Model>
    {
        States _include;
        Entity _source;
        Entity _target;
        IComponent[] _sources;
        IComponent[] _targets;
        IComponent[] _copies;
        Type[] _missing;
        OnAdd[] _onAdd;
        bool _success;

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            if (entities.Count <= 0) return false;

            var components = value.Components();
            _include = model.Random.NextState();
            _source = model.Random.NextEntity(entities);
            _sources = components.Get(_source, _include).ToArray();
            _target = model.Random.NextEntity(entities);
            _targets = components.Get(_target).ToArray();
            _missing = _sources.Select(component => component.GetType()).Except(_targets.Select(component => component.GetType())).ToArray();
            return true;
        }
        public override void Do(World value, Model model)
        {
            var components = value.Components();
            var messages = value.Messages();
            using (var onAdd = messages.Receive<OnAdd>())
            {
                _success = components.Copy(_source, _target, _include);
                _copies = components.Get(_target).ToArray();
                foreach (var component in _copies) model.Components[_target].Set(component.GetType());
                _onAdd = onAdd.Pop().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (_success, "success");
                yield return (_sources.Length <= _copies.Length, "sources.Length <= copies.Length");
                yield return (_targets.Length <= _copies.Length, "targets.Length <= copies.Length");
                yield return (_copies.Length == _targets.Length + _missing.Length, "copies.Length == targets.Length + missing.Length");

                yield return (_missing.Length == _onAdd.Length, "missing.Length == onAdd.Length");
                yield return (_missing.All(type => components.Has(_target, type)), "missing.All(Has)");
                yield return (_missing.All(type => components.TryGet(_target, type, out _)), "missing.All(TryGet)");
                yield return (_missing.All(type => components.State(_target, type) == States.Enabled), "missing.All(Enabled)");
                yield return (_missing.None(type => components.Enable(_target, type)), "missing.None(Enable)");

                yield return (_onAdd.All(message => message.Entity == _target && _missing.Contains(message.Component.Type)), "onAdd.All(missing.Contains())");
                yield return (_sources.All(source => components.Has(_target, source.GetType())), "sources.All(Has(target, source.GetType()))");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_source}, {_target}, {_include}, {_success})";
    }
}