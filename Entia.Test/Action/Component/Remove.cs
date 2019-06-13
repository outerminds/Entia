using System.Linq;
using Entia.Messages;
using Entia.Modules;
using Entia.Core;
using FsCheck;
using System;
using System.Collections.Generic;
using Entia.Modules.Component;

namespace Entia.Test
{
    public class RemoveComponent : Action<World, Model>
    {
        readonly Type _type;
        States _include;
        Entity _entity;
        bool _success;
        OnRemove[] _onRemove;

        public RemoveComponent(Type type) { _type = type; }

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            if (entities.Count <= 0) return false;
            _include = model.Random.NextState();
            _entity = model.Random.NextEntity(entities);
            return true;
        }
        public override void Do(World value, Model model)
        {
            using (var onRemove = value.Messages().Receive<OnRemove>())
            {
                _success = value.Components().Remove(_entity, _type, _include);
                model.Components[_entity].Remove(_type, _include);
                _onRemove = onRemove.Pop().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (components.Has(_entity, _type, _include).Not(), "components.Has().Not()");
                yield return (components.Get(_entity, _include).OfType(_type, true, true).None(), "components.Get().OfType(type).None()");
                yield return (components.TryGet(_entity, _type, out _, _include).Not(), "components.TryGet().Not()");
                yield return (components.State(_entity, _type).HasNone(_include), "components.State()");
                yield return (_onRemove.All(message => message.Entity == _entity && message.Component.Type.Is(_type, true, true)), "OnRemove");

                if (_success)
                    yield return (_onRemove.Length > 0, "onRemove.Length");
                else
                    yield return (_onRemove.Length == 0, "onRemove.Length");

                if (_include.HasAny(States.Disabled))
                    yield return (components.Enable(_entity, _type).Not(), "components.Enable().Not()");

                if (_include.HasAny(States.Enabled))
                    yield return (components.Disable(_entity, _type).Not(), "components.Disable().Not()");

                yield return (components.Remove(_entity, _type, _include).Not(), "components.Remove().Not()");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_entity}, {_type.Format()}, {_include}, {_success})";
    }

    public class RemoveComponent<T> : Action<World, Model> where T : struct, IComponent
    {
        States _include;
        Entity _entity;
        bool _success;
        OnRemove[] _onRemove;
        OnRemove<T>[] _onRemoveT;

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            if (entities.Count <= 0) return false;
            _include = model.Random.NextState();
            _entity = model.Random.NextEntity(entities);
            return true;
        }
        public override void Do(World value, Model model)
        {
            var messages = value.Messages();
            using (var onRemove = messages.Receive<OnRemove>())
            using (var onRemoveT = messages.Receive<OnRemove<T>>())
            {
                _success = value.Components().Remove<T>(_entity, _include);
                model.Components[_entity].Remove(typeof(T), _include);
                _onRemove = onRemove.Pop().ToArray();
                _onRemoveT = onRemoveT.Pop().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (components.Has<T>(_entity, _include).Not(), "components.Has<T>().Not()");
                yield return (components.Has(_entity, typeof(T), _include).Not(), "components.Has().Not()");
                yield return (components.Get(_entity, _include).OfType<T>().None(), "components.Get().OfType<T>().None()");
                yield return (components.TryGet<T>(_entity, out _, _include).Not(), "components.TryGet<T>().Not()");
                yield return (components.TryGet(_entity, typeof(T), out _, _include).Not(), "components.TryGet().Not()");
                yield return (components.State<T>(_entity).HasNone(_include), "components.State<T>()");
                yield return (components.State(_entity, typeof(T)).HasNone(_include), "components.State()");
                yield return (_onRemove.All(message => message.Entity == _entity && message.Component.Type.Is<T>()), "OnRemove");
                yield return (_onRemoveT.All(message => message.Entity == _entity), "OnRemoveT");

                if (_success)
                {
                    yield return (_onRemove.Length > 0, "onRemove.Length > 0");
                    yield return (_onRemoveT.Length > 0, "onRemoveT.Length > 0");
                }
                else
                {
                    yield return (_onRemove.Length == 0, "onRemove.Length == 0");
                    yield return (_onRemoveT.Length == 0, "onRemoveT.Length == 0");
                }

                if (_include.HasAny(States.Disabled))
                {
                    yield return (components.Enable<T>(_entity).Not(), "components.Enable<T>().Not()");
                    yield return (components.Enable(_entity, typeof(T)).Not(), "components.Enable().Not()");
                }

                if (_include.HasAny(States.Enabled))
                {
                    yield return (components.Disable<T>(_entity).Not(), "components.Disable<T>().Not()");
                    yield return (components.Disable(_entity, typeof(T)).Not(), "components.Disable().Not()");
                }
                yield return (components.Remove<T>(_entity, _include).Not(), "components.Remove<T>().Not()");
                yield return (components.Remove(_entity, typeof(T), _include).Not(), "components.Remove().Not()");
            }
        }

        public override string ToString() => $"{GetType().Format()}({_entity}, {_include}, {_success})";
    }
}