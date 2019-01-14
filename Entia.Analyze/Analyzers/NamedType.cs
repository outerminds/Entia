using Entia.Core;
using Entia.Injectables;
using Entia.Phases;
using Entia.Systems;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Entia.Analyze.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NamedType : DiagnosticAnalyzer
    {
        public static class Rules
        {
            public static readonly DiagnosticDescriptor MustBeStruct = new DiagnosticDescriptor(
                "Entia_" + nameof(MustBeStruct),
                nameof(MustBeStruct),
                $"Type '{{0}}' must be a struct.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor MustBeInstancePublicField = new DiagnosticDescriptor(
                "Entia_" + nameof(MustBeInstancePublicField),
                nameof(MustBeInstancePublicField),
                $"Type '{{0}}' can only hold instance public fields.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor FieldMustNotBeQueryable = new DiagnosticDescriptor(
                "Entia_" + nameof(FieldMustNotBeQueryable),
                nameof(FieldMustNotBeQueryable),
                $"Field '{{1}}' in type '{{0}}' must not store a value that implements '{typeof(Queryables.IQueryable).FullFormat()}'.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor FieldMustNotBeComponent = new DiagnosticDescriptor(
                "Entia_" + nameof(FieldMustNotBeComponent),
                nameof(FieldMustNotBeComponent),
                $"Field '{{1}}' in type '{{0}}' must not store a value that implements '{typeof(IComponent).FullFormat()}'.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor FieldMustNotBeResource = new DiagnosticDescriptor(
                "Entia_" + nameof(FieldMustNotBeResource),
                nameof(FieldMustNotBeResource),
                $"Field '{{1}}' in type '{{0}}' must not store a value that implements '{typeof(IResource).FullFormat()}'.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor FieldMustNotBeMessage = new DiagnosticDescriptor(
                "Entia_" + nameof(FieldMustNotBeMessage),
                nameof(FieldMustNotBeMessage),
                $"Field '{{1}}' in type '{{0}}' must not store a value that implements '{typeof(IMessage).FullFormat()}'.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor FieldMustNotBeSystem = new DiagnosticDescriptor(
                "Entia_" + nameof(FieldMustNotBeSystem),
                nameof(FieldMustNotBeSystem),
                $"Field '{{1}}' in type '{{0}}' must not store a value that implements '{typeof(ISystem).FullFormat()}'.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor FieldMustNotBePhase = new DiagnosticDescriptor(
                "Entia_" + nameof(FieldMustNotBePhase),
                nameof(FieldMustNotBePhase),
                $"Field '{{1}}' in type '{{0}}' must not store a value that implements '{typeof(IPhase).FullFormat()}'.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor MustImplementOnlyOneEntiaInterface = new DiagnosticDescriptor(
                "Entia_" + nameof(MustImplementOnlyOneEntiaInterface),
                nameof(MustImplementOnlyOneEntiaInterface),
                $"Type '{{0}}' can implement at most one of '{typeof(ISystem).FullFormat()}, {typeof(IComponent).FullFormat()}, {typeof(IMessage).FullFormat()}, {typeof(IResource).FullFormat()}, {typeof(IPhase).FullFormat()}'.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor SystemPublicFieldMustBeInjectable = new DiagnosticDescriptor(
                "Entia_" + nameof(SystemPublicFieldMustBeInjectable),
                nameof(SystemPublicFieldMustBeInjectable),
                $"Public field '{{1}}' in system '{{0}}' must implement '{typeof(IInjectable).FullFormat()}'.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor SystemNotPublicFieldWillNotBeInjected = new DiagnosticDescriptor(
                "Entia_" + nameof(SystemNotPublicFieldWillNotBeInjected),
                nameof(SystemNotPublicFieldWillNotBeInjected),
                $"Field '{{1}}' in type '{{0}}' will not be injected even though it implements '{typeof(IInjectable).FullFormat()}' because it is not public.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);

            public static readonly DiagnosticDescriptor MessagePrefix = new DiagnosticDescriptor(
                "Entia_" + nameof(MessagePrefix),
                nameof(MessagePrefix),
                $"Message '{{0}}' should be prefixed with 'On' or 'Do'.",
                nameof(Entia),
                DiagnosticSeverity.Warning,
                true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            Rules.MustBeStruct,
            Rules.FieldMustNotBeQueryable,
            Rules.FieldMustNotBeComponent,
            Rules.FieldMustNotBeMessage,
            Rules.FieldMustNotBeResource,
            Rules.FieldMustNotBeSystem,
            Rules.FieldMustNotBePhase,
            Rules.MustBeInstancePublicField,
            Rules.MustImplementOnlyOneEntiaInterface,
            Rules.SystemPublicFieldMustBeInjectable,
            Rules.SystemNotPublicFieldWillNotBeInjected,
            Rules.MessagePrefix
        );

        public override void Initialize(AnalysisContext context) => context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

        static void Analyze(SymbolAnalysisContext context)
        {
            if (context.Symbol is INamedTypeSymbol symbol)
            {
                void ReportType(DiagnosticDescriptor rule) => context.ReportDiagnostic(Diagnostic.Create(rule, symbol.Locations[0], symbol.Name));
                void ReportMember(DiagnosticDescriptor rule, ISymbol member) => context.ReportDiagnostic(Diagnostic.Create(rule, member.Locations[0], symbol.Name, member.Name));

                var members = symbol.Members().ToArray();
                var fields = symbol.Fields().ToArray();
                var global = context.Compilation.GlobalNamespace;
                var symbols = new Symbols(global);

                var isSystem = symbol.Implements(symbols.System);
                var isPhase = symbol.Implements(symbols.Phase);
                var isComponent = symbol.Implements(symbols.Component);
                var isResource = symbol.Implements(symbols.Resource);
                var isMessage = symbol.Implements(symbols.Message);

                if (isSystem.GetHashCode() +
                    isComponent.GetHashCode() +
                    isMessage.GetHashCode() +
                    isResource.GetHashCode() +
                    isPhase.GetHashCode() > 1)
                    context.ReportDiagnostic(Diagnostic.Create(Rules.MustImplementOnlyOneEntiaInterface, symbol.Locations[0], symbol.Name));

                if (isSystem || isComponent || isResource || isMessage || isPhase)
                {
                    if (symbol.TypeKind == TypeKind.Class) ReportType(Rules.MustBeStruct);
                    foreach (var field in fields)
                    {
                        if (field.Type != symbols.Entity && field.Type.Implements(symbols.Queryable)) ReportMember(Rules.FieldMustNotBeQueryable, field);
                        if (field.Type.Implements(symbols.System)) ReportMember(Rules.FieldMustNotBeSystem, field);
                    }
                }

                if (isComponent || isResource || isMessage || isPhase)
                {
                    foreach (var member in members.Where(member => !member.Is<INamedTypeSymbol>()))
                    {
                        if (member.IsStatic)
                            ReportMember(Rules.MustBeInstancePublicField, member);
                        else if (!member.Is<IFieldSymbol>() && !member.IsImplicitlyDeclared)
                            ReportMember(Rules.MustBeInstancePublicField, member);

                        if (member is IFieldSymbol field)
                        {
                            if (field.DeclaredAccessibility != Accessibility.Public) ReportMember(Rules.MustBeInstancePublicField, field);
                            if (field.Type.Implements(symbols.Component)) ReportMember(Rules.FieldMustNotBeComponent, field);
                            if (field.Type.Implements(symbols.Message)) ReportMember(Rules.FieldMustNotBeMessage, field);
                            if (field.Type.Implements(symbols.Resource)) ReportMember(Rules.FieldMustNotBeResource, field);
                            if (field.Type.Implements(symbols.Phase)) ReportMember(Rules.FieldMustNotBePhase, field);
                        }
                    }
                }

                if (isMessage)
                {
                    if (!symbol.Name.StartsWith("On", System.StringComparison.CurrentCulture) &&
                        !symbol.Name.StartsWith("Do", System.StringComparison.CurrentCulture))
                        ReportType(Rules.MessagePrefix);
                }

                if (isSystem)
                {
                    foreach (var field in fields)
                    {
                        var isInjectable = field.Type.Implements(symbols.Injectable);

                        if (field.DeclaredAccessibility == Accessibility.Public && !isInjectable)
                            ReportMember(Rules.SystemPublicFieldMustBeInjectable, field);

                        if (field.DeclaredAccessibility != Accessibility.Public && isInjectable)
                            ReportMember(Rules.SystemNotPublicFieldWillNotBeInjected, field);
                    }
                }
            }
        }
    }
}
