using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace NuciNotifications.Client.ArchitectureTests
{
    [TestFixture]
    public sealed class UnitTestSourceConventionTests
    {
        private static readonly string[] ClassicAssertions =
        [
            "AreEqual",
            "AreNotEqual",
            "IsTrue",
            "IsFalse",
            "IsNull",
            "IsNotNull",
            "Greater",
            "Less"
        ];

        private static readonly string[] PlatformDependentApis =
        [
            "DateTime.Now",
            "DateTime.Today",
            "DateTime.UtcNow",
            "Environment.CurrentDirectory",
            "Environment.MachineName",
            "Environment.UserName",
            "Environment.GetEnvironmentVariable",
            "CultureInfo.CurrentCulture",
            "Path.GetTempPath"
        ];

        [Test]
        public void GivenUnitTestSources_WhenChecked_ThenTheConstraintModelIsUsed()
        {
            foreach (string source in GetUnitTestSources())
            {
                string[] classicAssertCalls = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Select(invocation => invocation.Expression.ToString())
                    .Where(expression => expression.StartsWith("Assert.", StringComparison.Ordinal))
                    .Where(expression => ClassicAssertions.Contains(
                        expression.Substring("Assert.".Length),
                        StringComparer.Ordinal))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                Assert.That(
                    classicAssertCalls,
                    Is.Empty,
                    $"{source}: use the Assert.That constraint model instead of {string.Join(", ", classicAssertCalls)}.");
            }
        }

        [Test]
        public void GivenUnitTestSources_WhenChecked_ThenBooleanAssertionsUseConstraintValues()
        {
            // Narrowly scoped: the prescribed rule prohibits Assert.That(..., Is.True);
            // Is.False remains permitted for explicit negative-value assertions.
            foreach (string source in GetUnitTestSources())
            {
                string[] booleanConstraints = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation => invocation.Expression.ToString().EndsWith("Assert.That", StringComparison.Ordinal))
                    .Where(invocation => invocation.ArgumentList.Arguments.Count == 2)
                    .Select(invocation => invocation.ArgumentList.Arguments[1].Expression.ToString())
                    .Where(expression => string.Equals(expression, "Is.True", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    booleanConstraints,
                    Is.Empty,
                    $"{source}: assert the expected value with Is.EqualTo/Has.Some rather than {string.Join(", ", booleanConstraints)}.");
            }
        }

        [Test]
        public void GivenUnitTestSources_WhenChecked_ThenParameterisedTestsProvideAtLeastTwoCases()
        {
            foreach (string source in GetUnitTestSources())
            {
                IEnumerable<MethodDeclarationSyntax> underParameterised = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => CountAttributes(method.AttributeLists, "TestCase") == 1)
                    .Where(method => CountAttributes(method.AttributeLists, "TestCaseSource") == 0);

                Assert.That(
                    underParameterised.Select(method => method.Identifier.ValueText),
                    Is.Empty,
                    $"{source}: parameterised tests must declare at least two test cases.");
            }
        }

        [Test]
        public void GivenUnitTestSources_WhenChecked_ThenFixturesUseNUnitSetUpForMocksAndSubject()
        {
            foreach (string source in GetUnitTestSources())
            {
                foreach (ClassDeclarationSyntax fixture in ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => declaration.Identifier.ValueText.EndsWith("Tests", StringComparison.Ordinal)))
                {
                    FieldDeclarationSyntax[] stateFields = fixture.Members
                        .OfType<FieldDeclarationSyntax>()
                        .Where(field => field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PrivateKeyword)) is false)
                        .ToArray();

                    Assert.That(
                        stateFields.Select(field => field.Declaration.Variables.First().Identifier.ValueText),
                        Is.Empty,
                        $"{source}: mock and subject fields must be class-level private fields.");

                    MethodDeclarationSyntax[] setUpMethods = fixture.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Where(method => HasAttribute(method.AttributeLists, "SetUp"))
                        .ToArray();

                    Assert.That(
                        setUpMethods.Where(method => string.Equals(method.Identifier.ValueText, "SetUp", StringComparison.Ordinal) is false),
                        Is.Empty,
                        $"{source}: the NUnit setup method must be named 'SetUp'.");
                }
            }
        }

        [Test]
        public void GivenUnitTestSources_WhenChecked_ThenNoUnitTestProjectHoldsIntegrationTests()
        {
            string[] integrationFixtures = GetUnitTestSources()
                .SelectMany(source => ParseTree(source).GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
                    .Select(declaration => declaration.Identifier.ValueText + "|" + source))
                .Where(entry => entry.Split('|')[0].EndsWith("IntegrationTests", StringComparison.Ordinal))
                .ToArray();

            Assert.That(
                integrationFixtures,
                Is.Empty,
                "Integration tests must live in a dedicated integration-test project.");
        }

        [Test]
        public void GivenUnitTestSources_WhenChecked_ThenTheyAreEnvironmentAgnostic()
        {
            foreach (string source in GetUnitTestSources())
            {
                string content = ArchitectureResources.ReadResource(
                    ArchitectureResources.UnitTestSourcePrefix,
                    source);

                string[] platformDependencies = PlatformDependentApis
                    .Where(api => content.Contains(api, StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    platformDependencies,
                    Is.Empty,
                    $"{source}: tests must be platform- and environment-agnostic ({string.Join(", ", platformDependencies)}).");
            }
        }

        [Test]
        public void GivenUnitTestSources_WhenChecked_ThenTheyDeclareNoInternalsVisibleToRequirement()
        {
            foreach (string source in GetUnitTestSources())
            {
                string[] friendAssemblyDeclarations = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<SkippedTokensTriviaSyntax>()
                    .Select(trivia => trivia.ToString())
                    .Where(text => text.Contains("InternalsVisibleTo", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    friendAssemblyDeclarations,
                    Is.Empty,
                    $"{source}: tests must not require InternalsVisibleTo.");
            }
        }

        private static IReadOnlyList<string> GetUnitTestSources()
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(
                ArchitectureResources.UnitTestSourcePrefix);

            Assert.That(sources, Is.Not.Empty, "No unit-test source files were found.");

            return sources;
        }

        private static SyntaxTree ParseTree(string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.UnitTestSourcePrefix,
                    source),
                path: source);

        private static int CountAttributes(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists.SelectMany(list => list.Attributes)
                .Count(attribute => string.Equals(attribute.Name.ToString(), attributeName, StringComparison.Ordinal));

        private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => CountAttributes(attributeLists, attributeName) > 0;
    }
}