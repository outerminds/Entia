using Entia.Initializers;
using Entia.Instantiators;

namespace Entia.Modules.Template
{
	public readonly struct Element
	{
		public readonly int Reference;
		public readonly IInitializer Initializer;
		public readonly IInstantiator Instantiator;
		public Element(int reference, (IInitializer initializer, IInstantiator instantiator) pair) : this(reference, pair.initializer, pair.instantiator) { }
		public Element(int reference, IInitializer initializer, IInstantiator instantiator)
		{
			Reference = reference;
			Initializer = initializer;
			Instantiator = instantiator;
		}
	}
}
