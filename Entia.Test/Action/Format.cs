namespace Entia.Test
{
	public interface ISized
	{
		int Size { get; }
	}

	public interface IFormatted
	{
		string Format(Format.Types format);
	}

	public static class Format
	{
		public enum Types { Summary, Detailed }

		public sealed class Summary
		{
			readonly IFormatted _formatted;

			public Summary(IFormatted formatted) { _formatted = formatted; }
			public override string ToString() => _formatted.Format(Types.Summary);
		}

		public sealed class Detailed
		{
			readonly IFormatted _formatted;

			public Detailed(IFormatted formatted) { _formatted = formatted; }
			public override string ToString() => _formatted.Format(Types.Detailed);
		}

		public static object Wrap(IFormatted formatted, Types type)
		{
			switch (type)
			{
				case Types.Summary: return new Summary(formatted);
				case Types.Detailed: return new Detailed(formatted);
			}

			return formatted;
		}
	}
}
