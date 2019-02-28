using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Entia.Analyze.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Parameter : DiagnosticAnalyzer
	{
		public static class Rules
		{
			public static readonly DiagnosticDescriptor MissingParameterRefOrIn = new DiagnosticDescriptor(
				"Entia_" + nameof(MissingParameterRefOrIn),
				nameof(MissingParameterRefOrIn),
				$"Parameter '{{0}}' should have the 'ref' or 'in' modifier.",
				nameof(Entia),
				DiagnosticSeverity.Warning,
				true);
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rules.MissingParameterRefOrIn);

		public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Parameter);

		static void Analyze(SyntaxNodeAnalysisContext context)
		{
			if (context.Node is ParameterSyntax parameter &&
				context.SemanticModel.GetDeclaredSymbol(parameter) is IParameterSymbol symbol &&
				!symbol.IsOptional &&
				!symbol.HasExplicitDefaultValue &&
				symbol.Type.IsValueType &&
				symbol.RefKind == RefKind.None)
			{
				var global = context.Compilation.GlobalNamespace;
				var symbols = new Symbols(global);

				if (symbol.Type.Implements(symbols.Component) ||
					symbol.Type.Implements(symbols.Resource) ||
					symbol.Type.Implements(symbols.Message) ||
					symbol.Type.Implements(symbols.Phase))
					context.ReportDiagnostic(Diagnostic.Create(Rules.MissingParameterRefOrIn, context.Node.GetLocation(), parameter.Identifier.Value));
			}
		}
	}
}
