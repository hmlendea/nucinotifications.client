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
    public sealed class TestSourceConventionTests
    {
        private static IEnumerable<string> TestSourcePrefixes =>
        [
            ArchitectureResources.UnitTestSourcePrefix,
            ArchitectureResources.ArchitectureTestSourcePrefix
        ];

        [TestCaseSource(nameof(TestSourcePrefixes))]
        public void GivenTestSources_WhenChecked_ThenEveryFileDeclaresOneTypeMatchingItsName(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                IReadOnlyList<string> declaredTypes = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Select(declaration => declaration.Identifier.ValueText)
                    .ToArray();

                Assert.That(
                    declaredTypes,
                    Has.Count.EqualTo(1),
                    $"{source}: expected exactly one declared type, but found {declaredTypes.Count}.");

                Assert.That(
                    declaredTypes[0],
                    Is.EqualTo(ArchitectureResources.GetFileStem(source)),
                    $"{source}: the declared type must match the file name.");
            }
        }

        [TestCaseSource(nameof(TestSourcePrefixes))]
        public void GivenTestSources_WhenChecked_ThenFixturesUseNUnitAndNotXunit(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                SyntaxNode root = ParseTree(sourcePrefix, source).GetRoot();

                string[] xunitImports = root.DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(directive => directive.Name?.ToString() ?? string.Empty)
                    .Where(name => name.StartsWith("Xunit", StringComparison.Ordinal))
                    .ToArray();

                string[] xunitAttributes = root.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .Select(attribute => attribute.Name.ToString())
                    .Where(name => name is "Fact" or "Theory" or "InlineData")
                    .ToArray();

                Assert.That(xunitImports, Is.Empty, $"{source}: xunit must not be referenced; NUnit is prescribed.");

                Assert.That(
                    xunitAttributes,
                    Is.Empty,
                    $"{source}: xunit attributes are not allowed; NUnit is the prescribed framework.");
            }
        }

        [TestCaseSource(nameof(TestSourcePrefixes))]
        public void GivenTestSources_WhenChecked_ThenFixturesDeclareTheNUnitFixtureAttribute(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<ClassDeclarationSyntax> fixtureDeclarations = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => declaration.Identifier.ValueText.EndsWith("Tests", StringComparison.Ordinal));

                foreach (ClassDeclarationSyntax fixture in fixtureDeclarations)
                {
                    Assert.That(
                        HasAttribute(fixture.AttributeLists, "TestFixture"),
                        Is.True,
                        $"{source}: '{fixture.Identifier.ValueText}' must declare the [TestFixture] attribute.");
                }
            }
        }

        [TestCaseSource(nameof(TestSourcePrefixes))]
        public void GivenTestSources_WhenChecked_ThenTestMethodsUseTheGivenWhenThenName(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] invalidNames = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => HasAttribute(method.AttributeLists, "Test") ||
                        HasAttribute(method.AttributeLists, "TestCase"))
                    .Select(method => method.Identifier.ValueText)
                    .Where(name => IsGivenWhenThenName(name) is false)
                    .ToArray();

                Assert.That(
                    invalidNames,
                    Is.Empty,
                    $"{source}: test methods must use the Given..._When..._Then... naming convention.");
            }
        }

        [TestCase("UnitTestSources/")]
        public void GivenUnitTestSources_WhenChecked_ThenTheyDoNotDependOnReflection(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] reflectionImports = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(directive => directive.Name?.ToString() ?? string.Empty)
                    .Where(name => name is "System.Reflection")
                    .ToArray();

                Assert.That(
                    reflectionImports,
                    Is.Empty,
                    $"{source}: tests must not inspect non-public code through reflection.");
            }
        }

        [TestCase("ArchitectureTestSources/")]
        public void GivenArchitectureTestSources_WhenChecked_ThenTheyUseBlockNamespaces(string sourcePrefix)
        {
            // Narrowly scoped: production and architecture-test sources must use block
            // namespaces. The pre-existing unit-test sources use file-scoped namespaces,
            // so that fixture is not included in this rule.
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                Assert.That(
                    ArchitectureResources.ReadResource(sourcePrefix, source),
                    Does.Not.Match(@"(?m)^\s*namespace\s+[A-Za-z_][A-Za-z0-9_.]*\s*;"),
                    $"{source}: file-scoped namespaces are not allowed.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists.Any(list => HasAttribute(list.Attributes, attributeName));

        private static bool HasAttribute(SeparatedSyntaxList<AttributeSyntax> attributes, string attributeName)
            => attributes.Any(attribute =>
                string.Equals(attribute.Name.ToString(), attributeName, StringComparison.Ordinal));

        private static bool IsGivenWhenThenName(string name)
            => name.StartsWith("Given", StringComparison.Ordinal) &&
                name.Contains("_When", StringComparison.Ordinal) &&
                name.Contains("_Then", StringComparison.Ordinal);
    }
}