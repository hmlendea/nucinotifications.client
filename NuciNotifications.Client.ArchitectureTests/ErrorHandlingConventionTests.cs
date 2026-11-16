using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace NuciNotifications.Client.ArchitectureTests
{
    [TestFixture]
    public sealed class ErrorHandlingConventionTests
    {
        private static IEnumerable<string> Sources =>
            ArchitectureResources.GetResourcePaths(ArchitectureResources.ProductionSourcePrefix);

        [Test]
        public void GivenProductionSources_WhenCheckingNullChecks_ThenPatternMatchingIsUsed()
        {
            Assert.That(Sources, Is.Not.Empty, "No production source files were found.");

            foreach (string source in Sources)
            {
                string[] equalityNullChecks = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<BinaryExpressionSyntax>()
                    .Where(expression => expression.IsKind(SyntaxKind.EqualsExpression) ||
                        expression.IsKind(SyntaxKind.NotEqualsExpression))
                    .Where(expression => expression.Right.IsKind(SyntaxKind.NullLiteralExpression) ||
                        expression.Left.IsKind(SyntaxKind.NullLiteralExpression))
                    .Select(expression => expression.ToString())
                    .ToArray();

                Assert.That(
                    equalityNullChecks,
                    Is.Empty,
                    $"{source}: use 'is null'/'is not null' instead of {string.Join(", ", equalityNullChecks)}.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingCoalescing_ThenTheCoalesceOperatorIsAbsent()
        {
            foreach (string source in Sources)
            {
                string[] coalescing = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<BinaryExpressionSyntax>()
                    .Where(expression => expression.IsKind(SyntaxKind.CoalesceExpression))
                    .Select(expression => expression.ToString())
                    .ToArray();

                Assert.That(
                    coalescing,
                    Is.Empty,
                    $"{source}: the null-coalescing operator '??' is not allowed: {string.Join(", ", coalescing)}.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingRethrows_ThenTheCaughtExceptionIsNotRewrapped()
        {
            foreach (string source in Sources)
            {
                SyntaxNode root = ParseTree(source).GetRoot();

                string[] rewrappedRethrows = root.DescendantNodes()
                    .OfType<CatchClauseSyntax>()
                    .Where(clause => clause.Declaration is not null)
                    .SelectMany(clause => clause.Block.DescendantNodes().OfType<ThrowStatementSyntax>()
                        .Where(statement => statement.Expression is IdentifierNameSyntax identifier &&
                            string.Equals(
                                identifier.Identifier.ValueText,
                                clause.Declaration!.Identifier.ValueText,
                                StringComparison.Ordinal)))
                    .Select(statement => statement.ToString())
                    .ToArray();

                Assert.That(
                    rewrappedRethrows,
                    Is.Empty,
                    $"{source}: rethrow with a bare 'throw;' instead of {string.Join(", ", rewrappedRethrows)}.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingCatchBlocks_ThenNoneAreEmpty()
        {
            foreach (string source in Sources)
            {
                CatchClauseSyntax[] emptyCatches = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<CatchClauseSyntax>()
                    .Where(clause => clause.Block.Statements.Count == 0)
                    .ToArray();

                Assert.That(emptyCatches, Is.Empty, $"{source}: empty catch blocks are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingStandardExceptions_ThenOnlyBclTypesAreThrown()
        {
            string[] bclExceptions =
            [
                "ArgumentNullException",
                "ArgumentException",
                "ArgumentOutOfRangeException",
                "InvalidOperationException",
                "NotSupportedException",
                "KeyNotFoundException",
                "AuthenticationException",
                "SmtpException",
                "ObjectDisposedException",
                "FormatException"
            ];

            foreach (string source in Sources)
            {
                string[] thrownTypes = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ThrowStatementSyntax>()
                    .Where(statement => statement.Expression is ObjectCreationExpressionSyntax)
                    .Select(statement => (ObjectCreationExpressionSyntax)statement.Expression!)
                    .Select(creation => creation.Type.ToString())
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                Assert.That(
                    thrownTypes.Where(type => bclExceptions.Contains(type, StringComparer.Ordinal) is false),
                    Is.Empty,
                    $"{source}: only BCL exceptions may be thrown.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingCasting_ThenSafeCastsPrecedeDirectCasts()
        {
            foreach (string source in Sources)
            {
                string[] casts = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<CastExpressionSyntax>()
                    .Select(cast => cast.ToString())
                    .ToArray();

                Assert.That(
                    casts,
                    Is.Empty,
                    $"{source}: prefer the 'as' operator over direct casts: {string.Join(", ", casts)}.");
            }
        }

        private static SyntaxTree ParseTree(string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProductionSourcePrefix,
                    source),
                path: source);
    }
}