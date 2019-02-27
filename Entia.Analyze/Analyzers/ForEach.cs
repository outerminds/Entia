using Entia.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;

namespace Entia.Analyze.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ForEach : DiagnosticAnalyzer
    {
        public static class Rules
        {
            public static readonly DiagnosticDescriptor MissingForeachRef = new DiagnosticDescriptor(
                "Entia_" + nameof(MissingForeachRef),
                nameof(MissingForeachRef),
                $"Variable '{{0}}' should have the 'ref' modifier.",
                nameof(Entia),
                DiagnosticSeverity.Info,
                true);

            public static readonly DiagnosticDescriptor MissingForeachRefReadonly = new DiagnosticDescriptor(
                "Entia_" + nameof(MissingForeachRefReadonly),
                nameof(MissingForeachRefReadonly),
                $"Variable '{{0}}' should have the 'ref readonly' modifier.",
                nameof(Entia),
                DiagnosticSeverity.Info,
                true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rules.MissingForeachRef,
            Rules.MissingForeachRefReadonly
        );

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ForEachStatement);

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var global = context.Compilation.GlobalNamespace;
            var symbols = new Symbols(global);

            if (context.Node is ForEachStatementSyntax statement &&
                !statement.Type.Is<RefTypeSyntax>() &&
                statement.Expression is ExpressionSyntax expression &&
                context.SemanticModel.GetTypeInfo(expression).Type is INamedTypeSymbol symbol &&
                symbol.IsValueType &&
                symbol.GetMembers(nameof(IEnumerable.GetEnumerator))
                    .OfType<IMethodSymbol>()
                    .SelectMany(getEnumerator => getEnumerator.ReturnType.GetMembers(nameof(IEnumerator.Current)))
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(current => current.ReturnsByRef || current.ReturnsByRefReadonly) is IPropertySymbol property)
            {
                var rule = property.IsReadOnly ? Rules.MissingForeachRefReadonly : Rules.MissingForeachRef;
                var locations = new[] { expression.GetLocation(), statement.Identifier.GetLocation(), statement.InKeyword.GetLocation() };
                context.ReportDiagnostic(Diagnostic.Create(rule, statement.Type.GetLocation(), locations, statement.Identifier.Value));
            }
        }
    }
}
