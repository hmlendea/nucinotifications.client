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
    public sealed class MemberOrganisationTests
    {
        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenMembersAreOrderedByKind(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<TypeDeclarationSyntax> typeDeclarations = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>();

                foreach (TypeDeclarationSyntax typeDeclaration in typeDeclarations)
                {
                    int[] kindOrders = typeDeclaration.Members
                        .Select(GetMemberKindOrder)
                        .Where(order => order >= 0)
                        .ToArray();

                    Assert.That(
                        kindOrders,
                        Is.Ordered,
                        $"{source}: members of '{typeDeclaration.Identifier.ValueText}' must be ordered " +
                        "fields -> properties -> events -> constructors -> methods.");
                }
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenFieldModifiersAreOrdered(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                SyntaxNode root = ParseTree(sourcePrefix, source).GetRoot();

                int[] fieldOrders = root.DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Select(GetFieldOrder)
                    .Where(order => order >= 0)
                    .ToArray();

                Assert.That(
                    fieldOrders,
                    Is.Ordered,
                    $"{source}: static readonly fields must precede mutable and instance fields.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenOverloadsAreOrderedByParameterCount(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<IGrouping<string, MethodDeclarationSyntax>> methodGroups = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .GroupBy(method => method.Identifier.ValueText, StringComparer.Ordinal);

                foreach (IGrouping<string, MethodDeclarationSyntax> methodGroup in methodGroups)
                {
                    int[] parameterCounts = methodGroup
                        .Select(method => method.ParameterList.Parameters.Count)
                        .ToArray();

                    if (parameterCounts.Length < 2)
                    {
                        continue;
                    }

                    Assert.That(
                        parameterCounts,
                        Is.Ordered,
                        $"{source}: the '{methodGroup.Key}' overloads must be ordered by parameter count.");
                }
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenPropertiesUseAutoOrExpressionBodies(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<PropertyDeclarationSyntax> statementBodiedProperties = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => property.AccessorList is not null)
                    .Where(property => property.AccessorList!.Accessors.Any(accessor =>
                        accessor.Body is not null &&
                        accessor.Body.Statements.Count > 1));

                Assert.That(
                    statementBodiedProperties,
                    Is.Empty,
                    $"{source}: properties must use auto-properties or expression bodies.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenUsingDirectivesPrecedeTheNamespace(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                SyntaxNode root = ParseTree(sourcePrefix, source).GetRoot();

                int namespaceIndex = root.ChildNodes().ToList()
                    .FindIndex(node => node is BaseNamespaceDeclarationSyntax);

                Assert.That(namespaceIndex, Is.GreaterThanOrEqualTo(0), $"{source}: a namespace declaration is required.");

                IEnumerable<UsingDirectiveSyntax> innerUsings = root.ChildNodes()
                    .Skip(namespaceIndex)
                    .SelectMany(node => node.ChildNodes())
                    .OfType<UsingDirectiveSyntax>();

                Assert.That(
                    innerUsings,
                    Is.Empty,
                    $"{source}: using directives must be declared above and outside the namespace.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static int GetMemberKindOrder(MemberDeclarationSyntax member)
        {
            if (member is FieldDeclarationSyntax)
            {
                return 0;
            }

            if (member is PropertyDeclarationSyntax)
            {
                return 1;
            }

            if (member is EventFieldDeclarationSyntax)
            {
                return 2;
            }

            if (member is ConstructorDeclarationSyntax)
            {
                return 3;
            }

            if (member is MethodDeclarationSyntax)
            {
                return 4;
            }

            return -1;
        }

        private static int GetFieldOrder(FieldDeclarationSyntax field)
        {
            bool isStatic = field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
            bool isReadOnly = field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword));

            if (isStatic && isReadOnly)
            {
                return 0;
            }

            if (isStatic)
            {
                return 1;
            }

            if (isReadOnly)
            {
                return 2;
            }

            return 3;
        }
    }
}