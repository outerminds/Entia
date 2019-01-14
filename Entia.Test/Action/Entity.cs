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

        public override bool Pre(World value, Model model) => true;
        public override void Do(World value, Model model)
        {
            var onCreate = value.Messages().Receiver<OnCreate>();
            {
                _entity = value.Entities().Create();
                model.Entities.Add(_entity);
                model.Components.Add(_entity, new Dictionary<Type, IComponent>());
                model.Tags.Add(_entity, new HashSet<Type>());
            }
            _onCreate = onCreate.Pop().ToArray();
            value.Messages().Remove(onCreate);
        }
        public override Property Check(World value, Model model) =>
            value.Entities().Except(model.Entities).None().Label("Entities.Except().None()")
            .And(value.Entities().Distinct().SequenceEqual(value.Entities()).Label("Entities.Distinct()"))
            .And(value.Entities().Has(_entity).Label("Entities.Has()"))
            .And(value.Entities().Contains(_entity).Label("Entities.Contains()"))
            .And((value.Entities().Count() == model.Entities.Count).Label("Entities.Count"))
            .And((_onCreate.Length == 1 && _onCreate[0].Entity == _entity).Label("OnCreate"));
        public override string ToString() => $"{GetType().Format()}({_entity})";
    }

    public class DestroyEntity : Action<World, Model>
    {
        Entity _entity;
        OnPreDestroy[] _onPreDestroy;
        OnPostDestroy[] _onPostDestroy;

        public override bool Pre(World value, Model model)
        {
            if (value.Entities().Count() <= 0) return false;
            _entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count()));
            return true;
        }
        public override void Do(World value, Model model)
        {
            var onPreDestroy = value.Messages().Receiver<OnPreDestroy>();
            var onPostDestroy = value.Messages().Receiver<OnPostDestroy>();
            {
                value.Entities().Destroy(_entity);
                model.Entities.Remove(_entity);
                model.Components.Remove(_entity);
                model.Tags.Remove(_entity);
            }
            _onPreDestroy = onPreDestroy.Pop().ToArray();
            _onPostDestroy = onPostDestroy.Pop().ToArray();
            value.Messages().Remove(onPreDestroy);
            value.Messages().Remove(onPostDestroy);
        }
        public override Property Check(World value, Model model) =>
            model.Entities.Except(value.Entities()).None().Label("Entities.Except().None()")
            .And(value.Entities().Has(_entity).Not().Label("Entities.Has()"))
            .And(value.Entities().Destroy(_entity).Not().Label("Entities.Destroy()"))
            .And((value.Entities().Count() == model.Entities.Count).Label("Entities.Count"))
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
            var onPreDestroy = value.Messages().Receiver<OnPreDestroy>();
            var onPostDestroy = value.Messages().Receiver<OnPostDestroy>();
            {
                value.Entities().Clear();
                model.Entities.Clear();
                model.Components.Clear();
                model.Tags.Clear();
            }
            _onPreDestroy = onPreDestroy.Pop().ToArray();
            _onPostDestroy = onPostDestroy.Pop().ToArray();
            value.Messages().Remove(onPreDestroy);
            value.Messages().Remove(onPostDestroy);
        }
        public override Property Check(World value, Model model) =>
            value.Entities().None().Label("Entities.None()")
            .And((value.Entities().Count() == 0).Label("Entities.Count"))
            .And(_entities.None(value.Entities().Has).Label("Entities.Has()"))
            .And(_entities.None(value.Entities().Destroy).Label("Entities.Destroy()"))
            .And(value.Entities().Clear().Not().Label("Entities.Clear()"))
            .And(value.Components().None().Label("Components.None()"))
            .And((_onPreDestroy.Length == _onPostDestroy.Length).Label("OnDestroy.Length"))
            .And((_entities.Length == _onPreDestroy.Length).Label("OnPreDestroy.Length"))
            .And(_entities.OrderBy(_ => _).SequenceEqual(_onPreDestroy.Select(message => message.Entity).OrderBy(_ => _)).Label("OnPreDestroy.Entity"))
            .And((_entities.Length == _onPostDestroy.Length).Label("OnPostDestroy.Length"))
            .And(_entities.OrderBy(_ => _).SequenceEqual(_onPostDestroy.Select(message => message.Entity).OrderBy(_ => _)).Label("OnPostDestroy.Entity"));
        public override string ToString() => GetType().Format();
    }
}
