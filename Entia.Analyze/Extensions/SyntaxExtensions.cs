using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace Entia.Analyze
{
	public static class SyntaxExtensions
	{
		public static bool IsSome(this SyntaxToken token) => !token.IsNone();
		public static bool IsNone(this SyntaxToken token) => token == default;
		public static SyntaxToken ToToken(this SyntaxKind kind) => SyntaxFactory.Token(kind);
		public static ForEachStatementSyntax ToRef(this ForEachStatementSyntax statement, bool @readonly = false) => statement.WithType(statement.Type.ToRef(@readonly));
		public static RefExpressionSyntax ToRef(this ExpressionSyntax expression) => expression is RefExpressionSyntax @ref ? @ref : SyntaxFactory.RefExpression(expression).WithTriviaFrom(expression);

		public static VariableDeclarationSyntax ToRef(this VariableDeclarationSyntax declaration, bool @readonly = false) =>
			declaration
				.WithType(declaration.Type.ToRef(@readonly))
				.WithVariables(declaration.Variables
					.Where(variable => variable.Initializer is EqualsValueClauseSyntax)
					.Select(variable =>
					{
						var initializer = variable.Initializer.WithValue(variable.Initializer.Value.ToRef());
						return (old: variable, @new: variable.WithInitializer(initializer));
					})
					.Aggregate(declaration.Variables, (variables, pair) => variables.Replace(pair.old, pair.@new)));

		public static ParameterSyntax ToRef(this ParameterSyntax declaration) =>
			declaration.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.RefKeyword) ? declaration :
			declaration.AddModifiers(SyntaxKind.RefKeyword.ToToken());

		public static ParameterSyntax ToIn(this ParameterSyntax declaration) =>
			declaration.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.InKeyword) ? declaration :
			declaration.AddModifiers(SyntaxKind.InKeyword.ToToken());

		public static ParameterSyntax ToOut(this ParameterSyntax declaration) =>
			declaration.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.OutKeyword) ? declaration :
			declaration.AddModifiers(SyntaxKind.OutKeyword.ToToken());

		public static RefTypeSyntax ToRef(this TypeSyntax type, bool @readonly = false)
		{
			var token = @readonly ? SyntaxKind.ReadOnlyKeyword.ToToken() : default;
			if (type is RefTypeSyntax @ref)
			{
				if (@ref.ReadOnlyKeyword.IsSome() == @readonly) return @ref;
				return @ref.WithReadOnlyKeyword(token);
			}

			return SyntaxFactory.RefType(SyntaxKind.RefKeyword.ToToken(), token, type).WithTriviaFrom(type);
		}
	}
}
