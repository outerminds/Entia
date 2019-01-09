using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Entia.Analyze
{
	public static class FixerExtensions
	{
		public static async Task RegisterCodeAction<T>(this CodeFixContext context, string name, string identifier, Func<Document, SyntaxNode, T, CancellationToken, Task<Solution>> action) where T : SyntaxNode
		{
			if (action == null) return;

			var diagnostic = context.Diagnostics.FirstOrDefault(current => string.IsNullOrWhiteSpace(identifier) || current.Id == identifier);
			if (diagnostic == null) return;

			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

			if (root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<T>().FirstOrDefault() is T node)
				context.RegisterCodeFix(CodeAction.Create(name, token => action(context.Document, root, node, token)), diagnostic);
		}

		public static async Task RegisterCodeAction<T>(this CodeFixContext context, string name, string identifier, Func<Document, SyntaxNode, T, Solution> action) where T : SyntaxNode =>
			await context.RegisterCodeAction<T>(name, identifier, (document, root, node, _) => Task.FromResult(action?.Invoke(document, root, node)));
	}
}
