using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NuciNotifications.Client.ArchitectureTests
{
    public static class ArchitectureResources
    {
        public static string ProductionSourcePrefix => "ProductionSources/";

        public static string UnitTestSourcePrefix => "UnitTestSources/";

        public static string ArchitectureTestSourcePrefix => "ArchitectureTestSources/";

        public static string RepositoryFilePrefix => "RepositoryFiles/";

        public static string ProjectFilePrefix => "ProjectFiles/";

        public static IEnumerable<string> SourcePrefixes =>
        [
            ProductionSourcePrefix,
            UnitTestSourcePrefix,
            ArchitectureTestSourcePrefix
        ];

        public static IEnumerable<string> AnalyzedSourcePrefixes =>
        [
            ProductionSourcePrefix,
            ArchitectureTestSourcePrefix
        ];

        public static string GetRootNamespace(string sourcePrefix)
        {
            if (string.Equals(sourcePrefix, ProductionSourcePrefix, StringComparison.Ordinal))
            {
                return "NuciNotifications.Client";
            }

            if (string.Equals(sourcePrefix, UnitTestSourcePrefix, StringComparison.Ordinal))
            {
                return "NuciNotifications.Client.UnitTests";
            }

            if (string.Equals(sourcePrefix, ArchitectureTestSourcePrefix, StringComparison.Ordinal))
            {
                return "NuciNotifications.Client.ArchitectureTests";
            }

            throw new ArgumentOutOfRangeException(nameof(sourcePrefix), sourcePrefix, "Unknown source prefix.");
        }

        public static string GetExpectedNamespace(string sourcePrefix, string source)
        {
            string rootNamespace = GetRootNamespace(sourcePrefix);
            string directory = GetDirectory(source);

            if (string.IsNullOrEmpty(directory))
            {
                return rootNamespace;
            }

            return rootNamespace + "." + directory.Replace('/', '.');
        }

        public static string GetFileStem(string source)
        {
            string fileName = source.Substring(source.LastIndexOf('/') + 1);

            return fileName.Substring(0, fileName.LastIndexOf('.'));
        }

        public static string GetDirectory(string source)
        {
            int separatorIndex = source.LastIndexOf('/');

            if (separatorIndex < 0)
            {
                return string.Empty;
            }

            return source.Substring(0, separatorIndex);
        }

        public static IReadOnlyList<string> GetResourcePaths(string prefix)
            => typeof(ArchitectureResources).Assembly
                .GetManifestResourceNames()
                .Select(Normalise)
                .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
                .Select(name => name.Substring(prefix.Length))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

        public static string ReadResource(string prefix, string relativePath)
        {
            string expectedName = Normalise(prefix + relativePath);

            foreach (string resourceName in typeof(ArchitectureResources).Assembly.GetManifestResourceNames())
            {
                if (string.Equals(Normalise(resourceName), expectedName, StringComparison.Ordinal) is false)
                {
                    continue;
                }

                Stream stream = typeof(ArchitectureResources).Assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"The embedded resource '{expectedName}' could not be opened.");

                using (stream)
                {
                    using StreamReader reader = new(stream);

                    return reader.ReadToEnd();
                }
            }

            throw new InvalidOperationException($"The embedded resource '{expectedName}' was not found.");
        }

        private static string Normalise(string resourceName)
            => resourceName.Replace('\\', '/');
    }
}