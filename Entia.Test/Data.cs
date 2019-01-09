using System;

namespace Entia.Test
{
	public class Data
	{
		public readonly World World;
		public readonly Model Model;

		public Data(World world, Model model)
		{
			World = world;
			Model = model;
		}

		public override string ToString() => Model + Environment.NewLine;
	}
}
