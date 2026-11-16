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
    public sealed class ConstructionConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenChecked_ThenVariableDeclarationsUseTargetTypedConstruction()
        {
            foreach (string source in GetProductionSources())
            {
                string[] explicitConstructions = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<VariableDeclarationSyntax>()
                    .SelectMany(declaration => declaration.Variables)
                    .Where(variable => variable.Initializer?.Value is ObjectCreationExpressionSyntax creation &&
                        creation.ArgumentList is not null)
                    .Select(variable => variable.Initializer!.Value.ToString())
                    .ToArray();

                Assert.That(
                    explicitConstructions,
                    Is.Empty,
                    $"{source}: the declaration already states the type; use target-typed 'new(...)' " +
                    $"instead of {string.Join(", ", explicitConstructions)}.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenModelPropertiesUseAutoProperties()
        {
            foreach (string source in GetProductionSources())
            {
                IEnumerable<PropertyDeclarationSyntax> nonAutoProperties = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => property.Parent is TypeDeclarationSyntax type &&
                        IsModelType(type))
                    .Where(property => property.AccessorList is not null)
                    .Where(property => property.AccessorList!.Accessors.Any(accessor => accessor.Body is not null));

                Assert.That(
                    nonAutoProperties,
                    Is.Empty,
                    $"{source}: model, request, and settings properties must use auto-properties " +
                    "or expression bodies; accessor blocks with statements are not allowed.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenSingleStatementMethodsAreExpressionBodied()
        {
            foreach (string source in GetProductionSources())
            {
                IEnumerable<MethodDeclarationSyntax> blockBodiedSingleStatements = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.ExpressionBody is null)
                    .Where(method => method.Body is not null && method.Body.Statements.Count == 1)
                    .Where(method => method.Body!.Statements[0] is not LocalDeclarationStatementSyntax);

                Assert.That(
                    blockBodiedSingleStatements.Select(method => method.Identifier.ValueText),
                    Is.Empty,
                    $"{source}: single-statement methods must use an expression body.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenOverloadsAreAdjacent()
        {
            foreach (string source in GetProductionSources())
            {
                foreach (TypeDeclarationSyntax type in ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>())
                {
                    string[] methodNames = type.Members.OfType<MethodDeclarationSyntax>()
                        .Select(method => method.Identifier.ValueText)
                        .ToArray();

                    string[] nonAdjacentOverloads = methodNames
                        .Select((name, index) => new { name, index })
                        .GroupBy(entry => entry.name, StringComparer.Ordinal)
                        .Where(group => group.Count() > 1)
                        .Where(group => group.Last().index - group.First().index != group.Count() - 1)
                        .Select(group => group.Key)
                        .ToArray();

                    Assert.That(
                        nonAdjacentOverloads,
                        Is.Empty,
                        $"{source}: overloads of '{type.Identifier.ValueText}' must be declared adjacently " +
                        $"({string.Join(", ", nonAdjacentOverloads)}).");
                }
            }
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenOverloadedInterfacesAreMirroredWithMatchingParameterNames()
        {
            int inspectedInterfaces = 0;

            foreach (string source in GetProductionSources())
            {
                InterfaceDeclarationSyntax[] interfaces = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .ToArray();

                foreach (InterfaceDeclarationSyntax interfaceDeclaration in interfaces)
                {
                    string[] interfaceNames = interfaceDeclaration.Members
                        .OfType<MethodDeclarationSyntax>()
                        .SelectMany(method => method.ParameterList.Parameters
                            .Select(parameter => method.Identifier.ValueText + ":" + parameter.Identifier.ValueText))
                        .OrderBy(name => name, StringComparer.Ordinal)
                        .ToArray();

                    Assert.That(interfaceNames, Is.Not.Empty, $"{source}: the interface contract must be inspected.");

                    inspectedInterfaces += 1;
                }
            }

            Assert.That(inspectedInterfaces, Is.GreaterThan(0), "At least one production interface must be inspected.");
        }

        [Test]
        public void GivenProductionSources_WhenChecked_ThenImplementationsMirrorTheirInterfaceSignatures()
        {
            foreach (string source in GetProductionSources())
            {
                foreach (ClassDeclarationSyntax classDeclaration in ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>())
                {
                    string? interfaceName = classDeclaration.BaseList?.Types
                        .Select(baseType => baseType.Type.ToString())
                        .Where(name => name.StartsWith('I') && name.Length > 1 && char.IsUpper(name[1]))
                        .FirstOrDefault(name => GetProductionSources().Any(fileName => string.Equals(
                            ArchitectureResources.GetFileStem(fileName),
                            name,
                            StringComparison.Ordinal)));

                    if (interfaceName is null)
                    {
                        continue;
                    }

                    string interfaceSource = GetProductionSources()
                        .FirstOrDefault(fileName => string.Equals(
                            ArchitectureResources.GetFileStem(fileName),
                            interfaceName,
                            StringComparison.Ordinal))
                        ?? string.Empty;

                    Assert.That(
                        interfaceSource,
                        Is.Not.Empty,
                        $"{source}: the colocated interface '{interfaceName}' must exist in the production sources.");

                    MethodDeclarationSyntax[] interfaceMethods = ParseTree(interfaceSource)
                        .GetRoot()
                        .DescendantNodes()
                        .OfType<InterfaceDeclarationSyntax>()
                        .Where(declaration => string.Equals(
                            declaration.Identifier.ValueText,
                            interfaceName,
                            StringComparison.Ordinal))
                        .SelectMany(declaration => declaration.Members.OfType<MethodDeclarationSyntax>())
                        .ToArray();

                    foreach (MethodDeclarationSyntax interfaceMethod in interfaceMethods)
                    {
                        MethodDeclarationSyntax? implementation = classDeclaration.Members
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault(method => string.Equals(
                                method.Identifier.ValueText,
                                interfaceMethod.Identifier.ValueText,
                                StringComparison.Ordinal) &&
                                method.ParameterList.Parameters.Count ==
                                interfaceMethod.ParameterList.Parameters.Count);

                        Assert.That(
                            implementation,
                            Is.Not.Null,
                            $"{source}: '{classDeclaration.Identifier.ValueText}' must implement " +
                            $"'{interfaceName}.{interfaceMethod.Identifier.ValueText}'.");

                        Assert.That(
                            implementation!.ParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText),
                            Is.EqualTo(interfaceMethod.ParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText)),
                            $"{source}: implementation parameter names must match '{interfaceName}." +
                            $"{interfaceMethod.Identifier.ValueText}' exactly.");
                    }
                }
            }
        }

        private static IReadOnlyList<string> GetProductionSources()
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(
                ArchitectureResources.ProductionSourcePrefix);

            Assert.That(sources, Is.Not.Empty, "No production source files were found.");

            return sources;
        }

        private static SyntaxTree ParseTree(string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProductionSourcePrefix,
                    source),
                path: source);

        private static bool IsModelType(TypeDeclarationSyntax type)
            => type.Identifier.ValueText.EndsWith("Request", StringComparison.Ordinal) ||
                type.Identifier.ValueText.EndsWith("Response", StringComparison.Ordinal) ||
                type.Identifier.ValueText.EndsWith("Settings", StringComparison.Ordinal);
    }
}