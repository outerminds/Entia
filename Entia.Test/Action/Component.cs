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
    public class AddComponent<TConcrete, TAbstract> : Action<World, Model> where TConcrete : struct, TAbstract where TAbstract : IComponent
    {
        Type _abstract;
        Entity _entity;
        OnAdd[] _onAdd;
        OnAdd<TConcrete>[] _onAddT;

        public AddComponent(Type @abstract) { _abstract = @abstract; }

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count()));
            if (value.Components().Has<TConcrete>(entity)) return false;
            _entity = entity;
            return true;
        }
        public override void Do(World value, Model model)
        {
            var onAdd = value.Messages().Receiver<OnAdd>();
            var onAddT = value.Messages().Receiver<OnAdd<TConcrete>>();
            {
                value.Components().Set(_entity, default(TConcrete));
                model.Components[_entity][typeof(TConcrete)] = default(TConcrete);
            }
            _onAdd = onAdd.Pop().ToArray();
            _onAddT = onAddT.Pop().ToArray();
            value.Messages().Remove(onAdd);
            value.Messages().Remove(onAddT);
        }
        public override Property Check(World value, Model model) =>
            value.Components().Has<TConcrete>(_entity).Label("Components.Has<TConcrete>()")
            .And(value.Components().Has<TAbstract>(_entity).Label("Components.Has<TAbstract>()"))
            .And(value.Components().Has<IComponent>(_entity).Label("Components.Has<IComponent>()"))
            .And(value.Components().Has(_entity, typeof(TConcrete)).Label("Components.Has(TConcrete)"))
            .And(value.Components().Has(_entity, typeof(TAbstract)).Label("Components.Has(TAbstract)"))
            .And(value.Components().Has(_entity, _abstract).Label("Components.Has(abstract)"))
            .And(value.Components().Has(_entity, typeof(IComponent)).Label("Components.Has(IComponent)"))

            .And((value.Components().Count<TConcrete>() == value.Components().Count(typeof(TConcrete))).Label("Components.Count<TConcrete>() == Components.Count(TConcrete)"))
            .And((value.Components().Count<TAbstract>() == value.Components().Count(typeof(TAbstract))).Label("Components.Count<TAbstract>() == Components.Count(TAbstract)"))
            .And((value.Components().Count<IComponent>() == value.Components().Count(typeof(IComponent))).Label("Components.Count<IComponent>() == Components.Count(IComponent)"))
            .And((value.Components().Count<TConcrete>() == model.Components.Count(pair => pair.Value.ContainsKey(typeof(TConcrete)))).Label("Components.Count<TConcrete>() == model.Components"))
            .And((value.Components().Count<TConcrete>() == value.Entities().Count(value.Components().Has<TConcrete>)).Label("Components.Count<TConcrete>() == Entities.Components"))
            .And((value.Components().Count<TAbstract>() == value.Entities().Count(value.Components().Has<TAbstract>)).Label("Components.Count<TAbstract>() == Entities.Components"))
            .And((value.Components().Count<IComponent>() == value.Entities().Count(value.Components().Has<IComponent>)).Label("Components.Count<IComponent>() == Entities.Components"))
            .And((value.Components().Count(typeof(TConcrete)) == model.Components.Count(pair => pair.Value.ContainsKey(typeof(TConcrete)))).Label("Components.Count(TConcrete) == model.Components"))
            .And((value.Components().Count(typeof(TConcrete)) == value.Entities().Count(entity => value.Components().Has(entity, typeof(TConcrete)))).Label("Components.Count(TConcrete) == Entities.Components"))
            .And((value.Components().Count(typeof(TAbstract)) == value.Entities().Count(entity => value.Components().Has(entity, typeof(TAbstract)))).Label("Components.Count(TAbstract) == Entities.Components"))
            .And((value.Components().Count(_abstract) == value.Entities().Count(entity => value.Components().Has(entity, _abstract))).Label("Components.Count(abstract) == Entities.Components"))
            .And((value.Components().Count(typeof(IComponent)) == value.Entities().Count(entity => value.Components().Has(entity, typeof(IComponent)))).Label("Components.Count(IComponent) == Entities.Components"))

            .And(value.Components().Get<TConcrete>().Any().Label("Components.Get<TConcrete>().Any()"))
            .And(value.Components().Get(typeof(TConcrete)).Any().Label("Components.Get(TConcrete).Any()"))
            .And(value.Components().Get(typeof(TAbstract)).None().Label("Components.Get(TAbstract).None()"))
            .And(value.Components().Get(_abstract).None().Label("Components.Get(abstract).None()"))
            .And(value.Components().Get(typeof(IComponent)).None().Label("Components.Get(IComponent).None()"))
            .And((value.Components().Get<TConcrete>().Count() == value.Components().Get(typeof(TConcrete)).Count()).Label("Components.Get<TConcrete>().Count() == Components.Get(TConcrete).Count()"))
            .And((value.Components().Get<TConcrete>().Count() == value.Entities().Count(value.Components().Has<TConcrete>)).Label("Components.Get<TConcrete>().Count()"))
            .And((value.Components().Get<TConcrete>().Count() == model.Components.Count(pair => pair.Value.ContainsKey(typeof(TConcrete)))).Label("Components.Get<TConcrete>().Count() == model.Components"))
            .And((value.Components().Get(typeof(TConcrete)).Count() == value.Entities().Count(entity => value.Components().Has(entity, typeof(TConcrete)))).Label("Components.Get(TConcrete).Count()"))
            .And((value.Components().Get(typeof(TConcrete)).Count() == model.Components.Count(pair => pair.Value.ContainsKey(typeof(TConcrete)))).Label("Components.Get(TConcrete).Count() == model.Components"))
            .And(value.Components().Get(_entity).OfType<TConcrete>().Any().Label("Components.Get().OfType<TConcrete>().Any()"))
            .And(value.Components().Get(_entity).OfType<TAbstract>().Any().Label("Components.Get().OfType<TAbstract>().Any()"))
            .And(value.Components().Get(_entity).OfType(_abstract, true, true).Any().Label("Components.Get().OfType(abstract).Any()"))
            .And(value.Components().Get(_entity).OfType<IComponent>().Any().Label("Components.Get().OfType<IComponent>().Any()"))

            .And(value.Components().TryGet<TConcrete>(_entity, out _).Label("Components.TryGet<TConcrete>()"))
            .And(value.Components().TryGet(_entity, typeof(TConcrete), out _).Label("Components.TryGet(TConcrete)"))
            .And(value.Components().TryGet(_entity, typeof(TAbstract), out _).Not().Label("Components.TryGet<T>(TAbstract).Not()"))
            .And(value.Components().TryGet(_entity, _abstract, out _).Not().Label("Components.TryGet<T>(abstract).Not()"))
            .And(value.Components().TryGet(_entity, typeof(IComponent), out _).Not().Label("Components.TryGet<T>(IComponent).Not()"))
            .And(value.Components().TryGet(_entity, typeof(void), out _).Not().Label("Components.TryGet<T>(void).Not()"))

            .And(value.Components().Set(_entity, default(TConcrete)).Not().Label("Components.Set<TConcrete>().Not()"))
            .And(value.Components().Set(_entity, default(TConcrete)).Not().Label("Components.Set(TConcrete).Not()"))

            .And((_onAdd.Length == 1 && _onAdd[0].Entity == _entity && _onAdd[0].Component.Type == typeof(TConcrete)).Label("OnAdd"))
            .And((_onAddT.Length == 1 && _onAddT[0].Entity == _entity).Label("OnAddT"));
        public override string ToString() => $"{GetType().Format()}({_entity}, {_abstract.Format()})";
    }

    public class RemoveComponent : Action<World, Model>
    {
        Type _type;
        Entity _entity;
        OnRemove[] _onRemove;

        public RemoveComponent(Type type) { _type = type; }

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count()));
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
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count()));
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
