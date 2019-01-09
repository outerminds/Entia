using FsCheck;

namespace Entia.Test
{
	public static class ArbitraryExtensions
	{
		public static Arbitrary<T> WithoutShrink<T>(this Arbitrary<T> arbitrary) => arbitrary.Generator.ToArbitrary();
	}
}
