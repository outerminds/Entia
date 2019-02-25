using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Message;
using Entia.Modules.Component;
using FsCheck;
using System.Linq;
using System;

namespace Entia.Test
{
    public class ClearComponent : Action<World, Model>
    {
        Type _type;
        Entity[] _entities;
        int _count;
        OnRemove[] _onRemove;

        public ClearComponent(Type type) { _type = type; }

        public override bool Pre(World value, Model model)
        {
            _entities = value.Entities().Where(entity => value.Components().Has(entity, _type)).ToArray();
            _count = value.Components().Count(_type);
            return true;
        }
        public override void Do(World value, Model model)
        {
            var onRemove = value.Messages().Receiver<OnRemove>();
            {
                value.Components().Clear(_type);
                model.Components.Iterate(pair =>
                    pair.Value.Keys.ToArray().Iterate(key => { if (key.Is(_type, true, true)) pair.Value.Remove(key); }));
            }
            _onRemove = onRemove.Pop().ToArray();
            value.Messages().Remove(onRemove);
        }
        public override Property Check(World value, Model model) =>
            value.Entities().Where(entity => value.Components().Has(entity, _type)).None().Label("Entitias.Where(Components.Has(type)).None()")
            .And(value.Entities().All(entity => value.Components().Get(entity).OfType(_type, true, true).None()).Label("Entities.All(Components.Get().None())"))
            .And(value.Components().Clear(_type).Not().Label("Components.Clear().Not()"))
            .And((_onRemove.Length == _entities.Length).Label("onRemove.Length = entities.Length"))
            .And((_onRemove.Length == _count).Label("onRemove.Length == count"))
            .And(_entities.OrderBy(_ => _).SequenceEqual(_onRemove.Select(message => message.Entity).OrderBy(_ => _)).Label("OnRemove.Entity"));
        public override string ToString() => $"{GetType().Format()}({_entities.Length}, {_type.Format()})";
    }

    public class ClearComponent<T> : Action<World, Model> where T : struct, IComponent
    {
        Entity[] _entities;
        OnRemove[] _onRemove;
        OnRemove<T>[] _onRemoveT;

        public override bool Pre(World value, Model model)
        {
            _entities = value.Entities().Where(entity => value.Components().Has<T>(entity)).ToArray();
            return true;
        }
        public override void Do(World value, Model model)
        {
            var onRemove = value.Messages().Receiver<OnRemove>();
            var onRemoveT = value.Messages().Receiver<OnRemove<T>>();
            {
                value.Components().Clear<T>();
                model.Components.Iterate(pair => pair.Value.Remove(typeof(T)));
            }
            _onRemove = onRemove.Pop().ToArray();
            _onRemoveT = onRemoveT.Pop().ToArray();
            value.Messages().Remove(onRemove);
            value.Messages().Remove(onRemoveT);
        }
        public override Property Check(World value, Model model) =>
            value.Components().Get<T>().None().Label("Components.Get<T>().None()")
            .And(value.Components().Get(typeof(T)).None().Label("Components.Get().None()"))
            .And(value.Components().Clear<T>().Not().Label("Components.Clear<T>()"))
            .And(value.Components().Clear(typeof(T)).Not().Label("Components.Clear()"))
            .And((_entities.Length == _onRemove.Length).Label("OnRemove.Length"))
            .And(_entities.OrderBy(_ => _).SequenceEqual(_onRemove.Select(message => message.Entity).OrderBy(_ => _)).Label("OnRemove.Entity"))
            .And(_onRemove.All(message => message.Component.Type == typeof(T)).Label("OnRemove.Type"))
            .And((_entities.Length == _onRemoveT.Length).Label("OnRemove<T>.Length"))
            .And(_entities.OrderBy(_ => _).SequenceEqual(_onRemoveT.Select(message => message.Entity).OrderBy(_ => _)).Label("OnRemoveT.Entity"));
        public override string ToString() => $"{GetType().Format()}({_entities.Length})";
    }
}
