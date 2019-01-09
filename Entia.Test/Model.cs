using Entia.Modules.Group;
using System;
using System.Collections.Generic;

namespace Entia.Test
{
	public class Model
	{
		public readonly Random Random;
		public readonly HashSet<Entity> Entities = new HashSet<Entity>();
		public readonly Dictionary<Entity, Dictionary<Type, IComponent>> Components = new Dictionary<Entity, Dictionary<Type, IComponent>>();
		public readonly Dictionary<Entity, HashSet<Type>> Tags = new Dictionary<Entity, HashSet<Type>>();
		public readonly HashSet<IGroup> Groups = new HashSet<IGroup>();

		public Model(int seed) { Random = new Random(seed); }
	}
}

namespace Entia.Test
{
	//public interface IAction { }

	//namespace Action
	//{
	//	public sealed class CreateEntity : IAction { public Entity Entity; }
	//	public sealed class DestroyEntity : IAction { public Entity Entity; }
	//	public sealed class ClearEntities : IAction { }
	//	public sealed class ReceiveMessage : IAction { public IMessage Message; }
	//	public sealed class SetComponent : IAction { public Entity Entity; public IComponent Component; }
	//	public sealed class AddComponent : IAction { public Entity Entity; public IComponent Component; }
	//	public sealed class RemoveComponent : IAction { public Entity Entity; public Type Type; }
	//	public sealed class ClearComponentOfType : IAction { public Type Type; }
	//	public sealed class AddTag : IAction { public Entity Entity; public Type Type; public bool Success; }
	//	public sealed class RemoveTag : IAction { public Entity Entity; public Type Type; public bool Success; }
	//	public sealed class ClearTagOfType : IAction { public Type Type; }
	//}

	//public class Model
	//{
	//	public IEnumerable<Entity> CreatedEntities => _actions.OfType<Action.CreateEntity>().Select(action => action.Entity);
	//	public IEnumerable<Entity> DestroyedEntities => _actions.OfType<Action.DestroyEntity>().Select(action => action.Entity);
	//	public IEnumerable<Entity> Entities
	//	{
	//		get
	//		{
	//			var entities = new List<Entity>();
	//			foreach (var action in _actions)
	//			{
	//				switch (action)
	//				{
	//					case Action.CreateEntity create: entities.Add(create.Entity); break;
	//					case Action.DestroyEntity destroy: entities.Remove(destroy.Entity); break;
	//					case Action.ClearEntities clear: entities.Clear(); break;
	//				}
	//			}
	//			return entities;
	//		}
	//	}

	//	public IEnumerable<(Entity, IComponent)> AddedComponents =>
	//		_actions.OfType<Action.AddComponent>().Select(action => (action.Entity, action.Component));
	//	public IEnumerable<(Entity, IComponent)> SetComponents =>
	//		_actions.OfType<Action.SetComponent>().Select(action => (action.Entity, action.Component));
	//	public IEnumerable<(Entity, Type)> RemovedComponents =>
	//		_actions.OfType<Action.RemoveComponent>().Select(action => (action.Entity, action.Type));
	//	public IEnumerable<(Entity, IComponent)> Components =>
	//		EntityToComponents.SelectMany(pair => pair.Value.Select(component => (pair.Key, component)));
	//	public IDictionary<Entity, IComponent[]> EntityToComponents
	//	{
	//		get
	//		{
	//			var components = new Dictionary<Entity, List<IComponent>>();
	//			foreach (var action in _actions)
	//			{
	//				switch (action)
	//				{
	//					case Action.CreateEntity create: components.Add(create.Entity, new List<IComponent>()); break;
	//					case Action.DestroyEntity destroy: components.Remove(destroy.Entity); break;
	//					case Action.ClearEntities clearEntities: components.Clear(); break;
	//					case Action.SetComponent set:
	//					{
	//						var list = components[set.Entity];
	//						var index = list.FindIndex(component => component.GetType() == set.Component.GetType());
	//						list[index] = set.Component;
	//						break;
	//					}
	//					case Action.AddComponent add:
	//						components[add.Entity].Add(add.Component);
	//						break;
	//					case Action.RemoveComponent remove:
	//						components[remove.Entity].RemoveAll(component => component.GetType() == remove.Type);
	//						break;
	//					case Action.ClearComponentOfType clearOfType:
	//					{
	//						foreach (var list in components.Values)
	//							list.RemoveAll(component => component.GetType() == clearOfType.Type);
	//						break;
	//					}
	//				}
	//			}
	//			return components.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
	//		}
	//	}
	//	public IDictionary<Type, IAction[]> ComponentActions => _actions
	//		.Select(action =>
	//			action is Action.SetComponent set ? (set.Component.GetType(), action) :
	//			action is Action.AddComponent add ? (add.Component.GetType(), action) :
	//			action is Action.RemoveComponent remove ? (remove.Type, action) :
	//			action is Action.ClearComponentOfType clear ? (clear.Type, action) :
	//			(null, null))
	//		.Where(pair => pair.Item1 != null && pair.Item2 != null)
	//		.GroupBy(pair => pair.Item1)
	//		.ToDictionary(group => group.Key, group => group.Select(pair => pair.Item2).ToArray());

