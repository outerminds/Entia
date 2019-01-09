using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Message;
using FsCheck;
using System.Linq;

namespace Entia.Test
{
	public class AddTag<T> : Action<World, Model> where T : struct, ITag
	{
		Entity _entity;
		OnAdd[] _onAdd;
		OnAdd<T>[] _onAddT;

		public override bool Pre(World value, Model model)
		{
			if (value.Entities().Count() <= 0) return false;
			var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count()));
			if (value.Tags().Has<T>(entity)) return false;
			_entity = entity;
			return true;
		}
		public override void Do(World value, Model model)
		{
			var onAdd = value.Messages().Receiver<OnAdd>();
			var onAddT = value.Messages().Receiver<OnAdd<T>>();
			{
				value.Tags().Set<T>(_entity);
				model.Tags[_entity].Add(typeof(T));
			}
			_onAdd = onAdd.Pop().ToArray();
			_onAddT = onAddT.Pop().ToArray();
			value.Messages().Remove(onAdd);
			value.Messages().Remove(onAddT);
		}
		public override Property Check(World value, Model model) =>
			value.Tags().Has<T>(_entity).Label("Tags.Has<T>()")
			.And(value.Tags().Has(_entity, typeof(T)).Label("Tags.Has()"))
			.And(value.Tags().Set<T>(_entity).Not().Label("Tags.Set<T>()"))
			.And(value.Tags().Set(_entity, typeof(T)).Not().Label("Tags.Set()"))
			.And(value.Tags().Get(_entity).Contains(typeof(T)).Label("Tags.Get().Contains()"))
			.And((_onAdd.Length == 1 && _onAdd[0].Entity == _entity && _onAdd[0].Type == typeof(T)).Label("OnAdd"))
			.And((_onAddT.Length == 1 && _onAddT[0].Entity == _entity).Label("OnAddT"));
		public override string ToString() => $"{GetType().Format()}({_entity})";
	}

	public class RemoveTag<T> : Action<World, Model> where T : struct, ITag
	{
		Entity _entity;
		OnRemove[] _onRemove;
		OnRemove<T>[] _onRemoveT;

		public override bool Pre(World value, Model model)
		{
			if (value.Entities().Count() <= 0) return false;
			var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count()));
			if (value.Tags().Has<T>(entity))
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
				value.Tags().Remove<T>(_entity);
				model.Tags[_entity].Remove(typeof(T));
			}
			_onRemove = onRemove.Pop().ToArray();
			_onRemoveT = onRemoveT.Pop().ToArray();
			value.Messages().Remove(onRemove);
			value.Messages().Remove(onRemoveT);
		}
		public override Property Check(World value, Model model) =>
			value.Tags().Has<T>(_entity).Not().Label("Tags.Has<T>()")
			.And(value.Tags().Has(_entity, typeof(T)).Not().Label("Tags.Has()"))
			.And(value.Tags().Remove<T>(_entity).Not().Label("Tags.Remove<T>()"))
			.And(value.Tags().Remove(_entity, typeof(T)).Not().Label("Tags.Remove<T>()"))
			.And(value.Tags().Get(_entity).Contains(typeof(T)).Not().Label("Tags.Get().Contains()"))
			.And((_onRemove.Length == 1 && _onRemove[0].Entity == _entity && _onRemove[0].Type == typeof(T)).Label("OnRemove"))
			.And((_onRemoveT.Length == 1 && _onRemoveT[0].Entity == _entity).Label("OnRemoveT"));
		public override string ToString() => $"{GetType().Format()}({_entity})";
	}

	public class ClearTag<T> : Action<World, Model> where T : struct, ITag
	{
		Entity[] _entities;
		OnRemove[] _onRemove;
		OnRemove<T>[] _onRemoveT;

		public override bool Pre(World value, Model model)
		{
			_entities = value.Entities().Where(entity => value.Tags().Has<T>(entity)).ToArray();
			return true;
		}
		public override void Do(World value, Model model)
		{
			var onRemove = value.Messages().Receiver<OnRemove>();
			var onRemoveT = value.Messages().Receiver<OnRemove<T>>();
			{
				value.Tags().Clear<T>();
				model.Tags.Iterate(pair => pair.Value.Remove(typeof(T)));
			}
			_onRemove = onRemove.Pop().ToArray();
			_onRemoveT = onRemoveT.Pop().ToArray();
			value.Messages().Remove(onRemove);
			value.Messages().Remove(onRemoveT);
		}
		public override Property Check(World value, Model model) =>
			value.Tags().Contains(typeof(T)).Not().Label("Tags.Contains()")
			.And(value.Tags().Clear<T>().Not().Label("Tags.Clear<T>()"))
			.And(value.Tags().Clear(typeof(T)).Not().Label("Tags.Clear()"))
			.And((_entities.Length == _onRemove.Length).Label("OnRemove.Length"))
			.And(_onRemove.All(message => message.Type == typeof(T)).Label("OnRemove.Type"))
			.And(_entities.OrderBy(_ => _).SequenceEqual(_onRemove.Select(message => message.Entity).OrderBy(_ => _)).Label("OnRemove.Entity"))
			.And((_entities.Length == _onRemoveT.Length).Label("OnRemove<T>.Length"))
			.And(_entities.OrderBy(_ => _).SequenceEqual(_onRemoveT.Select(message => message.Entity).OrderBy(_ => _)).Label("OnRemoveT.Entity"));
		public override string ToString() => GetType().Format();
	}

}
