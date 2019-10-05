using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Message;
using FsCheck;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Test
{
    public class CreateEntity : Action<World, Model>
    {
        Entity _entity;
        OnCreate[] _onCreate;

        public override void Do(World value, Model model)
        {
            var entities = value.Entities();
            var messages = value.Messages();
            using (var onCreate = messages.Receive<OnCreate>())
            {
                _entity = entities.Create();
                model.Entities.Add(_entity);
                model.Components.Add(_entity, new ComponentModel());
                _onCreate = onCreate.Messages().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool tests, string label)> Tests()
            {
                var entities = value.Entities();
                var components = value.Components();

                yield return (entities.Except(model.Entities).None(), "Entities.Except().None()");
                yield return (entities.Distinct().SequenceEqual(entities), "Entities.Distinct()");
                yield return (entities.Has(_entity), "Entities.Has()");
                yield return (entities.Contains(_entity), "Entities.Contains()");
                yield return (entities.Count == model.Entities.Count, "Entities.Count");

                yield return (components.Get(_entity).None(), "Components.Get().None()");
                yield return (components.State(_entity) == States.None, "Components.State() == States.None");

                yield return (_onCreate.Length == 1 && _onCreate[0].Entity == _entity, "OnCreate");

                yield return (components.Enable(_entity).Not(), "Components.Enable().Not()");
                yield return (components.Disable(_entity).Not(), "Components.Disable().Not()");
                yield return (components.Clear(_entity).Not(), "Components.Clear().Not()");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_entity})";
    }

    public class DestroyEntity : Action<World, Model>
    {
        Entity _entity;
        Entity[] _entities;
        OnPreDestroy[] _onPreDestroy;
        OnPostDestroy[] _onPostDestroy;

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            var families = value.Families();
            if (entities.Count <= 0) return false;
            _entity = entities.ElementAt(model.Random.Next(entities.Count));
            _entities = families.Descendants(_entity).Prepend(_entity).ToArray();
            return true;
        }
        public override void Do(World value, Model model)
        {
            var messages = value.Messages();
            var families = value.Families();
            using (var onPreDestroy = messages.Receive<OnPreDestroy>())
            using (var onPostDestroy = messages.Receive<OnPostDestroy>())
            {
                value.Entities().Destroy(_entity);
                foreach (var entity in _entities)
                {
                    model.Entities.Remove(entity);
                    model.Components.Remove(entity);
                }
                _onPreDestroy = onPreDestroy.Messages().ToArray();
                _onPostDestroy = onPostDestroy.Messages().ToArray();
            }
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool tests, string label)> Tests()
            {
                yield return (model.Entities.Except(value.Entities()).None(), "Entities.Except().None()");
                yield return (value.Entities().Has(_entity).Not(), "Entities.Has()");
                yield return (value.Entities().Destroy(_entity).Not(), "Entities.Destroy()");
                yield return (value.Entities().Count == model.Entities.Count, "Entities.Count");
                yield return (_onPreDestroy.Length == _entities.Length, "OnPreDestroy.Length");
                yield return (_onPreDestroy.First().Entity == _entity, "OnPreDestroy.First() == entity");
                yield return (_onPreDestroy.Select(message => message.Entity).Except(_entities).None(), "OnPostDestroy.Except(entities).None()");
                yield return (_onPostDestroy.Length == _entities.Length, "OnPostDestroy.Length");
                yield return (_onPostDestroy.Select(message => message.Entity).Except(_entities).None(), "OnPostDestroy.Except(entities).None()");
                yield return (_onPostDestroy.Last().Entity == _entity, "OnPostDestroy.Last() == entity");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_entity})";
    }

    public class ClearEntities : Action<World, Model>
    {
        Entity[] _entities;
        OnPreDestroy[] _onPreDestroy;
        OnPostDestroy[] _onPostDestroy;

        public override bool Pre(World value, Model model)
        {
            _entities = value.Entities().ToArray();
            return true;
        }
        public override void Do(World value, Model model)
        {
            var messages = value.Messages();
            using (var onPreDestroy = messages.Receive<OnPreDestroy>())
            using (var onPostDestroy = messages.Receive<OnPostDestroy>())
            {
                value.Entities().Clear();
                model.Entities.Clear();
                model.Components.Clear();
                _onPreDestroy = onPreDestroy.Messages().ToArray();
                _onPostDestroy = onPostDestroy.Messages().ToArray();
            }
        }
        public override Property Check(World value, Model model) =>
            value.Entities().None().Label("Entities.None()")
            .And((value.Entities().Count == 0).Label("Entities.Count"))
            .And(_entities.None(value.Entities().Has).Label("Entities.Has()"))
            .And(_entities.None(value.Entities().Destroy).Label("Entities.Destroy()"))
            .And(value.Entities().Clear().Not().Label("Entities.Clear().Not()"))
            .And(value.Components().None().Label("Components.None()"))
            .And((_onPreDestroy.Length == _onPostDestroy.Length).Label("OnDestroy.Length"))
            .And((_entities.Length == _onPreDestroy.Length).Label("OnPreDestroy.Length"))
            .And(_entities.OrderBy(_ => _).SequenceEqual(_onPreDestroy.Select(message => message.Entity).OrderBy(_ => _)).Label("OnPreDestroy.Entity"))
            .And((_entities.Length == _onPostDestroy.Length).Label("OnPostDestroy.Length"))
            .And(_entities.OrderBy(_ => _).SequenceEqual(_onPostDestroy.Select(message => message.Entity).OrderBy(_ => _)).Label("OnPostDestroy.Entity"));
        public override string ToString() => $"{GetType().Format()}({_entities.Length})";
    }
}
