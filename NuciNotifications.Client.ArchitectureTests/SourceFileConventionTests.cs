using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace NuciNotifications.Client.ArchitectureTests
{
    [TestFixture]
    public sealed class SourceFileConventionTests
    {
        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenEveryFileDeclaresExactlyOneTypeMatchingItsName(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                IReadOnlyList<string> declaredTypes = GetDeclaredTypeNames(sourcePrefix, source);

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

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenBlockNamespacesAreUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"(?m)^\s*namespace\s+[A-Za-z_][A-Za-z0-9_.]*\s*;"),
                    $"{source}: file-scoped namespaces are not allowed.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenTheNamespaceMatchesTheFolder(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string[] namespaces = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .Select(declaration => declaration.Name.ToString())
                    .ToArray();

                Assert.That(namespaces, Has.Length.EqualTo(1), $"{source}: exactly one namespace is required.");

                Assert.That(
                    namespaces[0],
                    Is.EqualTo(ArchitectureResources.GetExpectedNamespace(sourcePrefix, source)),
                    $"{source}: the namespace must mirror the folder structure.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoNullableDirectivesOrRegionsAreUsed(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"(?m)^\s*#nullable\s+"),
                    $"{source}: the nullable directive is not allowed.");

                Assert.That(
                    content,
                    Does.Not.Match(@"(?m)^\s*#(end)?region\b"),
                    $"{source}: regions are not allowed.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenTheLayoutIsClean(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                Assert.That(
                    content,
                    Does.Not.Match(@"[ \t]+(\r?\n)"),
                    $"{source}: trailing whitespace is not allowed.");

                Assert.That(
                    content,
                    Does.Not.Match(@"\n[ \t]*\n[ \t]*[}\])]"),
                    $"{source}: no blank line may precede a closing delimiter.");

                Assert.That(
                    content,
                    Does.Not.Match(@"\n[ \t]*\n[ \t]*\n[ \t]*namespace "),
                    $"{source}: exactly one blank line must precede the namespace declaration.");
            }
        }

        private static IReadOnlyList<string> GetDeclaredTypeNames(string sourcePrefix, string source)
        {
            SyntaxNode root = ParseTree(sourcePrefix, source).GetRoot();

            string[] typeDeclarations = root.DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .Select(declaration => declaration.Identifier.ValueText)
                .ToArray();

            string[] delegateDeclarations = root.DescendantNodes()
                .OfType<DelegateDeclarationSyntax>()
                .Select(declaration => declaration.Identifier.ValueText)
                .ToArray();

            return typeDeclarations.Concat(delegateDeclarations).ToArray();
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);
    }
}