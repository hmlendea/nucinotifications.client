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
    public sealed class PublicApiConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenCheckingPublicTypes_ThenTheyHaveXmlDocumentation()
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix))
            {
                IEnumerable<BaseTypeDeclarationSyntax> publicTypes = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Where(type => HasModifier(type.Modifiers, SyntaxKind.PublicKeyword));

                foreach (BaseTypeDeclarationSyntax type in publicTypes)
                {
                    Assert.That(
                        HasDocumentation(type),
                        Is.True,
                        $"{source}: public type '{type.Identifier.ValueText}' must have XML documentation.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingPublicMembers_ThenTheyHaveXmlDocumentation()
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix))
            {
                IEnumerable<MemberDeclarationSyntax> publicMembers = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .Where(member => HasModifier(member, SyntaxKind.PublicKeyword));

                foreach (MemberDeclarationSyntax member in publicMembers)
                {
                    Assert.That(
                        HasDocumentation(member),
                        Is.True,
                        $"{source}: public member '{GetMemberName(member)}' must have XML documentation.");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingTypes_ThenEveryTypeHasExplicitAccessibility()
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix))
            {
                IEnumerable<BaseTypeDeclarationSyntax> typesWithoutAccessibility = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<BaseTypeDeclarationSyntax>()
                    .Where(type => HasAnyAccessibility(type.Modifiers) is false);

                Assert.That(
                    typesWithoutAccessibility,
                    Is.Empty,
                    $"{source}: every type must declare explicit accessibility.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingInterfaces_ThenTheirMembersRemainImplicitlyPublic()
        {
            // Narrowly scoped: interface members are implicitly public in this codebase, so
            // explicit accessibility is not required for them even though it is required
            // for every other member kind.
            foreach (string source in ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix))
            {
                MemberDeclarationSyntax[] redundantModifiers = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .SelectMany(interfaceDeclaration => interfaceDeclaration.Members)
                    .Where(member => HasAnyAccessibility(GetModifiers(member)))
                    .ToArray();

                Assert.That(
                    redundantModifiers,
                    Is.Empty,
                    $"{source}: interface members must use implicit public accessibility.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingNamespaceDeclarations_ThenThereIsExactlyOneNamespacePerFile()
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix))
            {
                NamespaceDeclarationSyntax[] namespaces = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .ToArray();

                Assert.That(
                    namespaces,
                    Has.Length.EqualTo(1),
                    $"{source}: exactly one block namespace must be declared.");
            }
        }

        private static SyntaxTree ParseTree(string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProductionSourcePrefix,
                    source),
                path: source);

        private static bool HasDocumentation(SyntaxNode node)
            => node.GetLeadingTrivia().Any(trivia =>
                trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

        private static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind)
            => modifiers.Any(modifier => modifier.IsKind(kind));

        private static bool HasModifier(MemberDeclarationSyntax member, SyntaxKind kind)
            => HasModifier(GetModifiers(member), kind);

        private static bool HasAnyAccessibility(SyntaxTokenList modifiers)
            => modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword));

        private static SyntaxTokenList GetModifiers(SyntaxNode node)
            => node switch
            {
                BaseTypeDeclarationSyntax type => type.Modifiers,
                FieldDeclarationSyntax field => field.Modifiers,
                PropertyDeclarationSyntax property => property.Modifiers,
                EventFieldDeclarationSyntax eventField => eventField.Modifiers,
                ConstructorDeclarationSyntax constructor => constructor.Modifiers,
                MethodDeclarationSyntax method => method.Modifiers,
                _ => default
            };

        private static string GetMemberName(MemberDeclarationSyntax member)
            => member switch
            {
                BaseMethodDeclarationSyntax method => method switch
                {
                    ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
                    MethodDeclarationSyntax namedMethod => namedMethod.Identifier.ValueText,
                    _ => member.Kind().ToString()
                },
                BasePropertyDeclarationSyntax property => property switch
                {
                    PropertyDeclarationSyntax namedProperty => namedProperty.Identifier.ValueText,
                    _ => member.Kind().ToString()
                },
                FieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.ValueText,
                EventFieldDeclarationSyntax eventField => eventField.Declaration.Variables.First().Identifier.ValueText,
                _ => member.Kind().ToString()
            };
    }
}