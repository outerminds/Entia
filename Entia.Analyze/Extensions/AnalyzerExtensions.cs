namespace Entia.Analyze
{
	public static class AnalyzerExtensions
	{
		public static string ToIdentifier(this string value) => $"Entia_{value}";
	}
}
