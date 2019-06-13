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
    public class DisableComponent : Action<World, Model>
    {
        Type _type;
        Entity _entity;
        bool _success;
        OnDisable[] _onDisable;

        public DisableComponent(Type type) { _type = type; }

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            if (entities.Count <= 0) return false;
            _entity = model.Random.NextEntity(entities);
            return true;
        }
        public override void Do(World value, Model model)
        {
            var messages = value.Messages();
            using (var onDisable = messages.Receive<OnDisable>())
            {
                _success = value.Components().Disable(_entity, _type);
                model.Components[_entity].Disable(_type);
                _onDisable = onDisable.Pop().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (components.Get(_entity, States.Enabled).OfType(_type, true, true).None(), "Components.Get(Enabled).None()");
                yield return (components.TryGet(_entity, _type, out _, States.Enabled).Not(), "Components.TryGet(Type, Enabled).Not()");
                yield return (components.Has(_entity, _type, States.Enabled).Not(), "Components.Has(Type, Enabled).Not()");
                yield return (_onDisable.All(message => message.Entity == _entity && message.Component.Type.Is(_type, true, true)), "onDisable.All()");

                if (_success)
                {
                    yield return (components.State(_entity).HasAny(States.Disabled), "Components.State()");
                    yield return (components.State(_entity, _type).HasAny(States.Disabled), "Components.State()");

                    yield return (components.Has(States.Disabled), "Components.Has<T>(Disabled)");
                    yield return (components.Has(), "Components.Has<T>()");
                    yield return (components.Has(_type, States.Disabled), "Components.Has(type, Disabled)");
                    yield return (components.Has(_type), "Components.Has(type)");
                    yield return (components.Has(_entity, _type, States.Disabled), "Components.Has(entity, Type, Disabled)");
                    yield return (components.Has(_entity, _type), "Components.Has(entity, Type)");

                    yield return (components.Count(States.Disabled) > 0, "Components.Count(Disabled)");
                    yield return (components.Count() > 0, "Components.Count()");
                    yield return (components.Count(_type) > 0, "Components.Count(type)");
                    yield return (components.Count(_type, States.Disabled) > 0, "Components.Count(type, Disabled)");
                    yield return (components.Count(_entity, States.Disabled) > 0, "Components.Count(entity, Disabled)");
                    yield return (components.Count(_entity) > 0, "Components.Count(entity)");

                    yield return (components.Get().OfType(_type, true, true).Any(), "Components.Get().Any()");
                    yield return (components.Get(States.Disabled).OfType(_type, true, true).Any(), "Components.Get(Disabled).Any()");
                    yield return (components.Get(_entity, States.Disabled).OfType(_type, true, true).Any(), "Components.Get(Disabled).Any()");
                    yield return (components.Get(_entity).OfType(_type, true, true).Any(), "Components.Get().Any()");

                    yield return (components.TryGet(_entity, _type, out _), "Components.TryGet()");
                    yield return (components.TryGet(_entity, _type, out _, States.Disabled), "Components.TryGet(Disabled)");

                    yield return (_onDisable.Length > 0, "onDisable.Length > 0");
                }
                else
                {
                    yield return (_onDisable.Length == 0, "onDisable.Length == 0");
                }

                yield return (components.Disable(_entity, _type).Not(), "Components.Disable(Type).Not()");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_entity}, {_type.Format()}, {_success})";
    }

    public class DisableComponent<T> : Action<World, Model> where T : struct, IComponent
    {
        Entity _entity;
        bool _success;
        OnDisable[] _onDisable;
        OnDisable<T>[] _onDisableT;

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            if (entities.Count <= 0) return false;
            _entity = model.Random.NextEntity(entities);
            return true;
        }
        public override void Do(World value, Model model)
        {
            var messages = value.Messages();
            using (var onDisable = messages.Receive<OnDisable>())
            using (var onDisableT = messages.Receive<OnDisable<T>>())
            {
                _success = value.Components().Disable<T>(_entity);
                model.Components[_entity].Disable(typeof(T));
                _onDisable = onDisable.Pop().ToArray();
                _onDisableT = onDisableT.Pop().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (components.Get(_entity, States.Enabled).OfType<T>().None(), "Components.Get(Enabled).None()");
                yield return (components.TryGet<T>(_entity, out _, States.Enabled).Not(), "Components.TryGet<T>(Enabled).Not()");
                yield return (components.TryGet(_entity, typeof(T), out _, States.Enabled).Not(), "Components.TryGet(Type, Enabled).Not()");
                yield return (components.Has<T>(_entity, States.Enabled).Not(), "Components.Has<T>(Enabled).Not()");
                yield return (components.Has(_entity, typeof(T), States.Enabled).Not(), "Components.Has(Type, Enabled).Not()");

                yield return (_onDisable.All(message => message.Entity == _entity && message.Component.Type.Is<T>()), "onDisable.All()");
                yield return (_onDisableT.All(message => message.Entity == _entity), "onDisableT.All()");

                if (_success)
                {
                    yield return (components.State(_entity).HasAny(States.Disabled), "Components.State()");
                    yield return (components.State<T>(_entity).HasAny(States.Disabled), "Components.State<T>()");
                    yield return (components.State(_entity, typeof(T)).HasAny(States.Disabled), "Components.State()");

                    yield return (components.Has(States.Disabled), "Components.Has<T>(Disabled)");
                    yield return (components.Has(), "Components.Has<T>()");
                    yield return (components.Has<T>(States.Disabled), "Components.Has<T>(Disabled)");
                    yield return (components.Has<T>(), "Components.Has<T>()");
                    yield return (components.Has(typeof(T), States.Disabled), "Components.Has(type, Disabled)");
                    yield return (components.Has(typeof(T)), "Components.Has(type)");
                    yield return (components.Has<T>(_entity, States.Disabled), "Components.Has<T>(entity, Disabled)");
                    yield return (components.Has(_entity, typeof(T), States.Disabled), "Components.Has(entity, Type, Disabled)");
                    yield return (components.Has<T>(_entity), "Components.Has<T>(entity)");
                    yield return (components.Has(_entity, typeof(T)), "Components.Has(entity, Type)");

                    yield return (components.Count(States.Disabled) > 0, "Components.Count(Disabled)");
                    yield return (components.Count() > 0, "Components.Count()");
                    yield return (components.Count<T>() > 0, "Components.Count<T>()");
                    yield return (components.Count<T>(States.Disabled) > 0, "Components.Count<T>(Disabled)");
                    yield return (components.Count(typeof(T)) > 0, "Components.Count(type)");
                    yield return (components.Count(typeof(T), States.Disabled) > 0, "Components.Count(type, Disabled)");
                    yield return (components.Count(_entity, States.Disabled) > 0, "Components.Count(entity, Disabled)");
                    yield return (components.Count(_entity) > 0, "Components.Count(entity)");

                    yield return (components.Get().OfType<T>().Any(), "Components.Get().Any()");
                    yield return (components.Get(States.Disabled).OfType<T>().Any(), "Components.Get(Disabled).Any()");
                    yield return (components.Get(_entity, States.Disabled).OfType<T>().Any(), "Components.Get(Disabled).Any()");
                    yield return (components.Get(_entity).OfType<T>().Any(), "Components.Get().Any()");

                    yield return (components.TryGet<T>(_entity, out _, States.Disabled), "Components.TryGet<T>(Disabled)");
                    yield return (components.TryGet(_entity, typeof(T), out _, States.Disabled), "Components.TryGet(Type, Disabled)");
                    yield return (components.TryGet<T>(_entity, out _), "Components.TryGet<T>()");
                    yield return (components.TryGet(_entity, typeof(T), out _), "Components.TryGet(Type)");

                    yield return (_onDisable.Length > 0, "onDisable.Length > 0");
                    yield return (_onDisableT.Length > 0, "onDisable.Length > 0");
                }
                else
                {
                    yield return (_onDisable.Length == 0, "onDisable.Length == 0");
                    yield return (_onDisableT.Length == 0, "onDisableT.Length == 0");
                }

                yield return (components.Disable<T>(_entity).Not(), "Components.Disable<T>().Not()");
                yield return (components.Disable(_entity, typeof(T)).Not(), "Components.Disable(Type).Not()");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_entity}, {_success})";
    }
}