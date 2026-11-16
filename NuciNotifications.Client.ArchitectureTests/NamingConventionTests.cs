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
    public sealed class NamingConventionTests
    {
        private static readonly string[] PermittedBooleanPrefixes =
        [
            "Is",
            "Has",
            "Does",
            "Are",
            "Was",
            "Were",
            "Can",
            "Should"
        ];

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenInterfacesUseTheIPrefixedPascalCaseForm(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] invalidNames = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => IsIPrefixedPascalCase(name) is false)
                    .ToArray();

                Assert.That(
                    invalidNames,
                    Is.Empty,
                    $"{source}: interface names must use the 'I' prefix followed by PascalCase.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenClassesAndMethodsUsePascalCase(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                SyntaxNode root = ParseTree(sourcePrefix, source).GetRoot();

                string[] invalidClassNames = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(declaration => declaration.Identifier.ValueText)
                    .Where(name => IsPascalCase(name) is false)
                    .ToArray();

                string[] invalidMethodNames = root.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Select(method => method.Identifier.ValueText)
                    .Where(name => IsPascalCase(name) is false)
                    .ToArray();

                Assert.That(invalidClassNames, Is.Empty, $"{source}: class names must use PascalCase.");
                Assert.That(invalidMethodNames, Is.Empty, $"{source}: method names must use PascalCase.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenInstanceFieldsUseCamelCaseWithoutUnderscorePrefix(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] invalidFieldNames = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Where(field => field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword)) is false)
                    .SelectMany(field => field.Declaration.Variables)
                    .Select(variable => variable.Identifier.ValueText)
                    .Where(name => IsValidFieldName(name) is false)
                    .ToArray();

                Assert.That(
                    invalidFieldNames,
                    Is.Empty,
                    $"{source}: fields must use camelCase or PascalCase without an underscore prefix.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenBooleanPropertiesUseAPermittedPrefix(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] invalidBooleanNames = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => property.Type is PredefinedTypeSyntax predefined &&
                        predefined.Keyword.IsKind(SyntaxKind.BoolKeyword))
                    .Select(property => property.Identifier.ValueText)
                    .Where(name => PermittedBooleanPrefixes.Any(prefix =>
                        name.StartsWith(prefix, StringComparison.Ordinal)) is false)
                    .ToArray();

                Assert.That(
                    invalidBooleanNames,
                    Is.Empty,
                    $"{source}: boolean properties must use Is/Has/Does/Are/Was/Were/Can/Should prefixes.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenConfigurationAndRequestTypesUseTheDocumentedSuffixes(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<NamespaceDeclarationSyntax> namespaces = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>();

                foreach (NamespaceDeclarationSyntax namespaceDeclaration in namespaces)
                {
                    string namespaceName = namespaceDeclaration.Name.ToString();

                    string[] classes = namespaceDeclaration.DescendantNodes()
                        .OfType<ClassDeclarationSyntax>()
                        .Select(declaration => declaration.Identifier.ValueText)
                        .ToArray();

                    if (namespaceName.EndsWith(".Configuration", StringComparison.Ordinal))
                    {
                        Assert.That(
                            classes.Where(name => name.EndsWith("Settings", StringComparison.Ordinal) is false),
                            Is.Empty,
                            $"{source}: configuration classes must use the 'Settings' suffix.");
                    }

                    if (namespaceName.EndsWith(".Requests", StringComparison.Ordinal))
                    {
                        Assert.That(
                            classes.Where(name => name.EndsWith("Request", StringComparison.Ordinal) is false),
                            Is.Empty,
                            $"{source}: request DTOs must use the 'Request' suffix.");
                    }
                }
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoInterfacesNamespaceIsUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] namespaces = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .Select(declaration => declaration.Name.ToString())
                    .Where(name => name.EndsWith(".Interfaces", StringComparison.Ordinal))
                    .ToArray();

                Assert.That(namespaces, Is.Empty, $"{source}: interfaces must be colocated with implementations.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static bool IsPascalCase(string name)
            => string.IsNullOrEmpty(name) is false && char.IsUpper(name[0]);

        private static bool IsIPrefixedPascalCase(string name)
            => name is not null &&
                name.Length > 1 &&
                name[0] == 'I' &&
                char.IsUpper(name[1]);

        private static bool IsValidFieldName(string name)
            => IsPascalCase(name) || (string.IsNullOrEmpty(name) is false && char.IsLower(name[0]));
    }
}