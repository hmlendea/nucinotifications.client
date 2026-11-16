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
    public sealed class TypeDeclarationConventionTests
    {
        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenEveryProductionClassIsSealedUnlessItIsExtensibleOrAModel(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] unsealedClasses = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(declaration => declaration.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.SealedKeyword) ||
                        modifier.IsKind(SyntaxKind.StaticKeyword) ||
                        modifier.IsKind(SyntaxKind.AbstractKeyword)) is false)
                    .Where(declaration => HasColocatedInterface(declaration) is false)
                    .Where(declaration => InheritsFromBaseType(declaration) is false)
                    .Select(declaration => declaration.Identifier.ValueText)
                    .ToArray();

                Assert.That(
                    unsealedClasses,
                    Is.Empty,
                    $"{source}: classes must be sealed unless they are framework contracts or implement a colocated interface.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoNullableObliviousConstructionIsDeclared(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] invalidDeclarations = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ConstructorDeclarationSyntax>()
                    .Where(constructor => constructor.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.PublicKeyword)) is false)
                    .Where(constructor => constructor.ParameterList.Parameters.Count == 0)
                    .Select(constructor => constructor.Identifier.ValueText)
                    .ToArray();

                Assert.That(
                    invalidDeclarations,
                    Is.Empty,
                    $"{source}: non-public parameterless constructors must not be declared.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenInterfacesExposeOnlyMemberSignatures(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<MemberDeclarationSyntax> implementedMembers = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Parent is InterfaceDeclarationSyntax)
                    .Where(method => method.Body is not null || method.ExpressionBody is not null);

                Assert.That(implementedMembers, Is.Empty, $"{source}: interfaces must not implement members.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoStaticMutableStateIsDeclared(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<FieldDeclarationSyntax> mutableStatics = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .Where(field => field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
                    .Where(field => field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword)) is false)
                    .Where(field => field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword)) is false);

                Assert.That(mutableStatics, Is.Empty, $"{source}: static fields must be readonly.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static bool HasColocatedInterface(ClassDeclarationSyntax declaration)
            => declaration.BaseList is not null &&
                declaration.BaseList.Types.Any(baseType =>
                    baseType.Type.ToString().StartsWith('I') &&
                    baseType.Type.ToString().Length > 1 &&
                    char.IsUpper(baseType.Type.ToString()[1]));

        private static bool InheritsFromBaseType(ClassDeclarationSyntax declaration)
            => declaration.BaseList is not null &&
                declaration.BaseList.Types.Any(baseType =>
                    baseType.Type.ToString().StartsWith('I') is false);
    }
}