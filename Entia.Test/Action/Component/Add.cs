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
        bool _has;
        bool _success;
        OnAdd[] _onAdd;
        OnAdd<TConcrete>[] _onAddT;

        public AddComponent(Type @abstract) { _abstract = @abstract; }

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            var components = value.Components();
            if (entities.Count <= 0) return false;
            _entity = model.Random.NextEntity(entities);
            _component = components.Default<TConcrete>();
            _has = components.Has<TConcrete>(_entity);
            return true;
        }
        public override void Do(World value, Model model)
        {
            var onAdd = value.Messages().Receiver<OnAdd>();
            var onAddT = value.Messages().Receiver<OnAdd<TConcrete>>();
            {
                _success = value.Components().Set(_entity, _component);
                model.Components[_entity].Set(typeof(TConcrete));
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

                yield return (_has != _success, "Has != Add");
                yield return (components.Has<TConcrete>(), "Components.Has<TConcrete>()");
                yield return (components.Has<TAbstract>(), "Components.Has<TAbstract>()");
                yield return (components.Has<IComponent>(), "Components.Has<IComponent>()");
                yield return (components.Has(typeof(TConcrete)), "Components.Has(TConcrete)");
                yield return (components.Has(typeof(TAbstract)), "Components.Has(TAbstract)");
                yield return (components.Has(typeof(IComponent)), "Components.Has(IComponent)");
                yield return (components.Has(_abstract), "Components.Has(abstract)");
                yield return (components.Has<TConcrete>(_entity), "Components.Has<TConcrete>(Entity)");
                yield return (components.Has<TAbstract>(_entity), "Components.Has<TAbstract>(Entity)");
                yield return (components.Has<IComponent>(_entity), "Components.Has<IComponent>(Entity)");
                yield return (components.Has(_entity, typeof(TConcrete)), "Components.Has(Entity, TConcrete)");
                yield return (components.Has(_entity, typeof(TAbstract)), "Components.Has(Entity, TAbstract)");
                yield return (components.Has(_entity, _abstract), "Components.Has(Entity, abstract)");
                yield return (components.Has(_entity, typeof(IComponent)), "Components.Has(Entity, IComponent)");

                yield return (components.Count<TConcrete>() == components.Count(typeof(TConcrete)), "Components.Count<TConcrete>() == Components.Count(TConcrete)");
                yield return (components.Count<TAbstract>() == components.Count(typeof(TAbstract)), "Components.Count<TAbstract>() == Components.Count(TAbstract)");
                yield return (components.Count<TConcrete>() == model.Components.Count(pair => pair.Value.Contains(typeof(TConcrete))), "Components.Count<TConcrete>() == model.Components");
                yield return (components.Count<TConcrete>() == entities.Count(entity => components.Has<TConcrete>(entity)), "Components.Count<TConcrete>() == Entities.Components");
                yield return (components.Count(typeof(TConcrete)) == model.Components.Count(pair => pair.Value.Contains(typeof(TConcrete))), "Components.Count(TConcrete) == model.Components");
                yield return (components.Count(typeof(TConcrete)) == entities.Count(entity => components.Has(entity, typeof(TConcrete))), "Components.Count(TConcrete) == Entities.Components");
                // NOTE: '>=' is used here because an entity may have more than one component of an abstract type
                yield return (components.Count<TAbstract>() >= entities.Count(entity => components.Has<TAbstract>(entity)), "Components.Count<TAbstract>() == Entities.Components");
                yield return (components.Count(typeof(TAbstract)) >= entities.Count(entity => components.Has(entity, typeof(TAbstract))), "Components.Count(TAbstract) >= Entities.Components");
                yield return (components.Count(_abstract) >= entities.Count(entity => components.Has(entity, _abstract)), "Components.Count(abstract) >= Entities.Components");

                yield return (components.Get<TConcrete>().Any(), "Components.Get<TConcrete>().Any()");
                yield return (components.Get(typeof(TConcrete)).Any(), "Components.Get(TConcrete).Any()");
                yield return (components.Get(typeof(TAbstract)).Any(), "Components.Get(TAbstract).Any()");
                yield return (components.Get(_abstract).Any(), "Components.Get(abstract).Any()");
                yield return (components.Get(typeof(IComponent)).Any(), "Components.Get(IComponent).Any()");
                yield return (components.Get<TConcrete>().Count() == components.Get(typeof(TConcrete)).Count(), "Components.Get<TConcrete>().Count() == Components.Get(TConcrete).Count()");
                yield return (components.Get<TConcrete>().Count() == entities.Count(entity => components.Has<TConcrete>(entity)), "Components.Get<TConcrete>().Count()");
                yield return (components.Get<TConcrete>().Count() == model.Components.Count(pair => pair.Value.Contains(typeof(TConcrete))), "Components.Get<TConcrete>().Count() == model.Components");
                yield return (components.Get(typeof(TConcrete)).Count() == entities.Count(entity => components.Has(entity, typeof(TConcrete))), "Components.Get(TConcrete).Count()");
                yield return (components.Get(typeof(TConcrete)).Count() == model.Components.Count(pair => pair.Value.Contains(typeof(TConcrete))), "Components.Get(TConcrete).Count() == model.Components");
                yield return (components.Get(_entity).OfType<TConcrete>().Any(), "Components.Get().OfType<TConcrete>().Any()");
                yield return (components.Get(_entity).OfType<TAbstract>().Any(), "Components.Get().OfType<TAbstract>().Any()");
                yield return (components.Get(_entity).OfType(_abstract, true, true).Any(), "Components.Get().OfType(abstract).Any()");
                yield return (components.Get(_entity).OfType<IComponent>().Any(), "Components.Get().OfType<IComponent>().Any()");
                yield return (components.Get<TConcrete>(_entity).Equals(_component), "Components.Get<TConcrete>() == component");
                yield return (components.Get(_entity, typeof(TConcrete)).Equals(_component), "Components.Get(TConcrete) == component");

                yield return (components.TryGet<TConcrete>(_entity, out _), "Components.TryGet<TConcrete>()");
                yield return (components.TryGet(_entity, typeof(TConcrete), out _), "Components.TryGet(TConcrete)");
                yield return (components.TryGet(_entity, typeof(TAbstract), out _), "Components.TryGet<T>(TAbstract)");
                yield return (components.TryGet(_entity, _abstract, out _), "Components.TryGet<T>(abstract)");
                yield return (components.TryGet(_entity, typeof(IComponent), out _), "Components.TryGet<T>(IComponent)");
                yield return (components.TryGet(_entity, typeof(void), out _).Not(), "Components.TryGet<T>(void).Not()");

                yield return (_onAdd.All(message => message.Entity == _entity && message.Component.Type.Is<TConcrete>()), "onAdd.All");
                yield return (_onAddT.All(message => message.Entity == _entity), "onAddT.All");

                if (_success)
                {
                    yield return (components.State<TConcrete>(_entity) == States.Enabled, "Components.State<T>()");
                    yield return (components.State(_entity, typeof(TConcrete)) == States.Enabled, "Components.State(Type)");
                    yield return (components.Enable<TConcrete>(_entity).Not(), "Components.Enable<T>()");
                    yield return (components.Enable(_entity, typeof(TConcrete)).Not(), "Components.Enable(Type)");
                    yield return (_onAdd.Length == 1, "onAdd.Length == 1");
                    yield return (_onAddT.Length == 1, "onAddT.Length == 1");
                }
                else
                {
                    yield return (_onAdd.Length == 0, "onAdd.Length == 0");
                    yield return (_onAddT.Length == 0, "onAddT.Length == 0");
                }

                yield return (components.Set(_entity, (IComponent)_component).Not(), "Components.Set(Entity, component).Not()");
                yield return (components.Set(_entity, _component).Not(), "Components.Set<TConcrete>(Entity, component).Not()");
                yield return (components.Set(_entity, typeof(TConcrete)).Not(), "Components.Set(Entity, TConcrete).Not()");
                yield return (components.Set<TConcrete>(_entity).Not(), "Components.Set<TConcrete>(Entity).Not()");
            }
        }

        public override string ToString() => $"{GetType().Format()}({_entity}, {_abstract.Format()}, {_success})";
    }
}