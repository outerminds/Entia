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
                _onCreate = onCreate.Pop().ToArray();
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
        OnPreDestroy[] _onPreDestroy;
        OnPostDestroy[] _onPostDestroy;

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count <= 0) return false;
            _entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count));
            return true;
        }
        public override void Do(World value, Model model)
        {
            var messages = value.Messages();
            using (var onPreDestroy = messages.Receive<OnPreDestroy>())
            using (var onPostDestroy = messages.Receive<OnPostDestroy>())
            {
                value.Entities().Destroy(_entity);
                model.Entities.Remove(_entity);
                model.Components.Remove(_entity);
                _onPreDestroy = onPreDestroy.Pop().ToArray();
                _onPostDestroy = onPostDestroy.Pop().ToArray();
            }
        }
        public override Property Check(World value, Model model) =>
            model.Entities.Except(value.Entities()).None().Label("Entities.Except().None()")
            .And(value.Entities().Has(_entity).Not().Label("Entities.Has()"))
            .And(value.Entities().Destroy(_entity).Not().Label("Entities.Destroy()"))
            .And((value.Entities().Count == model.Entities.Count).Label("Entities.Count"))
            .And((_onPreDestroy.Length == _onPostDestroy.Length).Label("OnDestroy.Length"))
            .And((_onPreDestroy.Length == 1 && _onPreDestroy[0].Entity == _entity).Label("OnPreDestroy"))
            .And((_onPostDestroy.Length == 1 && _onPostDestroy[0].Entity == _entity).Label("OnPostDestroy"));
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
                _onPreDestroy = onPreDestroy.Pop().ToArray();
                _onPostDestroy = onPostDestroy.Pop().ToArray();
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
