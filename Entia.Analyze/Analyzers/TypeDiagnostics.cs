using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using Entia.Core.Documentation;
using System;
using Entia.Core;

namespace Entia.Analyze.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TypeDiagnostics : DiagnosticAnalyzer
    {
        public static class Rules
        {
            public static readonly DiagnosticDescriptor TypeDiagnostic = new DiagnosticDescriptor(
                "Entia_" + nameof(TypeDiagnosticAttribute), nameof(TypeDiagnosticAttribute), "{0}",
                nameof(Entia), DiagnosticSeverity.Warning, true);
        }

        sealed class SymbolAnalysis
        {
            public readonly ISymbol Symbol;
            public readonly ITypeSymbol Type;
            public readonly Filters Filters;

            public SymbolAnalysis(ISymbol symbol, ITypeSymbol type, Filters filters)
            {
                Symbol = symbol;
                Type = type;
                Filters = filters;
            }
        }

        sealed class AttributeAnalysis
        {
            public readonly AttributeData Attribute;
            public readonly string Message;
            public readonly Func<SymbolAnalysis, bool> Filter;
            public readonly Func<SymbolAnalysis, bool> Validate;

            public AttributeAnalysis(AttributeData attribute, string message, Func<SymbolAnalysis, bool> filter, Func<SymbolAnalysis, bool> validate)
            {
                Attribute = attribute;
                Message = message;
                Filter = filter;
                Validate = validate;
            }
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rules.TypeDiagnostic);

        public override void Initialize(AnalysisContext context) => context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

        static void Analyze(SymbolAnalysisContext context)
        {
            var global = context.Compilation.GlobalNamespace;
            var type = global.Type<TypeDiagnosticAttribute>();
            var symbol = Analyze(context.Symbol);

            foreach (var data in context.Symbol.AllAttributes().Where(data => data.AttributeClass.Equals(type)))
            {
                var attribute = Analyze(data);
                if (attribute.Filter(symbol) && !attribute.Validate(symbol))
                {
                    var warning = attribute.Message
                        .Replace("{symbol}", symbol.Symbol.Name)
                        .Replace("{type}", symbol.Type.Name);
                    context.ReportDiagnostic(Diagnostic.Create(Rules.TypeDiagnostic, context.Symbol.Locations[0], warning));
                }
            }
        }

        static AttributeAnalysis Analyze(AttributeData attribute)
        {
            static Filters? AsFilters(TypedConstant constant) => constant.IsNull ? null : constant.Value is int value ? (Filters?)value : null;
            static string AsString(TypedConstant constant) => constant.IsNull ? null : constant.Value as string;
            static string[] AsStrings(TypedConstant constant) =>
                constant.IsNull || constant.Values.IsDefault ? null : constant.Values.Select(AsString).Some().ToArray();
            static INamedTypeSymbol AsType(TypedConstant constant) => constant.IsNull ? null : constant.Value as INamedTypeSymbol;
            static INamedTypeSymbol[] AsTypes(TypedConstant constant) =>
                constant.IsNull || constant.Values.IsDefault ? null : constant.Values.Select(AsType).Some().ToArray();

            var message = attribute.ConstructorArguments.Select(AsString).FirstOrDefault() ?? "";
            var filter = new Func<SymbolAnalysis, bool>(_ => true);
            var validate = new Func<SymbolAnalysis, bool>(_ => true);

            foreach (var pair in attribute.NamedArguments)
            {
                var previous = (filter, validate);
                switch (pair.Key)
                {
                    case nameof(TypeDiagnosticAttribute.WithAllFilters) when AsFilters(pair.Value) is Filters filters:
                        filter = symbol => previous.filter(symbol) && symbol.Filters.HasAll(filters);
                        break;
                    case nameof(TypeDiagnosticAttribute.WithAnyFilters) when AsFilters(pair.Value) is Filters filters:
                        filter = symbol => previous.filter(symbol) && symbol.Filters.HasAny(filters);
                        break;
                    case nameof(TypeDiagnosticAttribute.WithNoneFilters) when AsFilters(pair.Value) is Filters filters:
                        filter = symbol => previous.filter(symbol) && symbol.Filters.HasNone(filters);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveAllFilters) when AsFilters(pair.Value) is Filters filters:
                        validate = symbol => previous.validate(symbol) && symbol.Filters.HasAll(filters);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveAnyFilters) when AsFilters(pair.Value) is Filters filters:
                        validate = symbol => previous.validate(symbol) && symbol.Filters.HasAny(filters);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveNoneFilters) when AsFilters(pair.Value) is Filters filters:
                        validate = symbol => previous.validate(symbol) && symbol.Filters.HasNone(filters);
                        break;

                    case nameof(TypeDiagnosticAttribute.WithAnyPrefixes) when AsStrings(pair.Value) is string[] prefixes:
                        filter = symbol => previous.filter(symbol) && prefixes.Any(symbol.Symbol.Name.StartsWith);
                        break;
                    case nameof(TypeDiagnosticAttribute.WithNonePrefixes) when AsStrings(pair.Value) is string[] prefixes:
                        filter = symbol => previous.filter(symbol) && prefixes.None(symbol.Symbol.Name.StartsWith);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveAnyPrefixes) when AsStrings(pair.Value) is string[] prefixes:
                        validate = symbol => previous.validate(symbol) && prefixes.Any(symbol.Symbol.Name.StartsWith);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveNonePrefixes) when AsStrings(pair.Value) is string[] prefixes:
                        validate = symbol => previous.validate(symbol) && prefixes.None(symbol.Symbol.Name.StartsWith);
                        break;

                    case nameof(TypeDiagnosticAttribute.WithAnySuffixes) when AsStrings(pair.Value) is string[] suffixes:
                        filter = symbol => previous.filter(symbol) && suffixes.Any(symbol.Symbol.Name.EndsWith);
                        break;
                    case nameof(TypeDiagnosticAttribute.WithNoneSuffixes) when AsStrings(pair.Value) is string[] suffixes:
                        filter = symbol => previous.filter(symbol) && suffixes.None(symbol.Symbol.Name.EndsWith);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveAnySuffixes) when AsStrings(pair.Value) is string[] suffixes:
                        validate = symbol => previous.validate(symbol) && suffixes.Any(symbol.Symbol.Name.EndsWith);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveNoneSuffixes) when AsStrings(pair.Value) is string[] suffixes:
                        validate = symbol => previous.validate(symbol) && suffixes.None(symbol.Symbol.Name.EndsWith);
                        break;

                    case nameof(TypeDiagnosticAttribute.WithAllImplementations) when AsTypes(pair.Value) is INamedTypeSymbol[] implementations:
                        filter = symbol => previous.filter(symbol) && implementations.All(symbol.Type.Implements);
                        break;
                    case nameof(TypeDiagnosticAttribute.WithAnyImplementations) when AsTypes(pair.Value) is INamedTypeSymbol[] implementations:
                        filter = symbol => previous.filter(symbol) && implementations.Any(symbol.Type.Implements);
                        break;
                    case nameof(TypeDiagnosticAttribute.WithNoneImplementations) when AsTypes(pair.Value) is INamedTypeSymbol[] implementations:
                        filter = symbol => previous.filter(symbol) && implementations.None(symbol.Type.Implements);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveAllImplementations) when AsTypes(pair.Value) is INamedTypeSymbol[] implementations:
                        validate = symbol => previous.validate(symbol) && implementations.All(symbol.Type.Implements);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveAnyImplementations) when AsTypes(pair.Value) is INamedTypeSymbol[] implementations:
                        validate = symbol => previous.validate(symbol) && implementations.Any(symbol.Type.Implements);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveNoneImplementations) when AsTypes(pair.Value) is INamedTypeSymbol[] implementations:
                        validate = symbol => previous.validate(symbol) && implementations.None(symbol.Type.Implements);
                        break;

                    case nameof(TypeDiagnosticAttribute.WithAllAttributes) when AsTypes(pair.Value) is INamedTypeSymbol[] attributes:
                        filter = symbol => previous.filter(symbol) && attributes.All(symbol.Symbol.IsDefined);
                        break;
                    case nameof(TypeDiagnosticAttribute.WithAnyAttributes) when AsTypes(pair.Value) is INamedTypeSymbol[] attributes:
                        filter = symbol => previous.filter(symbol) && attributes.Any(symbol.Symbol.IsDefined);
                        break;
                    case nameof(TypeDiagnosticAttribute.WithNoneAttributes) when AsTypes(pair.Value) is INamedTypeSymbol[] attributes:
                        filter = symbol => previous.filter(symbol) && attributes.None(symbol.Symbol.IsDefined);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveAllAttributes) when AsTypes(pair.Value) is INamedTypeSymbol[] attributes:
                        validate = symbol => previous.validate(symbol) && attributes.All(symbol.Symbol.IsDefined);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveAnyAttributes) when AsTypes(pair.Value) is INamedTypeSymbol[] attributes:
                        validate = symbol => previous.validate(symbol) && attributes.Any(symbol.Symbol.IsDefined);
                        break;
                    case nameof(TypeDiagnosticAttribute.HaveNoneAttributes) when AsTypes(pair.Value) is INamedTypeSymbol[] attributes:
                        validate = symbol => previous.validate(symbol) && attributes.None(symbol.Symbol.IsDefined);
                        break;
                }
            }

            return new AttributeAnalysis(attribute, message, filter, validate);
        }

        static SymbolAnalysis Analyze(ISymbol symbol)
        {
            var filters = Filters.None;
            var type = symbol as ITypeSymbol ?? symbol.ContainingType;

            switch (symbol)
            {
                case IFieldSymbol field:
                    type = field.Type;
                    filters |= Filters.Field;
                    if (field.IsReadOnly) filters |= Filters.ReadOnly;
                    break;
                case IPropertySymbol property:
                    type = property.Type;
                    filters |= Filters.Property;
                    if (property.IsReadOnly) filters |= Filters.ReadOnly;
                    if (property.ReturnsByRef || property.ReturnsByRefReadonly) filters |= Filters.Ref;
                    break;
                case IMethodSymbol method:
                    type = method.ReturnType;
                    filters |= method.MethodKind switch
                    {
                        MethodKind.Constructor => Filters.Constructor,
                        MethodKind.Destructor => Filters.Destructor,
                        _ => Filters.Method,
                    };
                    if (method.IsGenericMethod) filters |= Filters.Generic;
                    if (method.ReturnsByRef || method.ReturnsByRefReadonly) filters |= Filters.Ref;
                    break;
                case IEventSymbol @event: type = @event.Type; filters |= Filters.Event; break;
            }

            switch (type.TypeKind)
            {
                case TypeKind.Array: filters |= Filters.Class; break;
                case TypeKind.Class: filters |= Filters.Class; break;
                case TypeKind.Delegate: filters |= Filters.Delegate; break;
                case TypeKind.Enum: filters |= Filters.Enum; break;
                case TypeKind.Interface: filters |= Filters.Interface; break;
                case TypeKind.Struct: filters |= Filters.Struct; break;
            }

            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Private: filters |= Filters.Private; break;
                case Accessibility.ProtectedAndInternal: filters |= Filters.Protected | Filters.Internal; break;
                case Accessibility.Protected: filters |= Filters.Protected; break;
                case Accessibility.Internal: filters |= Filters.Internal; break;
                case Accessibility.Public: filters |= Filters.Public; break;
            }

            if (symbol.IsStatic) filters |= Filters.Static;
            if (type.IsSealed) filters |= Filters.Sealed;
            return new SymbolAnalysis(symbol, type, filters);
        }
    }
}
