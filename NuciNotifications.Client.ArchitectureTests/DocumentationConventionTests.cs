using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace NuciNotifications.Client.ArchitectureTests
{
    [TestFixture]
    public sealed class DocumentationConventionTests
    {
        private static readonly string[] RootDocuments =
        [
            "README.md",
            "ARCHITECTURE.md",
            "CONTRIBUTING.md",
            "SECURITY.md",
            "PRIVACY.md",
            "CODE_OF_CONDUCT.md"
        ];

        [Test]
        public void GivenDocumentation_WhenChecked_ThenEveryRepositoryRelativeLinkResolves()
        {
            List<string> repositoryFiles = GetRepositoryFiles();
            List<string> violations = [];

            Assert.That(repositoryFiles, Is.Not.Empty, "No repository files were embedded.");

            foreach (string document in GetDocumentPaths())
            {
                string content = ArchitectureResources.ReadResource(
                    ArchitectureResources.RepositoryFilePrefix,
                    document);

                foreach (string target in GetLinkTargets(content))
                {
                    if (IsExternalOrAnchor(target))
                    {
                        continue;
                    }

                    string resolved = ResolvePath(document, target);

                    if (ResolvesToRepositoryPath(resolved, repositoryFiles))
                    {
                        continue;
                    }

                    violations.Add($"{document} -> {target}");
                }
            }

            Assert.That(
                violations,
                Is.Empty,
                $"Repository-relative links must resolve: {string.Join("; ", violations)}.");
        }

        [Test]
        public void GivenDocumentation_WhenChecked_ThenTheRequiredRootDocumentsExist()
        {
            IReadOnlyList<string> documents = GetDocumentPaths();

            Assert.That(documents, Is.Not.Empty, "No documentation files were embedded.");

            Assert.That(
                RootDocuments.Where(required => documents.Contains(required, StringComparer.Ordinal)),
                Is.EquivalentTo(RootDocuments),
                "Every required root document must exist.");
        }

        [Test]
        public void GivenArchitectureDocument_WhenChecked_ThenItDocumentsItsPrincipalSections()
        {
            string content = ArchitectureResources.ReadResource(
                ArchitectureResources.RepositoryFilePrefix,
                "ARCHITECTURE.md");

            string[] requiredSections =
            [
                "Purpose",
                "Architectural Style",
                "Components",
                "Dependency Direction and Rules",
                "External Dependencies",
                "Testing and Verification"
            ];

            Assert.That(
                requiredSections.Where(section =>
                    content.Contains(section, StringComparison.Ordinal) is false),
                Is.Empty,
                "The architecture document must cover its principal sections.");
        }

        [Test]
        public void GivenDocumentation_WhenChecked_ThenNoSecretsOrPlaceholderCredentialsArePresent()
        {
            string[] forbiddenPatterns =
            [
                "-----BEGIN PRIVATE KEY-----",
                "-----BEGIN RSA PRIVATE KEY-----",
                "-----BEGIN OPENSSH PRIVATE KEY-----",
                "AKIA"
            ];

            foreach (string document in GetDocumentPaths())
            {
                string content = ArchitectureResources.ReadResource(
                    ArchitectureResources.RepositoryFilePrefix,
                    document);

                string[] findings = forbiddenPatterns
                    .Where(pattern => content.Contains(pattern, StringComparison.Ordinal))
                    .ToArray();

                Assert.That(
                    findings,
                    Is.Empty,
                    $"{document}: documentation must not contain real credentials ({string.Join(", ", findings)}).");
            }
        }

        private static IReadOnlyList<string> GetDocumentPaths()
            => GetRepositoryFiles()
                .Where(file => file.EndsWith(".md", StringComparison.Ordinal))
                .OrderBy(file => file, StringComparer.Ordinal)
                .ToArray();

        private static List<string> GetRepositoryFiles()
        {
            List<string> files = ArchitectureResources
                .GetResourcePaths(ArchitectureResources.RepositoryFilePrefix)
                .ToList();

            files.AddRange(GetSourceRepositoryPaths(
                ArchitectureResources.ProductionSourcePrefix,
                "NuciNotifications.Client"));
            files.AddRange(GetSourceRepositoryPaths(
                ArchitectureResources.UnitTestSourcePrefix,
                "NuciNotifications.Client.UnitTests"));
            files.AddRange(GetSourceRepositoryPaths(
                ArchitectureResources.ArchitectureTestSourcePrefix,
                "NuciNotifications.Client.ArchitectureTests"));

            return files
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        private static IEnumerable<string> GetSourceRepositoryPaths(string sourcePrefix, string projectFolder)
            => ArchitectureResources.GetResourcePaths(sourcePrefix)
                .Select(source => projectFolder + "/" + source);

        private static IEnumerable<string> GetLinkTargets(string content)
            => Regex.Matches(content, @"\]\((?<target>[^)\s]+)\)")
                .Select(match => match.Groups["target"].Value)
                .ToArray();

        private static bool IsExternalOrAnchor(string target)
            => target.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                target.StartsWith('#') ||
                target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

        private static string ResolvePath(string document, string target)
        {
            string documentDirectory = string.Empty;

            if (document.Contains('/', StringComparison.Ordinal))
            {
                documentDirectory = document.Substring(0, document.LastIndexOf('/'));
            }

            return ResolveSegments(documentDirectory, target);
        }

        private static string ResolveSegments(string documentDirectory, string target)
        {
            List<string> segments = [];

            if (documentDirectory.Length > 0)
            {
                segments.AddRange(documentDirectory.Split('/'));
            }

            foreach (string segment in target.Split('/'))
            {
                if (segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.Equals(segment, "..", StringComparison.Ordinal))
                {
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }

                    continue;
                }

                segments.Add(segment);
            }

            return string.Join('/', segments);
        }

        private static bool ResolvesToRepositoryPath(string resolvedPath, IReadOnlyList<string> repositoryFiles)
        {
            string withoutAnchor = resolvedPath;

            if (withoutAnchor.Contains('#', StringComparison.Ordinal))
            {
                withoutAnchor = withoutAnchor.Substring(0, withoutAnchor.IndexOf('#', StringComparison.Ordinal));
            }

            if (withoutAnchor.Length == 0)
            {
                return true;
            }

            return repositoryFiles.Any(file =>
                string.Equals(file, withoutAnchor, StringComparison.Ordinal) ||
                file.StartsWith(withoutAnchor + "/", StringComparison.Ordinal));
        }
    }
}