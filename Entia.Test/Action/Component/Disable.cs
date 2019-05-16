using System.Linq;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Message;
using Entia.Core;
using FsCheck;
using System;

namespace Entia.Test
{
    public class DisableComponent : Action<World, Model>
    {
        Type _type;
        Entity _entity;
        OnDisable[] _onDisable;

        public DisableComponent(Type type) { _type = type; }

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            if (value.Components().Has(entity, _type, States.Enabled))
            {
                _entity = entity;
                return true;
            }
            return false;
        }
        public override void Do(World value, Model model)
        {
            var onDisable = value.Messages().Receiver<OnDisable>();
            {
                value.Components().Disable(_entity, _type);
            }
            _onDisable = onDisable.Pop().ToArray();
            value.Messages().Remove(onDisable);
        }
        public override Property Check(World value, Model model) =>
            value.Components().Has(_entity, _type, States.Disabled).Label("Components.Has(Type, Disabled)")
            .And(value.Components().Has(_entity, _type, States.Enabled).Not().Label("Components.Has(Type, Enabled).Not()"))
            .And(value.Components().Has(_entity, _type).Label("Components.Has(Type)"))

            .And((value.Components().Count(_entity, States.Disabled) > 0).Label("Components.Count(Disabled)"))
            .And((value.Components().Count(_entity) > 0).Label("Components.Count()"))

            .And(value.Components().Get(_entity, States.Disabled).OfType(_type, true, true).Any().Label("Components.Get(Disabled).Any()"))
            .And(value.Components().TryGet(_entity, _type, out _, States.Enabled).Not().Label("Components.TryGet(Type, Enabled).Not()"))
            .And(value.Components().Get(_entity, States.Enabled).OfType(_type, true, true).None().Label("Components.Get(Enabled).None()"))
            .And(value.Components().Get(_entity).OfType(_type, true, true).Any().Label("Components.Get().Any()"))

            .And(value.Components().Disable(_entity, _type).Not().Label("Components.Disable(Type).Not()"))
            .And((_onDisable.Length >= 1 && _onDisable.All(message => message.Entity == _entity && message.Component.Type.Is(_type, true, true))).Label("OnEnable"));
        public override string ToString() => $"{GetType().Format()}({_entity}, {_type.Format()})";
    }

    public class DisableComponent<T> : Action<World, Model> where T : struct, IComponent
    {
        Entity _entity;
        OnDisable[] _onDisable;
        OnDisable<T>[] _onDisableT;

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            if (value.Components().Has<T>(entity, States.Enabled))
            {
                _entity = entity;
                return true;
            }
            return false;
        }
        public override void Do(World value, Model model)
        {
            var onDisable = value.Messages().Receiver<OnDisable>();
            var onDisableT = value.Messages().Receiver<OnDisable<T>>();
            {
                value.Components().Disable<T>(_entity);
            }
            _onDisable = onDisable.Pop().ToArray();
            _onDisableT = onDisableT.Pop().ToArray();
            value.Messages().Remove(onDisable);
            value.Messages().Remove(onDisableT);
        }
        public override Property Check(World value, Model model) =>
            (value.Components().State<T>(_entity) == States.Disabled).Label("Components.State<T>()")
            .And((value.Components().State(_entity, typeof(T)) == States.Disabled).Label("Components.State()"))

            .And(value.Components().Has<T>(_entity, States.Disabled).Label("Components.Has<T>(Disabled)"))
            .And(value.Components().Has(_entity, typeof(T), States.Disabled).Label("Components.Has(Type, Disabled)"))
            .And(value.Components().Has<T>(_entity, States.Enabled).Not().Label("Components.Has<T>(Enabled).Not()"))
            .And(value.Components().Has(_entity, typeof(T), States.Enabled).Not().Label("Components.Has(Type, Enabled).Not()"))
            .And(value.Components().Has<T>(_entity).Label("Components.Has<T>()"))
            .And(value.Components().Has(_entity, typeof(T)).Label("Components.Has(Type)"))

            .And((value.Components().Count(_entity, States.Disabled) > 0).Label("Components.Count(Disabled)"))
            .And((value.Components().Count(_entity) > 0).Label("Components.Count()"))

            .And(value.Components().TryGet<T>(_entity, out _, States.Disabled).Label("Components.TryGet<T>(Disabled)"))
            .And(value.Components().TryGet(_entity, typeof(T), out _, States.Disabled).Label("Components.TryGet(Type, Disabled)"))
            .And(value.Components().Get(_entity, States.Disabled).OfType<T>().Any().Label("Components.Get(Disabled).Any()"))
            .And(value.Components().TryGet<T>(_entity, out _, States.Enabled).Not().Label("Components.TryGet<T>(Enabled).Not()"))
            .And(value.Components().TryGet(_entity, typeof(T), out _, States.Enabled).Not().Label("Components.TryGet(Type, Enabled).Not()"))
            .And(value.Components().Get(_entity, States.Enabled).OfType<T>().None().Label("Components.Get(Enabled).None()"))
            .And(value.Components().TryGet<T>(_entity, out _).Label("Components.TryGet<T>()"))
            .And(value.Components().TryGet(_entity, typeof(T), out _).Label("Components.TryGet(Type)"))
            .And(value.Components().Get(_entity).OfType<T>().Any().Label("Components.Get().Any()"))

            .And(value.Components().Disable<T>(_entity).Not().Label("Components.Disable<T>().Not()"))
            .And(value.Components().Disable(_entity, typeof(T)).Not().Label("Components.Disable(Type).Not()"))
            .And((_onDisable.Length == 1 && _onDisable[0].Entity == _entity && _onDisable[0].Component.Type == typeof(T)).Label("OnDisable"))
            .And((_onDisableT.Length == 1 && _onDisableT[0].Entity == _entity).Label("OnDisableT"));
        public override string ToString() => $"{GetType().Format()}({_entity})";
    }
}