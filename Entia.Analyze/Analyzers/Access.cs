using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Entia.Analyze.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class Access : DiagnosticAnalyzer
	{
		public static class Rules
		{
			public static readonly DiagnosticDescriptor SystemMustNotAccessSystem = new DiagnosticDescriptor(
				"Entia_" + nameof(SystemMustNotAccessSystem),
				nameof(SystemMustNotAccessSystem),
				$"System '{{0}}' must not access another system. Use an utility class to share logic instead.",
				nameof(Entia),
				DiagnosticSeverity.Warning,
				true);
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rules.SystemMustNotAccessSystem);

		public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyze,
			SyntaxKind.ConditionalAccessExpression,
			SyntaxKind.ElementAccessExpression,
			SyntaxKind.PointerMemberAccessExpression,
			SyntaxKind.SimpleMemberAccessExpression,
			SyntaxKind.InvocationExpression);

		static void Analyze(SyntaxNodeAnalysisContext context)
		{
			var global = context.Compilation.GlobalNamespace;
			var symbols = new Symbols(global);
			var expression =
				context.Node is ConditionalAccessExpressionSyntax conditional ? conditional.Expression :
				context.Node is ElementAccessExpressionSyntax element ? element.Expression :
				context.Node is MemberAccessExpressionSyntax access ? access.Expression :
				context.Node is InvocationExpressionSyntax invocation ? invocation.Expression :
				default;

			if (expression is ExpressionSyntax &&
				context.SemanticModel.GetTypeInfo(expression).Type is INamedTypeSymbol symbol &&
				context.Node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() is TypeDeclarationSyntax declaration &&
				context.SemanticModel.GetDeclaredSymbol(declaration) is INamedTypeSymbol type &&
				type != symbol &&
				type.Implements(symbols.System) &&
				symbol.Implements(symbols.System))
				context.ReportDiagnostic(Diagnostic.Create(Rules.SystemMustNotAccessSystem, context.Node.GetLocation(), type.Name));
		}
	}
}
