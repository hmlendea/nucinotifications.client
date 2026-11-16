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
    public sealed class CodingConventionTests
    {
        private static readonly string[] BclTypeNames =
        [
            "String",
            "Int32",
            "Int64",
            "Boolean",
            "Object",
            "Byte",
            "Char",
            "Double",
            "Single",
            "Decimal"
        ];

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenExplicitTypesAreUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                bool hasVar = ParseTree(sourcePrefix, source).GetRoot()
                    .DescendantNodes()
                    .OfType<VariableDeclarationSyntax>()
                    .Any(declaration => declaration.Type.IsVar);

                Assert.That(hasVar, Is.False, $"{source}: the 'var' keyword must not be used.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoTernariesAreUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                Assert.That(
                    ParseTree(sourcePrefix, source).GetRoot().DescendantNodes().OfType<ConditionalExpressionSyntax>(),
                    Is.Empty,
                    $"{source}: ternary expressions are not allowed.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoTuplesOrRecordsAreUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                SyntaxNode root = ParseTree(sourcePrefix, source).GetRoot();

                Assert.That(
                    root.DescendantNodes().Where(node =>
                        node.IsKind(SyntaxKind.TupleType) ||
                        node.IsKind(SyntaxKind.TupleExpression)),
                    Is.Empty,
                    $"{source}: tuples are not allowed.");

                Assert.That(
                    root.DescendantNodes().OfType<RecordDeclarationSyntax>(),
                    Is.Empty,
                    $"{source}: record types are not allowed.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoTopLevelStatementsAreUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                Assert.That(
                    ParseTree(sourcePrefix, source).GetRoot().DescendantNodes().OfType<GlobalStatementSyntax>(),
                    Is.Empty,
                    $"{source}: top-level statements are not allowed.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoPartialDeclarationsAreUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<TypeDeclarationSyntax> partialTypes = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Where(declaration => declaration.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.PartialKeyword)));

                Assert.That(partialTypes, Is.Empty, $"{source}: partial declarations are not allowed.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoInitAccessorsOrConstDeclarationsAreUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                SyntaxNode root = ParseTree(sourcePrefix, source).GetRoot();

                Assert.That(
                    root.DescendantNodes().OfType<AccessorDeclarationSyntax>()
                        .Where(accessor => accessor.IsKind(SyntaxKind.InitAccessorDeclaration)),
                    Is.Empty,
                    $"{source}: init-only accessors are not allowed.");

                Assert.That(
                    root.DescendantNodes().OfType<FieldDeclarationSyntax>()
                        .Where(field => field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword))),
                    Is.Empty,
                    $"{source}: 'const' declarations are not allowed; use static properties.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoRefOrOutParametersAreUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<ParameterSyntax> parameters = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ParameterSyntax>()
                    .Where(parameter => parameter.Modifiers.Any(modifier =>
                        modifier.IsKind(SyntaxKind.RefKeyword) ||
                        modifier.IsKind(SyntaxKind.OutKeyword)));

                Assert.That(parameters, Is.Empty, $"{source}: ref/out parameters are not allowed.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenEveryControlFlowBodyUsesBraces(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                SyntaxNode root = ParseTree(sourcePrefix, source).GetRoot();

                Assert.That(
                    root.DescendantNodes().OfType<IfStatementSyntax>()
                        .Where(statement => statement.Statement is not BlockSyntax),
                    Is.Empty,
                    $"{source}: every 'if' body must use braces.");

                Assert.That(
                    root.DescendantNodes().OfType<ForEachStatementSyntax>()
                        .Where(statement => statement.Statement is not BlockSyntax),
                    Is.Empty,
                    $"{source}: every 'foreach' body must use braces.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenMethodsAndConstructorsHaveAtMostSevenParameters(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                SyntaxNode root = ParseTree(sourcePrefix, source).GetRoot();

                Assert.That(
                    root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>()
                        .Where(method => method.ParameterList.Parameters.Count > 7),
                    Is.Empty,
                    $"{source}: methods and constructors must have at most seven parameters.");

                Assert.That(
                    root.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>()
                        .Where(lambda => lambda.ParameterList.Parameters.Count > 7),
                    Is.Empty,
                    $"{source}: lambdas must have at most seven parameters.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenBuiltInTypeAliasesAreUsed(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] typeNames = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Select(identifier => identifier.Identifier.ValueText)
                    .Where(name => BclTypeNames.Contains(name))
                    .Distinct()
                    .ToArray();

                Assert.That(
                    typeNames,
                    Is.Empty,
                    $"{source}: built-in aliases must be used instead of {string.Join(", ", typeNames)}.");
            }
        }

        [TestCase("ProductionSources/")]
        public void GivenProductionSources_WhenChecked_ThenNoInlineFullyQualifiedTypesAreUsed(string sourcePrefix)
        {
            string[] typeNames =
            [
                "System.String",
                "System.Int32",
                "System.Boolean",
                "System.Object",
                "System.Exception",
                "System.DateTime"
            ];

            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                string[] inlineTypes = typeNames
                    .Where(typeName => content.Contains(typeName, StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    inlineTypes,
                    Is.Empty,
                    $"{source}: fully qualified types must be imported instead of {string.Join(", ", inlineTypes)}.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenEveryUsingDirectiveIsAlphabetisedWithinItsGroup(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                string[] lines = ArchitectureResources.ReadResource(sourcePrefix, source)
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Split('\n');

                List<string> currentGroup = [];
                List<List<string>> groups = [];

                foreach (string line in lines)
                {
                    if (line.StartsWith("using ", StringComparison.Ordinal) && line.EndsWith(';'))
                    {
                        currentGroup.Add(line);
                        continue;
                    }

                    if (currentGroup.Count > 0)
                    {
                        groups.Add(currentGroup);
                        currentGroup = [];
                    }
                }

                if (currentGroup.Count > 0)
                {
                    groups.Add(currentGroup);
                }

                foreach (List<string> group in groups)
                {
                    List<string> directiveNames = group
                        .Select(directive => directive.Replace("using ", string.Empty, StringComparison.Ordinal).TrimEnd(';'))
                        .ToList();

                    List<string> orderedNames = directiveNames
                        .OrderBy(name => name, StringComparer.Ordinal)
                        .ToList();

                    Assert.That(
                        directiveNames,
                        Is.EqualTo(orderedNames),
                        $"{source}: using directives must be alphabetised within each group.");
                }
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenEveryMemberDeclaresExplicitAccessibility(string sourcePrefix)
        {
            foreach (string source in ArchitectureResources.GetResourcePaths(sourcePrefix))
            {
                IEnumerable<MemberDeclarationSyntax> membersWithoutAccessibility = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .Where(IsClassMember)
                    .Where(member => HasAccessibility(member) is false);

                Assert.That(
                    membersWithoutAccessibility,
                    Is.Empty,
                    $"{source}: every field, property, event, constructor, and method must declare accessibility.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static bool IsClassMember(MemberDeclarationSyntax member)
            => member.Parent is TypeDeclarationSyntax parent &&
                parent is not InterfaceDeclarationSyntax &&
                member is FieldDeclarationSyntax or
                    PropertyDeclarationSyntax or
                    EventFieldDeclarationSyntax or
                    ConstructorDeclarationSyntax or
                    MethodDeclarationSyntax;

        private static bool HasAccessibility(MemberDeclarationSyntax member)
        {
            SyntaxTokenList modifiers = member switch
            {
                FieldDeclarationSyntax field => field.Modifiers,
                PropertyDeclarationSyntax property => property.Modifiers,
                EventFieldDeclarationSyntax eventField => eventField.Modifiers,
                ConstructorDeclarationSyntax constructor => constructor.Modifiers,
                MethodDeclarationSyntax method => method.Modifiers,
                BaseTypeDeclarationSyntax type => type.Modifiers,
                _ => default
            };

            return modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.PublicKeyword) ||
                modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword));
        }
    }
}