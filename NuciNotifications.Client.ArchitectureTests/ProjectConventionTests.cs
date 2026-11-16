using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace NuciNotifications.Client.ArchitectureTests
{
    [TestFixture]
    public sealed class ProjectConventionTests
    {
        private static string ProductionProjectPath => "NuciNotifications.Client/NuciNotifications.Client.csproj";

        private static string UnitTestProjectPath => "NuciNotifications.Client.UnitTests/NuciNotifications.Client.UnitTests.csproj";

        private static string ArchitectureTestProjectPath => "NuciNotifications.Client.ArchitectureTests/NuciNotifications.Client.ArchitectureTests.csproj";

        private static IEnumerable<string> ProjectPaths =>
        [
            ProductionProjectPath,
            UnitTestProjectPath,
            ArchitectureTestProjectPath
        ];

        [TestCaseSource(nameof(ProjectPaths))]
        public void GivenProjects_WhenLoaded_ThenTheRequiredCompilerPropertiesAreDeclared(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            Assert.That(
                GetProperty(project, "TargetFramework"),
                Is.EqualTo("net10.0"),
                $"{projectPath}: the target framework must be net10.0.");

            Assert.That(
                GetProperty(project, "ImplicitUsings"),
                Is.EqualTo("disable"),
                $"{projectPath}: implicit usings must be disabled.");

            Assert.That(
                GetProperty(project, "LangVersion"),
                Is.EqualTo("latest"),
                $"{projectPath}: the latest language version must be used.");

            Assert.That(
                GetProperty(project, "Nullable"),
                Is.EqualTo("enable"),
                $"{projectPath}: nullable references must be explicitly enabled.");
        }

        [TestCaseSource(nameof(ProjectPaths))]
        public void GivenProjects_WhenLoaded_ThenPackageReferencesAreAlphabetical(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            string[] packages = project.Descendants("PackageReference")
                .Select(reference => GetAttributeValue(reference, "Include"))
                .ToArray();

            Assert.That(packages, Is.Not.Empty, $"{projectPath}: no package references were found.");

            Assert.That(
                packages,
                Is.EqualTo(packages.OrderBy(package => package, StringComparer.Ordinal).ToArray()),
                $"{projectPath}: package references must be ordered alphabetically.");
        }

        [TestCase("NuciNotifications.Client.UnitTests/NuciNotifications.Client.UnitTests.csproj")]
        [TestCase("NuciNotifications.Client.ArchitectureTests/NuciNotifications.Client.ArchitectureTests.csproj")]
        public void GivenTestProjects_WhenLoaded_ThenTheTestFlagsAreDeclared(string projectPath)
        {
            XDocument project = LoadProject(projectPath);

            Assert.That(
                GetProperty(project, "IsTestProject"),
                Is.EqualTo("true"),
                $"{projectPath}: the test project flag must be declared.");

            Assert.That(
                GetProperty(project, "ExcludeFromCodeCoverage"),
                Is.EqualTo("true"),
                $"{projectPath}: the code coverage exclusion flag must be declared.");

            Assert.That(
                GetProperty(project, "IsPackable"),
                Is.EqualTo("false"),
                $"{projectPath}: test projects must not be packable.");
        }

        [Test]
        public void GivenProductionProject_WhenLoaded_ThenItIsPackable()
            => Assert.That(
                GetProperty(LoadProject(ProductionProjectPath), "IsPackable"),
                Is.Not.EqualTo("false"),
                "The production project is published as a NuGet package and must be packable.");

        [Test]
        public void GivenProductionProject_WhenLoaded_ThenItDoesNotReferenceTestProjects()
        {
            string[] testReferences = GetProjectReferences(LoadProject(ProductionProjectPath))
                .Where(reference => reference.Contains("Tests", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            Assert.That(testReferences, Is.Empty, "The production project must not reference test projects.");
        }

        [Test]
        public void GivenProductionProject_WhenLoaded_ThenItDoesNotDeclareFriendAssemblies()
            => Assert.That(
                LoadProject(ProductionProjectPath).Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "InternalsVisibleTo", StringComparison.Ordinal)),
                Is.Empty,
                "The production project must not declare InternalsVisibleTo.");

        [TestCase("NuciNotifications.Client.UnitTests/NuciNotifications.Client.UnitTests.csproj")]
        [TestCase("NuciNotifications.Client.ArchitectureTests/NuciNotifications.Client.ArchitectureTests.csproj")]
        public void GivenTestProjects_WhenLoaded_ThenTheyReferenceTheProductionProject(string projectPath)
            => Assert.That(
                GetProjectReferences(LoadProject(projectPath)).Select(NormaliseReference),
                Has.Some.EqualTo(NormaliseReference("../NuciNotifications.Client/NuciNotifications.Client.csproj")),
                $"{projectPath}: the test project must reference the production project.");

        [Test]
        public void GivenArchitectureTestProject_WhenLoaded_ThenItReferencesTheProductionProject()
            => Assert.That(
                GetProjectReferences(LoadProject(ArchitectureTestProjectPath)).Select(NormaliseReference),
                Has.Some.EqualTo(NormaliseReference("../NuciNotifications.Client/NuciNotifications.Client.csproj")),
                "The architecture test project must reference the production project.");

        private static XDocument LoadProject(string relativePath)
            => XDocument.Parse(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProjectFilePrefix,
                    relativePath));

        private static string[] GetProjectReferences(XDocument project)
            => project.Descendants("ProjectReference")
                .Select(reference => GetAttributeValue(reference, "Include"))
                .ToArray();

        private static string NormaliseReference(string reference)
            => reference.Replace('\\', '/');

        private static string GetAttributeValue(XElement element, string attributeName)
        {
            XAttribute? attribute = element.Attribute(attributeName);

            if (attribute is null)
            {
                return string.Empty;
            }

            return attribute.Value;
        }

        private static string GetProperty(XDocument project, string name)
        {
            XElement? element = project.Descendants()
                .FirstOrDefault(candidate => string.Equals(candidate.Name.LocalName, name, StringComparison.Ordinal));

            if (element is null)
            {
                return string.Empty;
            }

            return element.Value;
        }
    }
}