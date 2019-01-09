using Entia.Analyze.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace Entia.Analyze.Fixers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddRefReadonlyModifier)), Shared]
	public sealed class AddRefReadonlyModifier : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
			Variable.Rules.MissingVariableRefReadonly.Id,
			ForEach.Rules.MissingForeachRefReadonly.Id);
		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			Solution FixVariable(Document document, SyntaxNode root, VariableDeclarationSyntax declaration)
			{
				var replaced = root.ReplaceNode(declaration, declaration.ToRef(true));
				var solution = document.Project.Solution;
				return solution.WithDocumentSyntaxRoot(document.Id, replaced);
			}

			Solution FixForEach(Document document, SyntaxNode root, ForEachStatementSyntax statement)
			{
				var replaced = root.ReplaceNode(statement.Type, statement.Type.ToRef(true));
				var solution = document.Project.Solution;
				return solution.WithDocumentSyntaxRoot(document.Id, replaced);
			}

			await context.RegisterCodeAction<VariableDeclarationSyntax>("Add 'ref readonly ' modifier.", Variable.Rules.MissingVariableRefReadonly.Id, FixVariable);
			await context.RegisterCodeAction<ForEachStatementSyntax>("Add 'ref readonly ' modifier.", ForEach.Rules.MissingForeachRefReadonly.Id, FixForEach);
		}
	}
}
