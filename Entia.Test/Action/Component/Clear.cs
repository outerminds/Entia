using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Message;
using Entia.Modules.Component;
using FsCheck;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Entia.Test
{
    public class ClearComponent : Action<World, Model>
    {
        States _include;
        Type _type;
        Entity[] _entities;
        int _count;
        bool _success;
        OnRemove[] _onRemove;

        public ClearComponent(Type type) { _type = type; }

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            var components = value.Components();
            _include = model.Random.NextState();
            _entities = entities.Where(entity => components.Has(entity, _type, _include)).ToArray();
            _count = components.Count(_type, _include);
            return true;
        }
        public override void Do(World value, Model model)
        {
            var components = value.Components();
            var messages = value.Messages();
            var onRemove = messages.Receiver<OnRemove>();
            {
                _success = components.Clear(_type, _include);
                foreach (var entity in _entities) model.Components[entity].Remove(_type, _include);
            }
            _onRemove = onRemove.Pop().ToArray();
            messages.Remove(onRemove);
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var entities = value.Entities();
                var components = value.Components();

                yield return (components.Get(_type, _include).None(), "Components.Get().None()");
                yield return (components.Count(_type, _include) == 0, "Components.Count() == 0");
                yield return (entities.None(entity => components.Has(entity, _type, _include)), "Entities.None(Components.Has())");
                yield return (entities.None(entity => components.TryGet(entity, _type, out _, _include)), "Entities.None(Components.TryGet())");
                yield return (entities.All(entity => components.Get(entity, _include).OfType(_type, true, true).None()), "Entities.All(Components.Get().OfType().None())");
                yield return (entities.None(entity => _include.HasAny(components.State(entity, _type))), "Entities.None(include.HasAny(Components.State()))");

                yield return (_onRemove.Length >= _entities.Length, "onRemove.Length >= entities.Length");
                yield return (_onRemove.Length == _count, "onRemove.Length == count");
                yield return (_onRemove.All(message => message.Component.Type.Is(_type, true, true)), "onRemove.All(Is(type))");
                yield return (_entities.OrderBy(_ => _).SequenceEqual(_onRemove.Select(message => message.Entity).Distinct().OrderBy(_ => _)), "OnRemove.Entity");

                if (_success)
                {
                    yield return (_onRemove.Length > 0, "onRemove.Length > 0");
                    yield return (_count > 0, "count > 0");
                }

                if (_include.HasAny(States.Disabled))
                    yield return (_entities.None(entity => components.Enable(entity, _type)), "Entities.None(Components.Enable())");

                if (_include.HasAny(States.Enabled))
                    yield return (_entities.None(entity => components.Disable(entity, _type)), "Entities.None(Components.Disable())");

                yield return (components.Clear(_type, _include).Not(), "Components.Clear().Not()");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_type.Format()}, {_include}, {_count}, {_success})";
    }

    public class ClearComponent<T> : Action<World, Model> where T : struct, IComponent
    {
        States _include;
        Entity[] _entities;
        int _count;
        bool _success;
        OnRemove[] _onRemove;
        OnRemove<T>[] _onRemoveT;

        public override bool Pre(World value, Model model)
        {
            _include = model.Random.NextState();
            _entities = value.Entities().Where(entity => value.Components().Has<T>(entity, _include)).ToArray();
            _count = value.Components().Count<T>(_include);
            return true;
        }
        public override void Do(World value, Model model)
        {
            var onRemove = value.Messages().Receiver<OnRemove>();
            var onRemoveT = value.Messages().Receiver<OnRemove<T>>();
            {
                _success = value.Components().Clear<T>(_include);
                foreach (var entity in _entities) model.Components[entity].Remove(typeof(T), _include);
            }
            _onRemove = onRemove.Pop().ToArray();
            _onRemoveT = onRemoveT.Pop().ToArray();
            value.Messages().Remove(onRemove);
            value.Messages().Remove(onRemoveT);
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var entities = value.Entities();
                var components = value.Components();

                yield return (components.Get<T>(_include).None(), "Components.Get<T>().None()");
                yield return (components.Get(typeof(T), _include).None(), "Components.Get().None()");
                yield return (components.Count<T>(_include) == 0, "Components.Count<T>() == 0");
                yield return (components.Count(typeof(T), _include) == 0, "Components.Count() == 0");
                yield return (entities.None(entity => components.Has<T>(entity, _include)), "Entities.None(Components.Has<T>())");
                yield return (entities.None(entity => components.Has(entity, typeof(T), _include)), "Entities.None(Components.Has())");
                yield return (entities.None(entity => components.TryGet<T>(entity, out _, _include)), "Entities.None(Components.TryGet<T>())");
                yield return (entities.None(entity => components.TryGet(entity, typeof(T), out _, _include)), "Entities.None(Components.TryGet())");
                yield return (entities.All(entity => components.Get(entity, _include).OfType<T>().None()), "Entities.All(Components.Get().OfType<T>().None())");
                yield return (_entities.All(entity => components.State<T>(entity) == States.None), "Entities.All(Components.State<T>() == States.None)");
                yield return (_entities.All(entity => components.State(entity, typeof(T)) == States.None), "Entities.All(Components.State() == States.None)");

                yield return (_onRemove.Length >= _entities.Length, "onRemove.Length >= entities.Length");
                yield return (_onRemoveT.Length >= _entities.Length, "onRemoveT.Length = entities.Length");
                yield return (_onRemove.Length == _count, "onRemove.Length == count");
                yield return (_onRemoveT.Length == _count, "onRemoveT.Length == count");
                yield return (_onRemove.All(message => message.Component.Type.Is<T>()), "onRemove.All(Is<T>())");
                yield return (_entities.OrderBy(_ => _).SequenceEqual(_onRemove.Select(message => message.Entity).Distinct().OrderBy(_ => _)), "OnRemove.Entity");
                yield return (_entities.OrderBy(_ => _).SequenceEqual(_onRemoveT.Select(message => message.Entity).Distinct().OrderBy(_ => _)), "OnRemoveT.Entity");

                if (_success)
                {
                    yield return (_onRemove.Length > 0, "onRemove.Length > 0");
                    yield return (_onRemoveT.Length > 0, "onRemoveT.Length > 0");
                    yield return (_count > 0, "count > 0");
                }

                yield return (_entities.None(entity => components.Enable<T>(entity)), "Entities.None(Components.Enable<T>())");
                yield return (_entities.None(entity => components.Enable(entity, typeof(T))), "Entities.None(Components.Enable())");
                yield return (_entities.None(entity => components.Disable<T>(entity)), "Entities.None(Components.Disable<T>())");
                yield return (_entities.None(entity => components.Disable(entity, typeof(T))), "Entities.None(Components.Disable())");
                yield return (components.Clear<T>(_include).Not(), "Components.Clear<T>().Not()");
                yield return (components.Clear(typeof(T), _include).Not(), "Components.Clear().Not()");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_include}, {_count}, {_success})";
    }
}
