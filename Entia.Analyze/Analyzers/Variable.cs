using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Entia.Analyze.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class Variable : DiagnosticAnalyzer
    {
        public static class Rules
        {
            public static readonly DiagnosticDescriptor MissingVariableRef = new DiagnosticDescriptor(
                "Entia_" + nameof(MissingVariableRef),
                nameof(MissingVariableRef),
                $"Variable '{{0}}' should have the 'ref' modifier.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor MissingVariableRefReadonly = new DiagnosticDescriptor(
                "Entia_" + nameof(MissingVariableRefReadonly),
                nameof(MissingVariableRefReadonly),
                $"Variable '{{0}}' should have the 'ref readonly' modifier.",
                nameof(Entia),
                DiagnosticSeverity.Info,
                true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rules.MissingVariableRef,
            Rules.MissingVariableRefReadonly
        );

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.VariableDeclarator);

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is VariableDeclaratorSyntax variable &&
                variable.Initializer?.Value is ExpressionSyntax expression &&
                context.SemanticModel.GetSymbolInfo(expression).Symbol is ISymbol symbol)
            {
                var (type, @ref, @readonly) =
                    symbol is IPropertySymbol property ? (property.Type, property.ReturnsByRef, property.ReturnsByRefReadonly) :
                    symbol is IMethodSymbol method ? (method.ReturnType, method.ReturnsByRef, method.ReturnsByRefReadonly) :
                    default;
                if (type == null || type.IsReferenceType) return;

                var rule = @ref ? Rules.MissingVariableRef : @readonly ? Rules.MissingVariableRefReadonly : default;
                if (rule == null) return;

                var global = context.Compilation.GlobalNamespace;
                var symbols = new Symbols(global);

                if (type.Implements(symbols.Component) ||
                    type.Implements(symbols.Resource) ||
                    type.Implements(symbols.Message) ||
                    type.Implements(symbols.Injectable) ||
                    type.Implements(symbols.Queryable) ||
                    type.Implements(symbols.Phase))
                    context.ReportDiagnostic(Diagnostic.Create(rule, context.Node.GetLocation(), variable.Identifier.Value));
            }
        }
    }
}
