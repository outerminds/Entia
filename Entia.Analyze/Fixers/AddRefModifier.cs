using Entia.Analyze.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace Entia.Analyze.Fixers
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddRefModifier)), Shared]
	public sealed class AddRefModifier : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
			Variable.Rules.MissingVariableRef.Id,
			ForEach.Rules.MissingForeachRef.Id,
			Parameter.Rules.MissingParameterRefOrIn.Id);
		public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		public override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			Solution FixParameter(Document document, SyntaxNode root, ParameterSyntax declaration)
			{
				var replaced = root.ReplaceNode(declaration, declaration.ToRef());
				var solution = document.Project.Solution;
				return solution.WithDocumentSyntaxRoot(document.Id, replaced);
			}

			Solution FixVariable(Document document, SyntaxNode root, VariableDeclarationSyntax declaration)
			{
				var replaced = root.ReplaceNode(declaration, declaration.ToRef());
				var solution = document.Project.Solution;
				return solution.WithDocumentSyntaxRoot(document.Id, replaced);
			}

			Solution FixForEach(Document document, SyntaxNode root, ForEachStatementSyntax statement)
			{
				var replaced = root.ReplaceNode(statement.Type, statement.Type.ToRef());
				var solution = document.Project.Solution;
				return solution.WithDocumentSyntaxRoot(document.Id, replaced);
			}

			await Task.WhenAll(
				context.RegisterCodeAction<VariableDeclarationSyntax>("Add 'ref' modifier.", Variable.Rules.MissingVariableRef.Id, FixVariable),
				context.RegisterCodeAction<ForEachStatementSyntax>("Add 'ref' modifier.", ForEach.Rules.MissingForeachRef.Id, FixForEach),
				context.RegisterCodeAction<ParameterSyntax>("Add 'ref' modifier.", Parameter.Rules.MissingParameterRefOrIn.Id, FixParameter));
		}
	}
}
