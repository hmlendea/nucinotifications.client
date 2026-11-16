using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.NUnit;

using NUnit.Framework;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace NuciNotifications.Client.ArchitectureTests
{
    [TestFixture]
    public sealed class DependencyBoundaryTests
    {
        private static readonly string[] AllowedNonSystemAssemblyPrefixes =
        [
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Primitives",
            "Microsoft.Net.Http.Headers",
            "NuciAPI",
            "NuciExtensions",
            "NuciSecurity.HMAC"
        ];

        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenOnlyTheProductionAssemblyIsLoaded()
            => Assert.That(
                ProductionArchitecture.Model.Assemblies.Select(assembly => assembly.Name),
                Is.EquivalentTo(new[] { ProductionArchitecture.Assembly.GetName().Name }));

        [Test]
        public void GivenProductionAssembly_WhenLoaded_ThenItExposesTheDocumentedPublicTypes()
        {
            string[] typeNames = ProductionArchitecture.Assembly.GetTypes()
                .Where(type => type.IsPublic)
                .Select(GetTypeName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(typeNames, Is.Not.Empty, "The production assembly must expose public types.");

            Assert.That(typeNames, Is.EquivalentTo(new[]
            {
                "NuciNotifications.Client.Configuration.NuciNotificationsSettings",
                "NuciNotifications.Client.INuciNotificationsClient",
                "NuciNotifications.Client.NuciNotificationsClient",
                "NuciNotifications.Client.Requests.EmailAttachmentRequest",
                "NuciNotifications.Client.Requests.SendEmailRequest",
                "NuciNotifications.Client.ServiceCollectionExtensions"
            }));
        }

        [TestCase("Microsoft.AspNetCore")]
        [TestCase("Microsoft.EntityFrameworkCore")]
        [TestCase("Amazon.DynamoDBv2")]
        [TestCase("Dapper")]
        [TestCase("MediatR")]
        [TestCase("Microsoft.Extensions.Logging")]
        public void GivenProductionAssembly_WhenCheckingExternalDependencies_ThenPersistenceAndHostingFrameworksAreAbsent(
            string forbiddenAssembly)
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");

            Assert.That(
                references.Select(GetReferenceName)
                    .Where(reference => reference.StartsWith(forbiddenAssembly, StringComparison.OrdinalIgnoreCase)),
                Is.Empty,
                $"The production assembly must not reference {forbiddenAssembly}.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingExternalDependencies_ThenOnlyDocumentedDependenciesAreReferenced()
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");

            Assert.That(
                references.Select(GetReferenceName).Where(IsUndocumentedReference),
                Is.Empty,
                "The production assembly must only reference the documented dependencies.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingReferences_ThenTestDependenciesAreAbsent()
        {
            AssemblyName[] references = ProductionArchitecture.Assembly.GetReferencedAssemblies();

            Assert.That(references, Is.Not.Empty, "The production assembly references must be inspected.");

            Assert.That(
                references.Select(GetReferenceName).Where(IsTestDependency),
                Is.Empty,
                "The production assembly must remain independent of test assemblies and frameworks.");
        }

        [Test]
        public void GivenProductionAssembly_WhenCheckingFriendAssemblies_ThenInternalsAreNotExposed()
            => Assert.That(
                ProductionArchitecture.Assembly.GetCustomAttributesData()
                    .Where(attribute => Type.Equals(
                        attribute.AttributeType,
                        typeof(InternalsVisibleToAttribute))),
                Is.Empty,
                "The production assembly must not expose internals to friend assemblies.");

        [Test]
        public void GivenProductionTypes_WhenCheckingPlacement_ThenEveryTypeResidesInADocumentedNamespace()
        {
            AssertHasProductionTypes();

            Types().That().Are(ProductionArchitecture.ProductionTypes)
                .Should().ResideInNamespaceMatching(@"^NuciNotifications\.Client(\.Configuration|\.Requests)?$")
                .Because("every production type must live in one of the documented namespaces")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenProductionTypes_WhenCheckingPersistenceDependencies_ThenNoPersistenceTechnologyIsReferenced()
        {
            AssertHasProductionTypes();

            Types().That().Are(ProductionArchitecture.ProductionTypes)
                .Should().NotDependOnAny(Types().That().ResideInNamespace("Microsoft.EntityFrameworkCore"))
                .Because("the client library performs no persistence")
                .Check(ProductionArchitecture.Model);

            Types().That().Are(ProductionArchitecture.ProductionTypes)
                .Should().NotDependOnAny(Types().That().ResideInNamespace("Amazon.DynamoDBv2"))
                .Because("the client library performs no persistence")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenRequestTypes_WhenCheckingDependencies_ThenTheyDoNotDependOnTheClientFacade()
        {
            AssertHasProductionTypes();

            IObjectProvider<IType> requestTypes = Types()
                .That()
                .ResideInNamespace("NuciNotifications.Client.Requests");

            AssertHasTypes(requestTypes, "the request contract boundary");

            Types().That().Are(requestTypes)
                .Should().NotDependOnAny(Types().That().HaveFullName("NuciNotifications.Client.NuciNotificationsClient"))
                .Because("request contracts are data and validation objects and must not depend on the facade")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenFacadeTypes_WhenCheckingDependencies_ThenSettingsAreTheOnlyConfigurationDependency()
        {
            AssertHasProductionTypes();

            IObjectProvider<IType> facadeTypes = Types()
                .That()
                .HaveFullName("NuciNotifications.Client.NuciNotificationsClient");

            AssertHasTypes(facadeTypes, "the facade boundary");

            Types().That().Are(facadeTypes)
                .Should().DependOnAny(Types().That().HaveFullName(
                    "NuciNotifications.Client.Requests.SendEmailRequest"))
                .Because("the facade constructs the documented request contract")
                .Check(ProductionArchitecture.Model);

            Types().That().Are(facadeTypes)
                .Should().DependOnAny(Types().That().HaveFullName(
                    "NuciNotifications.Client.Configuration.NuciNotificationsSettings"))
                .Because("the facade reads the documented settings")
                .Check(ProductionArchitecture.Model);
        }

        [Test]
        public void GivenClientTypes_WhenCheckingPublicMethods_ThenEveryEligibleClassHasAColocatedInterface()
        {
            Type[] eligibleClasses = ProductionArchitecture.Assembly.GetTypes()
                .Where(type => type.IsClass && type.IsPublic && !type.IsAbstract && !type.IsSealed)
                .Where(type => IsServiceRole(type.Namespace))
                .Where(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length > 0)
                .ToArray();

            Assert.That(eligibleClasses, Is.Not.Empty, "The production assembly must contain eligible classes.");

            string[] missingInterfaces = eligibleClasses
                .Where(type => ProductionArchitecture.Assembly.GetType(GetInterfaceName(type)) is null)
                .Select(GetTypeName)
                .ToArray();

            Assert.That(
                missingInterfaces,
                Is.Empty,
                "Every non-static public service class with public methods must have a colocated I-prefixed interface.");
        }

        private static void AssertHasProductionTypes()
            => AssertHasTypes(ProductionArchitecture.ProductionTypes, "the production boundary");

        private static void AssertHasTypes(IObjectProvider<IType> selection, string boundaryName)
            => Assert.That(
                selection.GetObjects(ProductionArchitecture.Model),
                Is.Not.Empty,
                $"The {boundaryName} must select production types.");

        private static string GetTypeName(Type type)
        {
            if (type.FullName is null)
            {
                return type.Name;
            }

            return type.FullName;
        }

        private static bool IsServiceRole(string? namespaceName)
            => namespaceName is not null &&
                namespaceName.StartsWith("NuciNotifications.Client", StringComparison.Ordinal) &&
                namespaceName.EndsWith(".Requests", StringComparison.Ordinal) is false &&
                namespaceName.EndsWith(".Configuration", StringComparison.Ordinal) is false;

        private static string GetInterfaceName(Type type)
        {
            if (type.FullName is null)
            {
                return "I" + type.Name;
            }

            int separatorIndex = type.FullName.LastIndexOf('.');

            if (separatorIndex < 0)
            {
                return "I" + type.Name;
            }

            return type.FullName.Substring(0, separatorIndex + 1) + "I" + type.Name;
        }

        private static string GetReferenceName(AssemblyName reference)
        {
            if (reference.Name is null)
            {
                return string.Empty;
            }

            return reference.Name;
        }

        private static bool IsUndocumentedReference(string reference)
        {
            if (reference.StartsWith("System", StringComparison.Ordinal) ||
                string.Equals(reference, "mscorlib", StringComparison.Ordinal) ||
                string.Equals(reference, "netstandard", StringComparison.Ordinal))
            {
                return false;
            }

            return !AllowedNonSystemAssemblyPrefixes
                .Any(allowed => reference.StartsWith(allowed, StringComparison.Ordinal));
        }

        private static bool IsTestDependency(string reference)
            => reference.Contains("Tests", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("NUnit", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("Moq", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("ArchUnit", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
                reference.Contains("coverlet", StringComparison.OrdinalIgnoreCase);
    }
}