using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Message;
using FsCheck;
using System.Linq;

namespace Entia.Test
{
	public class AddComponent<T> : Action<World, Model> where T : struct, IComponent
	{
		Entity _entity;
		OnAdd[] _onAdd;
		OnAdd<T>[] _onAddT;

		public override bool Pre(World value, Model model)
		{
			if (value.Entities().Count() <= 0) return false;
			var entity = value.Entities().ElementAt(model.Random.Next(value.Entities().Count()));
			if (value.Components().Has<T>(entity)) return false;
			_entity = entity;
			return true;
		}
		public override void Do(World value, Model model)
		{
			var onAdd = value.Messages().Receiver<OnAdd>();
			var onAddT = value.Messages().Receiver<OnAdd<T>>();
			{
				value.Components().Set(_entity, default(T));
				model.Components[_entity][typeof(T)] = default(T);
			}
			_onAdd = onAdd.Pop().ToArray();
			_onAddT = onAddT.Pop().ToArray();
			value.Messages().Remove(onAdd);
			value.Messages().Remove(onAddT);
		}
		public override Property Check(World value, Model model) =>
			value.Components().Has<T>(_entity).Label("Components.Has<T>()")
			.And(value.Components().Has(_entity, typeof(T)).Label("Components.Has()"))
			.And((value.Components().Count<T>() == value.Components().Count(typeof(T))).Label("Components.Count<T>() == Components.Count(type)"))
			.And((value.Components().Count<T>() == model.Components.Count(pair => pair.Value.ContainsKey(typeof(T)))).Label("Components.Count<T>() == model.Components"))
			.And((value.Components().Count<T>() == value.Entities().Count(value.Components().Has<T>)).Label("Components.Count<T>() == Entities.Components"))
			.And((value.Components().Count(typeof(T)) == model.Components.Count(pair => pair.Value.ContainsKey(typeof(T)))).Label("Components.Count(type) == model.Components"))
			.And((value.Components().Count(typeof(T)) == value.Entities().Count(entity => value.Components().Has(entity, typeof(T)))).Label("Components.Count(type) == Entities.Components"))
			.And(value.Components().Get<T>().Any().Label("Components.Get<T>().Any()"))
			.And(value.Components().Get(typeof(T)).Any().Label("Components.Get(type).Any()"))
			.And((value.Components().Get<T>().Count() == value.Components().Get(typeof(T)).Count()).Label("Components.Get<T>().Count() == Components.Get(type).Count()"))
			.And((value.Components().Get<T>().Count() == value.Entities().Count(value.Components().Has<T>)).Label("Components.Get<T>().Count()"))
			.And((value.Components().Get<T>().Count() == model.Components.Count(pair => pair.Value.ContainsKey(typeof(T)))).Label("Components.Get<T>().Count() == model.Components"))
			.And((value.Components().Get(typeof(T)).Count() == value.Entities().Count(entity => value.Components().Has(entity, typeof(T)))).Label("Components.Get(type).Count()"))
			.And((value.Components().Get(typeof(T)).Count() == model.Components.Count(pair => pair.Value.ContainsKey(typeof(T)))).Label("Components.Get(type).Count() == model.Components"))
			.And(value.Components().Get(_entity).OfType<T>().Any().Label("Components.Get().OfType<T>().Any()"))
			.And(value.Components().TryRead<T>(_entity, out _).Label("Components.TryRead<T>()"))
			.And(value.Components().TryWrite<T>(_entity, out _).Label("Components.TryWrite<T>()"))
			.And((_onAdd.Length == 1 && _onAdd[0].Entity == _entity && _onAdd[0].Type == typeof(T)).Label("OnAdd"))
			.And((_onAddT.Length == 1 && _onAddT[0].Entity == _entity).Label("OnAddT"))
			.And(value.Components().Set(_entity, default(T)).Not().Label("Components.Set<T>()"))
			.And(value.Components().Set(_entity, default(T)).Not().Label("Components.Set()"));
		public override string ToString() => $"{GetType().Format()}({_entity})";
	}

	public class RemoveComponent<T> : Action<World, Model> where T : struct, IComponent
	{
		Entity _entity;
		OnRemove[] _onRemove;
		OnRemove<T>[] _onRemoveT;

		public override bool Pre(World value, Model model)
		{
			if (value.Entities().Count() <= 0) return false;
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
			value.Components().Has<T>(_entity).Not().Label("Components.Has<T>()")
			.And(value.Components().Has(_entity, typeof(T)).Not().Label("Components.Has()"))
			.And(value.Components().Remove<T>(_entity).Not().Label("Components.Remove<T>()"))
			.And(value.Components().Remove(_entity, typeof(T)).Not().Label("Components.Remove<T>()"))
			.And(value.Components().Get(_entity).OfType<T>().None().Label("Components.Get().OfType<T>().None()"))
			.And(value.Components().TryRead<T>(_entity, out _).Not().Label("Components.TryRead<T>()"))
			.And(value.Components().TryWrite<T>(_entity, out _).Not().Label("Components.TryWrite<T>()"))
			.And((_onRemove.Length == 1 && _onRemove[0].Entity == _entity && _onRemove[0].Type == typeof(T)).Label("OnRemove"))
			.And((_onRemoveT.Length == 1 && _onRemoveT[0].Entity == _entity).Label("OnRemoveT"));
		public override string ToString() => $"{GetType().Format()}({_entity})";
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
			.And(_onRemove.All(message => message.Type == typeof(T)).Label("OnRemove.Type"))
			.And((_entities.Length == _onRemoveT.Length).Label("OnRemove<T>.Length"))
			.And(_entities.OrderBy(_ => _).SequenceEqual(_onRemoveT.Select(message => message.Entity).OrderBy(_ => _)).Label("OnRemoveT.Entity"));
		public override string ToString() => GetType().Format();
	}
}
