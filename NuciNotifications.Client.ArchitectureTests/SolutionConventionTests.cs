using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using NUnit.Framework;

namespace NuciNotifications.Client.ArchitectureTests
{
    [TestFixture]
    public sealed class SolutionConventionTests
    {
        private static string SolutionFile => "NuciNotifications.Client.slnx";

        [Test]
        public void GivenSolution_WhenLoaded_ThenItUsesTheSdkStyleFormat()
        {
            XDocument solution = LoadSolution();
            string content = ArchitectureResources.ReadResource(
                ArchitectureResources.ProjectFilePrefix,
                SolutionFile);

            Assert.That(solution.Root, Is.Not.Null, "The solution file must have a root element.");

            Assert.That(
                solution.Root!.Name.LocalName,
                Is.EqualTo("Solution"),
                "The solution file must use the SDK-style <Solution> root element.");

            Assert.That(solution.Root.Attributes(), Is.Empty, "The <Solution> element must not declare attributes.");

            Assert.That(
                content.TrimStart().StartsWith("<?xml", StringComparison.Ordinal),
                Is.False,
                "The solution file must not declare an XML declaration.");

            Assert.That(
                solution.Descendants().Where(element =>
                    string.Equals(element.Name.LocalName, "Version", StringComparison.Ordinal)),
                Is.Empty,
                "The solution file must not contain a <Version> element.");
        }

        [Test]
        public void GivenSolution_WhenLoaded_ThenEveryProjectDeclaresOnlyItsPath()
        {
            XElement[] projects = GetProjects();

            Assert.That(projects, Is.Not.Empty, "The solution file must declare projects.");

            foreach (XElement project in projects)
            {
                Assert.That(
                    project.Attributes().Select(attribute => attribute.Name.LocalName),
                    Is.EqualTo(new[] { "Path" }),
                    "Each <Project> element must declare only the Path attribute.");
            }
        }

        [Test]
        public void GivenSolution_WhenLoaded_ThenTheProjectsAreInTheDocumentedOrder()
        {
            string[] paths = GetProjects()
                .Select(project => GetAttributeValue(project, "Path").Replace('\\', '/'))
                .ToArray();

            Assert.That(paths, Is.EqualTo(new[]
            {
                "NuciNotifications.Client/NuciNotifications.Client.csproj",
                "NuciNotifications.Client.ArchitectureTests/NuciNotifications.Client.ArchitectureTests.csproj",
                "NuciNotifications.Client.UnitTests/NuciNotifications.Client.UnitTests.csproj"
            }));
        }

        [Test]
        public void GivenSolution_WhenLoaded_ThenProjectsResideInSiblingFoldersNamedAfterThemselves()
        {
            foreach (XElement project in GetProjects())
            {
                string path = GetAttributeValue(project, "Path").Replace('\\', '/');
                string[] segments = path.Split('/');

                Assert.That(segments, Has.Length.EqualTo(2), $"{path}: the project must reside in a sibling folder.");

                Assert.That(
                    segments[1],
                    Is.EqualTo(segments[0] + ".csproj"),
                    $"{path}: the project folder must be named after the project file.");
            }
        }

        private static XElement[] GetProjects()
            => LoadSolution()
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "Project", StringComparison.Ordinal))
                .ToArray();

        private static XDocument LoadSolution()
            => XDocument.Parse(
                ArchitectureResources.ReadResource(
                    ArchitectureResources.ProjectFilePrefix,
                    SolutionFile));

        private static string GetAttributeValue(XElement element, string attributeName)
        {
            XAttribute? attribute = element.Attribute(attributeName);

            if (attribute is null)
            {
                return string.Empty;
            }

            return attribute.Value;
        }
    }
}