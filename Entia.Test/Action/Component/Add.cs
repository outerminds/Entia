using System;
using System.Linq;
using Entia.Messages;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Message;
using FsCheck;

namespace Entia.Test
{
    public class AddComponent<TConcrete, TAbstract> : Action<World, Model> where TConcrete : struct, TAbstract where TAbstract : IComponent
    {
        Type _abstract;
        Entity _entity;
        TConcrete _component;
        OnAdd[] _onAdd;
        OnAdd<TConcrete>[] _onAddT;

        public AddComponent(Type @abstract) { _abstract = @abstract; }

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            if (value.Components().Has<TConcrete>(entity)) return false;
            _entity = entity;
            _component = value.Components().Default<TConcrete>();
            return true;
        }
        public override void Do(World value, Model model)
        {
            var onAdd = value.Messages().Receiver<OnAdd>();
            var onAddT = value.Messages().Receiver<OnAdd<TConcrete>>();
            {
                value.Components().Set(_entity, _component);
                model.Components[_entity][typeof(TConcrete)] = _component;
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
            .And((value.Components().Get<TConcrete>(_entity).Equals(_component)).Label("Components.Get<TConcrete>() == component"))
            .And((value.Components().Get(_entity, typeof(TConcrete)).Equals(_component)).Label("Components.Get(TConcrete) == component"))

            .And(value.Components().TryGet<TConcrete>(_entity, out _).Label("Components.TryGet<TConcrete>()"))
            .And(value.Components().TryGet(_entity, typeof(TConcrete), out _).Label("Components.TryGet(TConcrete)"))
            .And(value.Components().TryGet(_entity, typeof(TAbstract), out _).Not().Label("Components.TryGet<T>(TAbstract).Not()"))
            .And(value.Components().TryGet(_entity, _abstract, out _).Not().Label("Components.TryGet<T>(abstract).Not()"))
            .And(value.Components().TryGet(_entity, typeof(IComponent), out _).Not().Label("Components.TryGet<T>(IComponent).Not()"))
            .And(value.Components().TryGet(_entity, typeof(void), out _).Not().Label("Components.TryGet<T>(void).Not()"))

            .And(value.Components().Set(_entity, (IComponent)_component).Not().Label("Components.Set(Entity, component).Not()"))
            .And(value.Components().Set(_entity, _component).Not().Label("Components.Set<TConcrete>(Entity, component).Not()"))
            .And(value.Components().Set(_entity, typeof(TConcrete)).Not().Label("Components.Set(Entity, TConcrete).Not()"))
            .And(value.Components().Set<TConcrete>(_entity).Not().Label("Components.Set<TConcrete>(Entity).Not()"))

            .And((_onAdd.Length == 1 && _onAdd[0].Entity == _entity && _onAdd[0].Component.Type == typeof(TConcrete)).Label("OnAdd"))
            .And((_onAddT.Length == 1 && _onAddT[0].Entity == _entity).Label("OnAddT"));
        public override string ToString() => $"{GetType().Format()}({_entity}, {_abstract.Format()})";
    }
}