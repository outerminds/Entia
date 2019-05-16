using System.Linq;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Message;
using Entia.Core;
using FsCheck;
using System;

namespace Entia.Test
{
    public class EnableComponent : Action<World, Model>
    {
        Type _type;
        Entity _entity;
        OnEnable[] _onEnable;

        public EnableComponent(Type type) { _type = type; }

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            if (value.Components().Has(entity, _type, States.Disabled))
            {
                _entity = entity;
                return true;
            }
            return false;
        }
        public override void Do(World value, Model model)
        {
            var onEnable = value.Messages().Receiver<OnEnable>();
            {
                value.Components().Enable(_entity, _type);
            }
            _onEnable = onEnable.Pop().ToArray();
            value.Messages().Remove(onEnable);
        }
        public override Property Check(World value, Model model) =>
            value.Components().Has(_entity, _type, States.Enabled).Label("Components.Has(Type, Enabled)")
            .And(value.Components().Has(_entity, _type, States.Disabled).Not().Label("Components.Has(Type, Disabled).Not()"))
            .And(value.Components().Has(_entity, _type).Label("Components.Has(Type)"))

            .And((value.Components().Count(_entity, States.Enabled) > 0).Label("Components.Count(Enabled)"))
            .And((value.Components().Count(_entity) > 0).Label("Components.Count()"))

            .And(value.Components().Get(_entity, States.Enabled).OfType(_type, true, true).Any().Label("Components.Get(Enabled).Any()"))
            .And(value.Components().TryGet(_entity, _type, out _, States.Disabled).Not().Label("Components.TryGet(Type, Disabled).Not()"))
            .And(value.Components().Get(_entity, States.Disabled).OfType(_type, true, true).None().Label("Components.Get(Disabled).None()"))
            .And(value.Components().Get(_entity).OfType(_type, true, true).Any().Label("Components.Get().Any()"))

            .And(value.Components().Enable(_entity, _type).Not().Label("Components.Enable(Type).Not()"))
            .And((_onEnable.Length >= 1 && _onEnable.All(message => message.Entity == _entity && message.Component.Type.Is(_type, true, true))).Label("OnEnable"));
        public override string ToString() => $"{GetType().Format()}({_entity}, {_type.Format()})";
    }

    public class EnableComponent<T> : Action<World, Model> where T : struct, IComponent
    {
        Entity _entity;
        OnEnable[] _onEnable;
        OnEnable<T>[] _onEnableT;

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            if (value.Components().Has<T>(entity, States.Disabled))
            {
                _entity = entity;
                return true;
            }
            return false;
        }
        public override void Do(World value, Model model)
        {
            var onEnable = value.Messages().Receiver<OnEnable>();
            var onEnableT = value.Messages().Receiver<OnEnable<T>>();
            {
                value.Components().Enable<T>(_entity);
            }
            _onEnable = onEnable.Pop().ToArray();
            _onEnableT = onEnableT.Pop().ToArray();
            value.Messages().Remove(onEnable);
            value.Messages().Remove(onEnableT);
        }
        public override Property Check(World value, Model model) =>
            (value.Components().State<T>(_entity) == States.Enabled).Label("Components.State<T>()")
            .And((value.Components().State(_entity, typeof(T)) == States.Enabled).Label("Components.State()"))

            .And(value.Components().Has<T>(_entity, States.Enabled).Label("Components.Has<T>(Enabled)"))
            .And(value.Components().Has(_entity, typeof(T), States.Enabled).Label("Components.Has(Type, Enabled)"))
            .And(value.Components().Has<T>(_entity, States.Disabled).Not().Label("Components.Has<T>(Disabled).Not()"))
            .And(value.Components().Has(_entity, typeof(T), States.Disabled).Not().Label("Components.Has(Type, Disabled).Not()"))
            .And(value.Components().Has<T>(_entity).Label("Components.Has<T>()"))
            .And(value.Components().Has(_entity, typeof(T)).Label("Components.Has(Type)"))

            .And((value.Components().Count(_entity, States.Enabled) > 0).Label("Components.Count(Enabled)"))
            .And((value.Components().Count(_entity) > 0).Label("Components.Count()"))

            .And(value.Components().TryGet<T>(_entity, out _, States.Enabled).Label("Components.TryGet<T>(Enabled)"))
            .And(value.Components().TryGet(_entity, typeof(T), out _, States.Enabled).Label("Components.TryGet(Type, Enabled)"))
            .And(value.Components().Get(_entity, States.Enabled).OfType<T>().Any().Label("Components.Get(Enabled).Any()"))
            .And(value.Components().TryGet<T>(_entity, out _, States.Disabled).Not().Label("Components.TryGet<T>(Disabled).Not()"))
            .And(value.Components().TryGet(_entity, typeof(T), out _, States.Disabled).Not().Label("Components.TryGet(Type, Disabled).Not()"))
            .And(value.Components().Get(_entity, States.Disabled).OfType<T>().None().Label("Components.Get(Disabled).None()"))
            .And(value.Components().TryGet<T>(_entity, out _).Label("Components.TryGet<T>()"))
            .And(value.Components().TryGet(_entity, typeof(T), out _).Label("Components.TryGet(Type)"))
            .And(value.Components().Get(_entity).OfType<T>().Any().Label("Components.Get().Any()"))

            .And(value.Components().Enable<T>(_entity).Not().Label("Components.Enable<T>().Not()"))
            .And(value.Components().Enable(_entity, typeof(T)).Not().Label("Components.Enable(Type).Not()"))
            .And((_onEnable.Length == 1 && _onEnable[0].Entity == _entity && _onEnable[0].Component.Type == typeof(T)).Label("OnEnable"))
            .And((_onEnableT.Length == 1 && _onEnableT[0].Entity == _entity).Label("OnEnableT"));
        public override string ToString() => $"{GetType().Format()}({_entity})";
    }
}