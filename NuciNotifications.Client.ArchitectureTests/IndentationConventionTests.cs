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
    public sealed class IndentationConventionTests
    {
        private static IEnumerable<string> Sources =>
            ArchitectureResources.GetResourcePaths(ArchitectureResources.ProductionSourcePrefix);

        [Test]
        public void GivenProductionSources_WhenChecked_ThenTabsAreNotUsed()
        {
            Assert.That(Sources, Is.Not.Empty, "No production source files were found.");

            foreach (string source in Sources)
            {
                Assert.That(
                    ArchitectureResources.ReadResource(ArchitectureResources.ProductionSourcePrefix, source)
                        .Contains('\t', StringComparison.Ordinal),
                    Is.False,
                    $"{source}: tabs are not allowed; use four-space indentation.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenSingleClauseConditionsAreNotSplit()
        {
            foreach (string source in Sources)
            {
                IEnumerable<IfStatementSyntax> splitSingleClauseConditions = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<IfStatementSyntax>()
                    .Where(statement => statement.Condition is BinaryExpressionSyntax binary &&
                        (binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                         binary.IsKind(SyntaxKind.LogicalOrExpression)))
                    .Where(statement => statement.Condition.GetText().ToString()
                        .Replace("\r\n", "\n", StringComparison.Ordinal)
                        .Split('\n')
                        .Length > 1)
                    .Where(statement => GetConditionLineCount(statement) <= 72);

                Assert.That(
                    splitSingleClauseConditions,
                    Is.Empty,
                    $"{source}: conditions fitting within 72 characters must remain on one line.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenMultiClauseConditionsBreakEveryClause()
        {
            foreach (string source in Sources)
            {
                IEnumerable<IfStatementSyntax> inlineLongConditions = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<IfStatementSyntax>()
                    .Where(statement => statement.Condition is BinaryExpressionSyntax binary &&
                        (binary.IsKind(SyntaxKind.LogicalAndExpression) ||
                         binary.IsKind(SyntaxKind.LogicalOrExpression)))
                    .Where(statement => GetConditionLineCount(statement) > 72)
                    .Where(statement => statement.Condition.GetText().ToString()
                        .Replace("\r\n", "\n", StringComparison.Ordinal)
                        .Split('\n')
                        .Length == 1);

                Assert.That(
                    inlineLongConditions,
                    Is.Empty,
                    $"{source}: conditions exceeding 72 characters must place one clause per line.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenLogicalOperatorsEndTheLine()
        {
            foreach (string source in Sources)
            {
                IEnumerable<SyntaxToken> leadingOperators = ParseTree(source)
                    .GetRoot()
                    .DescendantTokens()
                    .Where(token => token.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
                        token.IsKind(SyntaxKind.BarBarToken))
                    .Where(token => token.GetLocation().GetLineSpan().StartLinePosition.Line !=
                        token.GetPreviousToken().GetLocation().GetLineSpan().EndLinePosition.Line);

                Assert.That(
                    leadingOperators,
                    Is.Empty,
                    $"{source}: logical operators must terminate the line, not begin the next one.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenElseIfIsNotSplit()
        {
            foreach (string source in Sources)
            {
                IEnumerable<ElseClauseSyntax> splitElseIf = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ElseClauseSyntax>()
                    .Where(clause => clause.Statement is IfStatementSyntax inner &&
                        inner.IfKeyword.GetLocation().GetLineSpan().StartLinePosition.Line !=
                        clause.ElseKeyword.GetLocation().GetLineSpan().StartLinePosition.Line);

                Assert.That(splitElseIf, Is.Empty, $"{source}: 'else if' must remain on a single line.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenBlankLinesSeparateMethods()
        {
            foreach (string source in Sources)
            {
                string content = ArchitectureResources.ReadResource(
                    ArchitectureResources.ProductionSourcePrefix,
                    source)
                    .Replace("\r\n", "\n", StringComparison.Ordinal);

                Assert.That(
                    content,
                    Does.Not.Match(@";\n[ \t]*(private|public|protected|internal) "),
                    $"{source}: exactly one blank line must separate consecutive methods.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenContinuationsUseFourSpaces()
        {
            foreach (string source in Sources)
            {
                string[] lines = ArchitectureResources.ReadResource(
                    ArchitectureResources.ProductionSourcePrefix,
                    source)
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Split('\n');

                string[] invalidIndentations = lines
                    .Select((line, index) => new { line, index })
                    .Where(entry => entry.line.TrimStart().Length > 0)
                    .Where(entry => entry.line.StartsWith(' ') is false)
                    .Where(entry => entry.line.StartsWith('\t'))
                    .Select(entry => $"line {entry.index + 1}")
                    .ToArray();

                Assert.That(
                    invalidIndentations,
                    Is.Empty,
                    $"{source}: indentation must use four-space multiples ({string.Join(", ", invalidIndentations)}).");

                string[] misaligned = lines
                    .Select((line, index) => new { trimmed = line.TrimStart(), width = line.Length - line.TrimStart().Length, index })
                    .Where(entry => entry.trimmed.Length > 0)
                    .Where(entry => entry.width % 4 != 0)
                    .Select(entry => $"line {entry.index + 1}")
                    .ToArray();

                Assert.That(
                    misaligned,
                    Is.Empty,
                    $"{source}: every line must be indented by a multiple of four spaces ({string.Join(", ", misaligned)}).");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenExpressionBodiesUseTheNextLineForm()
        {
            foreach (string source in Sources)
            {
                IEnumerable<MemberDeclarationSyntax> bodiesOnSignatureLine = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .Where(member => member.GetFirstToken().GetLocation().GetLineSpan().StartLinePosition.Line ==
                        member.GetLastToken().GetLocation().GetLineSpan().StartLinePosition.Line)
                    .Where(member => member is MethodDeclarationSyntax method &&
                        method.ExpressionBody is not null &&
                        method.GetText().ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Length > 1 &&
                        method.ExpressionBody.ArrowToken.GetLocation().GetLineSpan().StartLinePosition.Line !=
                        method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line);

                Assert.That(
                    bodiesOnSignatureLine,
                    Is.Empty,
                    $"{source}: multi-line signatures must place the expression body arrow on the next line.");
            }
        }

        private static SyntaxTree ParseTree(string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProductionSourcePrefix,
                    source),
                path: source);

        private static int GetConditionLineCount(IfStatementSyntax statement)
        {
            string condition = statement.Condition.GetText().ToString()
                .Replace("\r\n", "\n", StringComparison.Ordinal);

            if (condition.Contains('\n', StringComparison.Ordinal))
            {
                return int.MaxValue;
            }

            int indentation = statement.IfKeyword.GetLocation().GetLineSpan().StartLinePosition.Character;

            return statement.IfKeyword.Span.Length + 1 + condition.Length + indentation;
        }
    }
}