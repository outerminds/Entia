using System;
using System.Linq;
using Entia.Messages;
using Entia.Core;
using Entia.Modules;
using Entia.Modules.Message;
using FsCheck;
using System.Collections.Generic;

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
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool test, string label)> Tests()
            {
                var entities = value.Entities();
                var components = value.Components();

                yield return (components.Has<TConcrete>(_entity), "Components.Has<TConcrete>()");
                yield return (components.Has<TAbstract>(_entity), "Components.Has<TAbstract>()");
                yield return (components.Has<IComponent>(_entity), "Components.Has<IComponent>()");
                yield return (components.Has(_entity, typeof(TConcrete)), "Components.Has(TConcrete)");
                yield return (components.Has(_entity, typeof(TAbstract)), "Components.Has(TAbstract)");
                yield return (components.Has(_entity, _abstract), "Components.Has(abstract)");
                yield return (components.Has(_entity, typeof(IComponent)), "Components.Has(IComponent)");

                yield return (components.Count<TConcrete>() == components.Count(typeof(TConcrete)), "Components.Count<TConcrete>() == Components.Count(TConcrete)");
                yield return (components.Count<TAbstract>() == components.Count(typeof(TAbstract)), "Components.Count<TAbstract>() == Components.Count(TAbstract)");
                yield return (components.Count<IComponent>() == components.Count(typeof(IComponent)), "Components.Count<IComponent>() == Components.Count(IComponent)");
                yield return (components.Count<TConcrete>() == model.Components.Count(pair => pair.Value.ContainsKey(typeof(TConcrete))), "Components.Count<TConcrete>() == model.Components");
                yield return (components.Count<TConcrete>() == entities.Count(entity => components.Has<TConcrete>(entity)), "Components.Count<TConcrete>() == Entities.Components");
                yield return (components.Count<TAbstract>() == entities.Count(entity => components.Has<TAbstract>(entity)), "Components.Count<TAbstract>() == Entities.Components");
                yield return (components.Count<IComponent>() == entities.Count(entity => components.Has<IComponent>(entity)), "Components.Count<IComponent>() == Entities.Components");
                yield return (components.Count(typeof(TConcrete)) == model.Components.Count(pair => pair.Value.ContainsKey(typeof(TConcrete))), "Components.Count(TConcrete) == model.Components");
                yield return (components.Count(typeof(TConcrete)) == entities.Count(entity => components.Has(entity, typeof(TConcrete))), "Components.Count(TConcrete) == Entities.Components");
                yield return (components.Count(typeof(TAbstract)) == entities.Count(entity => components.Has(entity, typeof(TAbstract))), "Components.Count(TAbstract) == Entities.Components");
                yield return (components.Count(_abstract) == entities.Count(entity => components.Has(entity, _abstract)), "Components.Count(abstract) == Entities.Components");
                yield return (components.Count(typeof(IComponent)) == entities.Count(entity => components.Has(entity, typeof(IComponent))), "Components.Count(IComponent) == Entities.Components");

                yield return (components.Get<TConcrete>().Any(), "Components.Get<TConcrete>().Any()");
                yield return (components.Get(typeof(TConcrete)).Any(), "Components.Get(TConcrete).Any()");
                yield return (components.Get(typeof(TAbstract)).None(), "Components.Get(TAbstract).None()");
                yield return (components.Get(_abstract).None(), "Components.Get(abstract).None()");
                yield return (components.Get(typeof(IComponent)).None(), "Components.Get(IComponent).None()");
                yield return (components.Get<TConcrete>().Count() == components.Get(typeof(TConcrete)).Count(), "Components.Get<TConcrete>().Count() == Components.Get(TConcrete).Count()");
                yield return (components.Get<TConcrete>().Count() == entities.Count(entity => components.Has<TConcrete>(entity)), "Components.Get<TConcrete>().Count()");
                yield return (components.Get<TConcrete>().Count() == model.Components.Count(pair => pair.Value.ContainsKey(typeof(TConcrete))), "Components.Get<TConcrete>().Count() == model.Components");
                yield return (components.Get(typeof(TConcrete)).Count() == entities.Count(entity => components.Has(entity, typeof(TConcrete))), "Components.Get(TConcrete).Count()");
                yield return (components.Get(typeof(TConcrete)).Count() == model.Components.Count(pair => pair.Value.ContainsKey(typeof(TConcrete))), "Components.Get(TConcrete).Count() == model.Components");
                yield return (components.Get(_entity).OfType<TConcrete>().Any(), "Components.Get().OfType<TConcrete>().Any()");
                yield return (components.Get(_entity).OfType<TAbstract>().Any(), "Components.Get().OfType<TAbstract>().Any()");
                yield return (components.Get(_entity).OfType(_abstract, true, true).Any(), "Components.Get().OfType(abstract).Any()");
                yield return (components.Get(_entity).OfType<IComponent>().Any(), "Components.Get().OfType<IComponent>().Any()");
                yield return (components.Get<TConcrete>(_entity).Equals(_component), "Components.Get<TConcrete>() == component");
                yield return (components.Get(_entity, typeof(TConcrete)).Equals(_component), "Components.Get(TConcrete) == component");

                yield return (components.TryGet<TConcrete>(_entity, out _), "Components.TryGet<TConcrete>()");
                yield return (components.TryGet(_entity, typeof(TConcrete), out _), "Components.TryGet(TConcrete)");
                yield return (components.TryGet(_entity, typeof(TAbstract), out _).Not(), "Components.TryGet<T>(TAbstract).Not()");
                yield return (components.TryGet(_entity, _abstract, out _).Not(), "Components.TryGet<T>(abstract).Not()");
                yield return (components.TryGet(_entity, typeof(IComponent), out _).Not(), "Components.TryGet<T>(IComponent).Not()");
                yield return (components.TryGet(_entity, typeof(void), out _).Not(), "Components.TryGet<T>(void).Not()");

                yield return (components.Set(_entity, (IComponent)_component).Not(), "Components.Set(Entity, component).Not()");
                yield return (components.Set(_entity, _component).Not(), "Components.Set<TConcrete>(Entity, component).Not()");
                yield return (components.Set(_entity, typeof(TConcrete)).Not(), "Components.Set(Entity, TConcrete).Not()");
                yield return (components.Set<TConcrete>(_entity).Not(), "Components.Set<TConcrete>(Entity).Not()");

                yield return (_onAdd.Length == 1 && _onAdd[0].Entity == _entity && _onAdd[0].Component.Type == typeof(TConcrete), "OnAdd");
                yield return (_onAddT.Length == 1 && _onAddT[0].Entity == _entity, "OnAddT");
            }
        }

        public override string ToString() => $"{GetType().Format()}({_entity}, {_abstract.Format()})";
    }
}