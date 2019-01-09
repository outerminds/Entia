using Entia.Core;
using Entia.Injectables;
using Entia.Modules;
using Entia.Nodes;
using Entia.Queryables;
using Entia.Systems;
using System;
using static Entia.Nodes.Node;

namespace Entia.Experiment
{
	public static class Usage
	{
		public struct Boba : IRun, IInitialize
		{
			[All(typeof(IsDead))]
			public readonly Group<Read<Position>> Group;
			public readonly Emitter<OnMove> OnMove;

			public void Initialize() => Console.WriteLine(nameof(Boba) + nameof(Initialize));
			public void Run() => Console.WriteLine(nameof(Boba) + nameof(Run));
		}

		public struct Fett : IRun, IDispose
		{
			public readonly Components<Position> Positions;
			public readonly Reaction<OnMove> OnMove;

			public void Run() => Console.WriteLine(nameof(Fett) + nameof(Run));
			public void Dispose() => Console.WriteLine(nameof(Fett) + nameof(Dispose));
		}

		public struct Jango : IRun, IInitialize, IDispose
		{
			public readonly Tags<IsDead> IsDead;

			public void Initialize() => Console.WriteLine(nameof(Jango) + nameof(Initialize));
			public void Run() => Console.WriteLine(nameof(Jango) + nameof(Run));
			public void Dispose() => Console.WriteLine(nameof(Jango) + nameof(Dispose));
		}

		public static void Method(World world)
		{
			var node =
				Parallel("Main",
					System<Boba>(),
					System<Jango>())
				.Profile();

			var result = world.Controllers().Control(node).Do(controller =>
			{
				controller.Run(new Phases.Initialize());
				Console.WriteLine();
				for (var i = 0; i < 10; i++)
				{
					controller.Run(new Phases.Run());
					Console.WriteLine();
				}
				Console.WriteLine();
				controller.Run(new Phases.Dispose());
			});
		}
	}
}
