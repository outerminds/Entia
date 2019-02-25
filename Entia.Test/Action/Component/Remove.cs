using System.Linq;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Message;
using Entia.Core;
using FsCheck;
using System;

namespace Entia.Test
{
    public class RemoveComponent : Action<World, Model>
    {
        Type _type;
        Entity _entity;
        OnRemove[] _onRemove;

        public RemoveComponent(Type type) { _type = type; }

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            if (value.Components().Has(entity, _type))
            {
                _entity = entity;
                return true;
            }
            return false;
        }
        public override void Do(World value, Model model)
        {
            var onRemove = value.Messages().Receiver<OnRemove>();
            {
                value.Components().Remove(_entity, _type);
                var components = model.Components[_entity];
                components.Keys.ToArray().Iterate(key => { if (key.Is(_type, true, true)) components.Remove(key); });
            }
            _onRemove = onRemove.Pop().ToArray();
            value.Messages().Remove(onRemove);
        }
        public override Property Check(World value, Model model) =>
            value.Components().Has(_entity, _type).Not().Label("Components.Has()")
            .And(value.Components().Get(_entity).OfType(_type, true, true).None().Label("Components.Get().OfType(type).None()"))
            .And((_onRemove.Length > 0).Label("onRemove.Length"))
            .And(_onRemove.All(message => message.Entity == _entity && message.Component.Type.Is(_type, true, true)).Label("OnRemove"))
            .And(value.Components().Remove(_entity, _type).Not().Label("Components.Remove(type)"));
        public override string ToString() => $"{GetType().Format()}({_entity}, {_type.Format()})";
    }

    public class RemoveComponent<T> : Action<World, Model> where T : struct, IComponent
    {
        Entity _entity;
        OnRemove[] _onRemove;
        OnRemove<T>[] _onRemoveT;

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            if (value.Components().Has<T>(entity))
            {
                _entity = entity;
                return true;
            }
            return false;
        }
        public override void Do(World value, Model model)
        {
            var onRemove = value.Messages().Receiver<OnRemove>();
            var onRemoveT = value.Messages().Receiver<OnRemove<T>>();
            {
                value.Components().Remove<T>(_entity);
                model.Components[_entity].Remove(typeof(T));
            }
            _onRemove = onRemove.Pop().ToArray();
            _onRemoveT = onRemoveT.Pop().ToArray();
            value.Messages().Remove(onRemove);
            value.Messages().Remove(onRemoveT);
        }
        public override Property Check(World value, Model model) =>
            value.Components().Has<T>(_entity).Not().Label("Components.Has<T>().Not()")
            .And(value.Components().Has(_entity, typeof(T)).Not().Label("Components.Has().Not()"))
            .And(value.Components().Remove<T>(_entity).Not().Label("Components.Remove<T>().Not()"))
            .And(value.Components().Remove(_entity, typeof(T)).Not().Label("Components.Remove().Not()"))
            .And(value.Components().Get(_entity).OfType<T>().None().Label("Components.Get().OfType<T>().None()"))
            .And(value.Components().TryGet<T>(_entity, out _).Not().Label("Components.TryGet<T>().Not()"))
            .And(value.Components().TryGet(_entity, typeof(T), out _).Not().Label("Components.TryGet().Not()"))
            .And((_onRemove.Length == 1 && _onRemove[0].Entity == _entity && _onRemove[0].Component.Type == typeof(T)).Label("OnRemove"))
            .And((_onRemoveT.Length == 1 && _onRemoveT[0].Entity == _entity).Label("OnRemoveT"));
        public override string ToString() => $"{GetType().Format()}({_entity})";
    }
}