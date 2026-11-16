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
    public sealed class SerialisationConventionTests
    {
        [Test]
        public void GivenProductionSources_WhenCheckingJsonOptions_ThenSharedOptionsAreNotAllocatedRepeatedly()
        {
            foreach (string source in GetProductionSources())
            {
                ObjectCreationExpressionSyntax[] allocatedOptions = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ObjectCreationExpressionSyntax>()
                    .Where(creation => string.Equals(
                        creation.Type.ToString(),
                        "JsonSerializerOptions",
                        StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    allocatedOptions,
                    Is.Empty,
                    $"{source}: reuse JsonSerializerOptions.Web or a static readonly options instance.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingJsonNameAttributes_ThenIdentifierSegmentsUseId()
        {
            foreach (string source in GetProductionSources())
            {
                PropertyDeclarationSyntax[] invalidProperties = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => property.Identifier.ValueText.Contains("Identifier", StringComparison.Ordinal))
                    .Where(property => HasJsonPropertyNameForIdentifier(property) is false)
                    .ToArray();

                Assert.That(
                    invalidProperties,
                    Is.Empty,
                    $"{source}: Identifier properties must use a JsonPropertyName ending in 'id'.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingDateTimeCalls_ThenFormatProvidersAreSpecified()
        {
            foreach (string source in GetProductionSources())
            {
                string[] invalidCalls = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation => invocation.Expression.ToString().EndsWith(".ToString", StringComparison.Ordinal) ||
                        invocation.Expression.ToString().EndsWith("DateTime.Parse", StringComparison.Ordinal) ||
                        invocation.Expression.ToString().EndsWith("DateTime.TryParse", StringComparison.Ordinal))
                    .Where(invocation => invocation.ArgumentList.Arguments.Count == 0 ||
                        invocation.ArgumentList.Arguments.Count == 1)
                    .Select(invocation => invocation.ToString())
                    .ToArray();

                Assert.That(
                    invalidCalls,
                    Is.Empty,
                    $"{source}: date/time formatting and parsing must specify a format or provider.");
            }
        }

        [Test]
        public void GivenProductionSources_WhenCheckingJsonIgnoreProperties_ThenTheyAreComputedOrSigningOnly()
        {
            foreach (string source in GetProductionSources())
            {
                IEnumerable<PropertyDeclarationSyntax> ignoredProperties = ParseTree(source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(property => HasAttribute(property.AttributeLists, "JsonIgnore"));

                foreach (PropertyDeclarationSyntax property in ignoredProperties)
                {
                    Assert.That(
                        property.ExpressionBody is not null ||
                            property.Identifier.ValueText.EndsWith("SignatureContent", StringComparison.Ordinal),
                        Is.True,
                        $"{source}: JsonIgnore property '{property.Identifier.ValueText}' must be computed or signing-only.");
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

        private static bool HasJsonPropertyNameForIdentifier(PropertyDeclarationSyntax property)
            => property.AttributeLists
                .SelectMany(list => list.Attributes)
                .Where(attribute => string.Equals(attribute.Name.ToString(), "JsonPropertyName", StringComparison.Ordinal))
                .Select(attribute => attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression.ToString())
                .Any(value => value is not null && value.EndsWith("id\"", StringComparison.Ordinal));

        private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string attributeName)
            => attributeLists.SelectMany(list => list.Attributes)
                .Any(attribute => string.Equals(attribute.Name.ToString(), attributeName, StringComparison.Ordinal));
    }
}