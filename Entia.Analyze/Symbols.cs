using Entia.Core;
using Entia.Injectables;
using Entia.Phases;
using Entia.Queryables;
using Entia.Systems;
using Microsoft.CodeAnalysis;
using System;

namespace Entia.Analyze
{
	public readonly struct Symbols
	{
		public INamedTypeSymbol Entity => _entity.Value;
		readonly Lazy<INamedTypeSymbol> _entity;

		public INamedTypeSymbol World => _world.Value;
		readonly Lazy<INamedTypeSymbol> _world;

		public INamedTypeSymbol System => _system.Value;
		readonly Lazy<INamedTypeSymbol> _system;

		public INamedTypeSymbol Resource => _resource.Value;
		readonly Lazy<INamedTypeSymbol> _resource;

		public INamedTypeSymbol Component => _component.Value;
		readonly Lazy<INamedTypeSymbol> _component;

		public INamedTypeSymbol Message => _message.Value;
		readonly Lazy<INamedTypeSymbol> _message;

		public INamedTypeSymbol Phase => _phase.Value;
		readonly Lazy<INamedTypeSymbol> _phase;

		public INamedTypeSymbol Injectable => _injectable.Value;
		readonly Lazy<INamedTypeSymbol> _injectable;

		public INamedTypeSymbol Queryable => _queryable.Value;
		readonly Lazy<INamedTypeSymbol> _queryable;

		public INamedTypeSymbol Default => _default.Value;
		readonly Lazy<INamedTypeSymbol> _default;

		public Symbols(INamespaceSymbol global)
		{
			_entity = new Lazy<INamedTypeSymbol>(() => global.Type<Entity>());
			_world = new Lazy<INamedTypeSymbol>(() => global.Type<World>());
			_system = new Lazy<INamedTypeSymbol>(() => global.Type<ISystem>());
			_resource = new Lazy<INamedTypeSymbol>(() => global.Type<IResource>());
			_component = new Lazy<INamedTypeSymbol>(() => global.Type<IComponent>());
			_message = new Lazy<INamedTypeSymbol>(() => global.Type<IMessage>());
			_phase = new Lazy<INamedTypeSymbol>(() => global.Type<IPhase>());
			_injectable = new Lazy<INamedTypeSymbol>(() => global.Type<IInjectable>());
			_queryable = new Lazy<INamedTypeSymbol>(() => global.Type<IQueryable>());
			_default = new Lazy<INamedTypeSymbol>(() => global.Type<DefaultAttribute>());
		}
	}
}
