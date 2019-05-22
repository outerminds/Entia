using System.Linq;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Message;
using Entia.Core;
using FsCheck;
using System;
using System.Collections.Generic;
using Entia.Components;

namespace Entia.Test
{
    public class EnableComponent : Action<World, Model>
    {
        Type _type;
        Entity _entity;
        bool _success;
        OnEnable[] _onEnable;

        public EnableComponent(Type type) { _type = type; }

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
            var onEnable = messages.Receiver<OnEnable>();
            _success = value.Components().Enable(_entity, _type);
            model.Components[_entity].Enable(_type);
            _onEnable = onEnable.Pop().ToArray();
            messages.Remove(onEnable);
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (components.Get(_entity, States.Disabled).OfType(_type, true, true).None(), "Components.Get(Disabled).None()");
                yield return (components.TryGet(_entity, _type, out _, States.Disabled).Not(), "Components.TryGet(Type, Disabled).Not()");
                yield return (components.Has(_entity, _type, States.Disabled).Not(), "Components.Has(Type, Disabled).Not()");
                yield return (_onEnable.All(message => message.Entity == _entity && message.Component.Type.Is(_type, true, true)), "onEnable.All()");

                if (_success)
                {
                    yield return (components.Has(_entity, _type, States.Enabled), "Components.Has(Type, Enabled)");
                    yield return (components.Has(_entity, _type), "Components.Has(Type)");

                    yield return (components.Count(_entity, States.Enabled) > 0, "Components.Count(Enabled)");
                    yield return (components.Count(_entity) > 0, "Components.Count()");

                    yield return (components.Get(_entity, States.Enabled).OfType(_type, true, true).Any(), "Components.Get(Enabled).Any()");
                    yield return (components.Get(_entity).OfType(_type, true, true).Any(), "Components.Get().Any()");

                    yield return (components.TryGet(_entity, _type, out _), "Components.TryGet()");
                    yield return (components.TryGet(_entity, _type, out _, States.Enabled), "Components.TryGet(Enabled)");

                    yield return (_onEnable.Length > 0, "onEnable.Length > 0");
                }
                else
                {
                    yield return (_onEnable.Length == 0, "onEnable.Length == 0");
                }

                yield return (components.Enable(_entity, _type).Not(), "Components.Enable(Type).Not()");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_entity}, {_type.Format()}, {_success})";
    }

    public class EnableComponent<T> : Action<World, Model> where T : struct, IComponent
    {
        Entity _entity;
        bool _success;
        OnEnable[] _onEnable;
        OnEnable<T>[] _onEnableT;

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
            var onEnable = messages.Receiver<OnEnable>();
            var onEnableT = messages.Receiver<OnEnable<T>>();
            _success = value.Components().Enable<T>(_entity);
            model.Components[_entity].Enable(typeof(T));
            _onEnable = onEnable.Pop().ToArray();
            _onEnableT = onEnableT.Pop().ToArray();
            messages.Remove(onEnable);
            messages.Remove(onEnableT);
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var components = value.Components();

                yield return (components.Get(_entity, States.Disabled).OfType<T>().None(), "Components.Get(Disabled).None()");
                yield return (components.TryGet<T>(_entity, out _, States.Disabled).Not(), "Components.TryGet<T>(Disabled).Not()");
                yield return (components.TryGet(_entity, typeof(T), out _, States.Disabled).Not(), "Components.TryGet(Type, Disabled).Not()");
                yield return (components.Has<T>(_entity, States.Disabled).Not(), "Components.Has<T>(Disabled).Not()");
                yield return (components.Has(_entity, typeof(T), States.Disabled).Not(), "Components.Has(Type, Disabled).Not()");

                yield return (_onEnable.All(message => message.Entity == _entity && message.Component.Type.Is<T>()), "onEnable.All()");
                yield return (_onEnableT.All(message => message.Entity == _entity), "onEnableT.All()");

                if (_success)
                {
                    yield return (components.State<T>(_entity) == States.Enabled, "Components.State<T>()");
                    yield return (components.State(_entity, typeof(T)) == States.Enabled, "Components.State()");

                    yield return (components.Has<T>(_entity, States.Enabled), "Components.Has<T>(Enabled)");
                    yield return (components.Has(_entity, typeof(T), States.Enabled), "Components.Has(Type, Enabled)");
                    yield return (components.Has<T>(_entity), "Components.Has<T>()");
                    yield return (components.Has(_entity, typeof(T)), "Components.Has(Type)");

                    yield return (components.Count(_entity, States.Enabled) > 0, "Components.Count(Enabled)");
                    yield return (components.Count(_entity) > 0, "Components.Count()");

                    yield return (components.Get(_entity, States.Enabled).OfType<T>().Any(), "Components.Get(Enabled).Any()");
                    yield return (components.Get(_entity).OfType<T>().Any(), "Components.Get().Any()");

                    yield return (components.TryGet<T>(_entity, out _, States.Enabled), "Components.TryGet<T>(Enabled)");
                    yield return (components.TryGet(_entity, typeof(T), out _, States.Enabled), "Components.TryGet(Type, Enabled)");
                    yield return (components.TryGet<T>(_entity, out _), "Components.TryGet<T>()");
                    yield return (components.TryGet(_entity, typeof(T), out _), "Components.TryGet(Type)");

                    yield return (_onEnable.Length > 0, "onEnable.Length > 0");
                    yield return (_onEnableT.Length > 0, "onEnable.Length > 0");
                }
                else
                {
                    yield return (_onEnable.Length == 0, "onEnable.Length == 0");
                    yield return (_onEnableT.Length == 0, "onEnableT.Length == 0");
                }

                yield return (components.Enable<T>(_entity).Not(), "Components.Enable<T>().Not()");
                yield return (components.Enable(_entity, typeof(T)).Not(), "Components.Enable(Type).Not()");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_entity}, {_success})";
    }
}