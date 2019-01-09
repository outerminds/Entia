using Entia.Analyze.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace Entia.Analyze.Fixers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddInModifier)), Shared]
	public sealed class AddInModifier : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Parameter.Rules.MissingParameterRefOrIn.Id);
		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			Solution FixParameter(Document document, SyntaxNode root, ParameterSyntax declaration)
			{
				var replaced = root.ReplaceNode(declaration, declaration.ToIn());
				var solution = document.Project.Solution;
				return solution.WithDocumentSyntaxRoot(document.Id, replaced);
			}

			await context.RegisterCodeAction<ParameterSyntax>("Add 'in' modifier.", Parameter.Rules.MissingParameterRefOrIn.Id, FixParameter);
		}
	}
}
