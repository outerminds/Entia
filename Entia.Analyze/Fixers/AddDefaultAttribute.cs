using Entia.Analyze.Analyzers;
using Entia.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Entia.Analyze.Fixers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddDefaultAttribute)), Shared]
	public sealed class AddDefaultAttribute : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NamedType.Rules.MissingDefaultAttribute.Id);
		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			bool RequiresUsing(SemanticModel model, SyntaxNode root) =>
				model.Compilation.GlobalNamespace?.Namespace(nameof(Entia))?.Namespace(nameof(Core)) is INamespaceSymbol @namespace &&
				root.DescendantNodes().OfType<UsingDirectiveSyntax>().None(@using => model.GetSymbolInfo(@using.Name).Symbol == @namespace);

			UsingDirectiveSyntax CreateUsing() => SyntaxFactory.UsingDirective(
				SyntaxFactory.QualifiedName(
					SyntaxFactory.IdentifierName(nameof(Entia)),
					SyntaxFactory.IdentifierName(nameof(Core))));

			SyntaxList<AttributeListSyntax> AddAttribute(in SyntaxList<AttributeListSyntax> lists)
			{
				var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Default"));
				return lists.Count > 0 ?
					lists.Replace(lists[0], lists[0].AddAttributes(attribute)) :
					lists.Add(SyntaxFactory.AttributeList().AddAttributes(attribute));
			}

			async Task<Solution> FixField(Document document, CompilationUnitSyntax root, FieldDeclarationSyntax declaration, CancellationToken token)
			{
				var model = await document.GetSemanticModelAsync(token);
				var replaced = root.ReplaceNode(declaration, declaration.WithAttributeLists(AddAttribute(declaration.AttributeLists)));
				if (RequiresUsing(model, root)) replaced = replaced.AddUsings(CreateUsing());

				var solution = document.Project.Solution;
				return solution.WithDocumentSyntaxRoot(document.Id, replaced);
			}

			async Task<Solution> FixProperty(Document document, CompilationUnitSyntax root, PropertyDeclarationSyntax declaration, CancellationToken token)
			{
				var model = await document.GetSemanticModelAsync(token);
				var replaced = root.ReplaceNode(declaration, declaration.WithAttributeLists(AddAttribute(declaration.AttributeLists)));
				if (RequiresUsing(model, root)) replaced = replaced.AddUsings(CreateUsing());

				var solution = document.Project.Solution;
				return solution.WithDocumentSyntaxRoot(document.Id, replaced);
			}

			async Task<Solution> FixMethod(Document document, CompilationUnitSyntax root, MethodDeclarationSyntax declaration, CancellationToken token)
			{
				var model = await document.GetSemanticModelAsync(token);
				var replaced = root.ReplaceNode(declaration, declaration.WithAttributeLists(AddAttribute(declaration.AttributeLists)));
				if (RequiresUsing(model, root)) replaced = replaced.AddUsings(CreateUsing());

				var solution = document.Project.Solution;
				return solution.WithDocumentSyntaxRoot(document.Id, replaced);
			}

			await context.RegisterCodeAction<FieldDeclarationSyntax>("Add 'Default' attribute.", NamedType.Rules.MissingDefaultAttribute.Id, FixField);
			await context.RegisterCodeAction<PropertyDeclarationSyntax>("Add 'Default' attribute.", NamedType.Rules.MissingDefaultAttribute.Id, FixProperty);
			await context.RegisterCodeAction<MethodDeclarationSyntax>("Add 'Default' attribute.", NamedType.Rules.MissingDefaultAttribute.Id, FixMethod);
		}
	}
}
