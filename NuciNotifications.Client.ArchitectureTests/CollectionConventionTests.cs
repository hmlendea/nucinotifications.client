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
    public sealed class CollectionConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenCheckingCollectionDeclarations_ThenInterfaceCollectionTypesAreUsed()
        {
            string[] forbiddenTypes =
            [
                "List",
                "IList",
                "IReadOnlyList",
                "HashSet",
                "ICollection",
                "Dictionary"
            ];

            foreach (string source in ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix))
            {
                string[] violations = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .Where(member => member.Parent is TypeDeclarationSyntax parent &&
                        IsDtosOrRequests(parent))
                    .SelectMany(GetDeclaredTypeNames)
                    .Where(name => forbiddenTypes.Contains(name, StringComparer.Ordinal))
                    .ToArray();

                Assert.That(
                    violations,
                    Is.Empty,
                    $"{source}: collection members must use IEnumerable<T> instead of {string.Join(", ", violations)}.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingCollectionCreation_ThenCollectionExpressionsAreUsed()
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix))
            {
                SyntaxNode root = ParseTree(source).GetRoot();

                string[] listCreations = root.DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Select(creation => creation.Type.ToString())
                    .Where(typeName => IsCollectionImplementation(typeName))
                    .ToArray();

                Assert.That(
                    listCreations,
                    Is.Empty,
                    $"{source}: collection expressions must be used instead of {string.Join(", ", listCreations)}.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingSerialisedCollections_ThenArraysArePermittedOnlyOnRequests()
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix))
            {
                PropertyDeclarationSyntax[] arrayProperties = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => property.Type is ArrayTypeSyntax)
                    .ToArray();

                foreach (PropertyDeclarationSyntax property in arrayProperties)
                {
                    Assert.That(
                        property.Parent is TypeDeclarationSyntax type &&
                            type.Identifier.ValueText.EndsWith("Request", StringComparison.Ordinal),
                        Is.True,
                        $"{source}: only serialised request DTOs may declare array properties " +
                        $"('{property.Identifier.ValueText}').");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingInMemoryQueries_ThenLinqIsUsedForFiltering()
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix))
            {
                IEnumerable<ForEachStatementSyntax> loops = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ForEachStatementSyntax>()
                    .Where(loop => loop.Statement.DescendantNodes().OfType<IfStatementSyntax>().Any() &&
                        loop.Statement.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
                            invocation.Expression.ToString().EndsWith(".Add", StringComparison.Ordinal)));

                Assert.That(
                    loops,
                    Is.Empty,
                    $"{source}: conditional collection building loops must be expressed with LINQ.");
            }
        }

        private static SyntaxTree ParseTree(string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProductionSourcePrefix,
                    source),
                path: source);

        private static bool IsDtosOrRequests(TypeDeclarationSyntax type)
            => type.Identifier.ValueText.EndsWith("Request", StringComparison.Ordinal) ||
                type.Identifier.ValueText.EndsWith("Response", StringComparison.Ordinal) ||
                type.Identifier.ValueText.EndsWith("Settings", StringComparison.Ordinal);

        private static IEnumerable<string> GetDeclaredTypeNames(MemberDeclarationSyntax member)
        {
            if (member is PropertyDeclarationSyntax property)
            {
                yield return GetSimpleTypeName(property.Type);
            }

            if (member is MethodDeclarationSyntax method)
            {
                yield return GetSimpleTypeName(method.ReturnType);
            }

            if (member is FieldDeclarationSyntax field)
            {
                yield return GetSimpleTypeName(field.Declaration.Type);
            }
        }

        private static string GetSimpleTypeName(TypeSyntax type)
        {
            if (type is GenericNameSyntax generic)
            {
                return GetSimpleTypeName(generic);
            }

            if (type is NullableTypeSyntax nullable)
            {
                return GetSimpleTypeName(nullable.ElementType);
            }

            return type.ToString();
        }

        private static string GetSimpleTypeName(GenericNameSyntax generic)
        {
            string identifier = generic.Identifier.ValueText;

            if (identifier is "IEnumerable" or "Task" or "ValueTask")
            {
                return "IEnumerable";
            }

            return identifier;
        }

        private static bool IsCollectionImplementation(string typeName)
            => typeName.StartsWith("List<", StringComparison.Ordinal) ||
                typeName.StartsWith("Dictionary<", StringComparison.Ordinal) ||
                typeName.StartsWith("HashSet<", StringComparison.Ordinal) ||
                typeName.EndsWith("[]", StringComparison.Ordinal);
    }
}