	//	public IEnumerable<(Entity, Type)> SuccessfullyAddedTags =>
	//		_actions.OfType<Action.AddTag>().Where(action => action.Success).Select(action => (action.Entity, action.Type));
	//	public IEnumerable<(Entity, Type)> UnsuccessfullyAddedTags =>
	//		_actions.OfType<Action.AddTag>().Where(action => !action.Success).Select(action => (action.Entity, action.Type));
	//	public IEnumerable<(Entity, Type)> SuccessfullyRemovedTags =>
	//		_actions.OfType<Action.RemoveTag>().Where(action => action.Success).Select(action => (action.Entity, action.Type));
	//	public IEnumerable<(Entity, Type)> UnsuccessfullyRemovedTags =>
	//		_actions.OfType<Action.RemoveTag>().Where(action => !action.Success).Select(action => (action.Entity, action.Type));
	//	public IDictionary<Entity, Type[]> Tags
	//	{
	//		get
	//		{
	//			var tags = new Dictionary<Entity, List<Type>>();
	//			foreach (var action in _actions)
	//			{
	//				switch (action)
	//				{
	//					case Action.CreateEntity create: tags.Add(create.Entity, new List<Type>()); break;
	//					case Action.DestroyEntity destroy: tags.Remove(destroy.Entity); break;
	//					case Action.ClearEntities clearEntities: tags.Clear(); break;
	//					case Action.AddTag add: if (add.Success) tags[add.Entity].Add(add.Type); break;
	//					case Action.RemoveTag remove: if (remove.Success) tags[remove.Entity].Remove(remove.Type); break;
	//					case Action.ClearTagOfType clearOfType:
	//					{
	//						foreach (var list in tags.Values)
	//							list.RemoveAll(type => type == clearOfType.Type);
	//						break;
	//					}
	//				}
	//			}
	//			return tags.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
	//		}
	//	}

	//	public IEnumerable<IMessage> Messages => _actions.OfType<Action.ReceiveMessage>().Select(action => action.Message);
	//	public IEnumerable<OnCreate> OnCreateMessages => Messages.OfType<OnCreate>();
	//	public IEnumerable<OnDestroy> OnDestroyMessages => Messages.OfType<OnDestroy>();
	//	public IEnumerable<OnAdd> OnAddMessages => Messages.OfType<OnAdd>();
	//	public IEnumerable<OnRemove> OnRemoveMessages => Messages.OfType<OnRemove>();

	//	readonly List<IAction> _actions = new List<IAction>();

	//	public void Record(IAction action) => _actions.Add(action);


	//	public override string ToString()
	//	{
	//		string FormatField(object instance, FieldInfo field) => $"{field.Name}: {field.GetValue(instance)}";
	//		string FormatFields(object instance) => string.Join(", ", instance.GetType().GetFields().Select(field => FormatField(instance, field)));
	//		string FormatAction(IAction action) => $"{action.GetType().Name} {{{FormatFields(action)}}}";
	//		return string.Join(Environment.NewLine, _actions.Select(action => "-> " + FormatAction(action)));
	//	}
	//}
}
