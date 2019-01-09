using Entia.Analyze.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Entia.Analyze.Fixers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToStruct)), Shared]
	public sealed class ConvertToStruct : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NamedType.Rules.MustBeStruct.Id);
		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			async Task<Solution> FixType(Document document, SyntaxNode _, TypeDeclarationSyntax declaration, CancellationToken token)
			{
				var solution = document.Project.Solution;
				var text = await document.GetTextAsync(token);
				var replaced = text.WithChanges(new TextChange(declaration.Keyword.Span, "struct"));
				return solution.WithDocumentText(document.Id, replaced);
			}

			await context.RegisterCodeAction<TypeDeclarationSyntax>("Convert to struct.", NamedType.Rules.MustBeStruct.Id, FixType);
		}
	}
}
