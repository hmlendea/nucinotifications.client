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
    public sealed class AsyncConventionTests
    {
        private static readonly string[] BlockingAsyncMembers =
        [
            ".Result",
            ".Wait(",
            ".GetAwaiter("
        ];

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoBlockingAsyncCallsAreMade(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                string content = ArchitectureResources.ReadResource(sourcePrefix, source);

                // Narrowly scoped: this fixture declares the forbidden member names as data,
                // so it is excluded from its own textual scan.
                if (string.Equals(source, "AsyncConventionTests.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] blockingCalls = BlockingAsyncMembers
                    .Where(member => content.Contains(member, StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    blockingCalls,
                    Is.Empty,
                    $"{source}: asynchronous work must be awaited rather than blocked ({string.Join(", ", blockingCalls)}).");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenNoAsyncVoidMethodsAreDeclared(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                MethodDeclarationSyntax[] asyncVoidMethods = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AsyncKeyword)))
                    .Where(method => method.ReturnType is PredefinedTypeSyntax predefined &&
                        predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
                    .ToArray();

                Assert.That(asyncVoidMethods, Is.Empty, $"{source}: async methods must return a task, never void.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenCancellationTokensAreLastOrAbsent(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                IEnumerable<ParameterSyntax> misplacedTokens = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<BaseMethodDeclarationSyntax>()
                    .Where(method => method.ParameterList.Parameters.Count > 0)
                    .Where(method => method.ParameterList.Parameters
                        .Take(method.ParameterList.Parameters.Count - 1)
                        .Any(parameter => IsCancellationToken(parameter)))
                    .SelectMany(method => method.ParameterList.Parameters)
                    .Where(parameter => IsCancellationToken(parameter))
                    .Where(parameter => parameter.Parent is ParameterListSyntax list &&
                        list.Parameters.IndexOf(parameter) != list.Parameters.Count - 1);

                Assert.That(
                    misplacedTokens.Where(token => token is not null),
                    Is.Empty,
                    $"{source}: CancellationToken must be the last parameter.");
            }
        }

        [TestCaseSource(typeof(ArchitectureResources), nameof(ArchitectureResources.AnalyzedSourcePrefixes))]
        public void GivenSources_WhenChecked_ThenAsyncMethodsAwaitTheirWork(string sourcePrefix)
        {
            IReadOnlyList<string> sources = ArchitectureResources.GetResourcePaths(sourcePrefix);

            Assert.That(sources, Is.Not.Empty, $"No source files were found under '{sourcePrefix}'.");

            foreach (string source in sources)
            {
                MethodDeclarationSyntax[] asyncWithoutAwait = ParseTree(sourcePrefix, source)
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.AsyncKeyword)))
                    .Where(method => method.DescendantNodes().OfType<AwaitExpressionSyntax>().Any() is false)
                    .ToArray();

                Assert.That(
                    asyncWithoutAwait,
                    Is.Empty,
                    $"{source}: methods marked 'async' must await asynchronous work.");
            }
        }

        private static SyntaxTree ParseTree(string sourcePrefix, string source)
            => CSharpSyntaxTree.ParseText(
                ArchitectureResources.ReadResource(sourcePrefix, source),
                path: source);

        private static bool IsCancellationToken(ParameterSyntax parameter)
            => parameter.Type is not null &&
                string.Equals(parameter.Type.ToString(), "CancellationToken", StringComparison.Ordinal);
    }
}