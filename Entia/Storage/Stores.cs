using Entia.Core;
using Entia.Stores;
using Entia.Stores.Factories;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Entia.Modules
{
	public sealed class Stores : IModule, IEnumerable<IStore>
	{
		readonly World _world;
		readonly TypeMap<IComponent, IFactory> _factories = new TypeMap<IComponent, IFactory>();
		readonly TypeMap<IComponent, IFactory> _defaults = new TypeMap<IComponent, IFactory>();
		readonly Dictionary<(Type component, Type segment), IStore> _stores = new Dictionary<(Type component, Type segment), IStore>();

		public Stores(World world) { _world = world; }

		public IStore Store(Type component, Type segment)
		{
			if (TryStore(component, segment, out var store)) return store;
			return _stores[(component, segment)] = Get(component).Create(segment, _world);
		}

		public Store<T> Store<T>(Type segment) where T : struct, IComponent
		{
			if (TryStore<T>(segment, out var store)) return store;
			_stores[(typeof(T), segment)] = store = Get<T>().Create(segment, _world);
			return store;
		}

		public bool TryStore(Type component, Type segment, out IStore store) => _stores.TryGetValue((component, segment), out store);

		public bool TryStore<T>(Type segment, out Store<T> store) where T : struct, IComponent
		{
			if (_stores.TryGetValue((typeof(T), segment), out var value) && value is Store<T> casted)
			{
				store = casted;
				return store != null;
			}

			store = default;
			return false;
		}

		public Factory<T> Default<T>() where T : struct, IComponent =>
			_defaults.TryGet<T>(out var factory) && factory is Factory<T> casted ? casted :
			_defaults.Default(typeof(T), typeof(IStorable<>), typeof(FactoryAttribute), () => new Entia.Stores.Factories.Default<T>()) as Factory<T>;
		public IFactory Default(Type component) =>
			_defaults.Default(component, typeof(IStorable<>), typeof(FactoryAttribute), typeof(Entia.Stores.Factories.Default<>));
		public Factory<T> Get<T>() where T : struct, IComponent =>
			_factories.TryGet<T>(out var factory, true) && factory is Factory<T> casted ? casted : Default<T>();
		public IFactory Get(Type component) =>
			_factories.TryGet(component, out var factory, true) ? factory : Default(component);

		public bool Set<T>(Factory<T> factory) where T : struct, IComponent => _factories.Set<T>(factory);
		public bool Set(Type component, IFactory factory) => _factories.Set(component, factory);
		public bool Has<T>() where T : struct, IComponent => _factories.Has<T>(true);
		public bool Has(Type component) => _factories.Has(component, true);
		public bool Remove<T>() where T : struct, IComponent => _factories.Remove<T>();
		public bool Remove(Type component) => _factories.Remove(component);
		public bool Clear()
		{
			var cleared = _factories.Clear() | _defaults.Clear() | _stores.Count > 0;
			_stores.Clear();
			return cleared;
		}

		public IEnumerator<IStore> GetEnumerator() => _stores.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
