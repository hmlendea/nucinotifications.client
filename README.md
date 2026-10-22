[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/nucinotifications.client)](https://github.com/hmlendea/nucinotifications.client/releases/latest)
[![Build Status](https://github.com/hmlendea/nucinotifications.client/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/nucinotifications.client/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/NuciNotifications.Client)](https://nuget.org/packages/NuciNotifications.Client)
[![License](https://img.shields.io/github/license/hmlendea/nucinotifications.client)](https://github.com/hmlendea/nucinotifications.client/blob/master/LICENSE)

# NuciNotifications.Client

NuciNotifications.Client is a .NET 10 library for asynchronous e-mail submission, Bearer authentication, HMAC signing, and optional ordered attachments.

> **Project status:** Attachments are implemented in this working tree. The latest verified published package is 1.1.2; do not presume it includes these changes.

## 📑 Table of Contents

- [Table of Contents](#-table-of-contents)
- [Capabilities](#-capabilities)
- [Use Cases](#-use-cases)
- [Usage](#-usage)
    - [Examples](#examples)
- [Known Limitations](#-known-limitations)
- [Installation](#-installation)
    - [Package Manager Installation](#package-manager-installation)
    - [Manual Installation](#manual-installation)
- [Configuration](#-configuration)
    - [Settings](#settings)
    - [Environment Variables](#environment-variables)
    - [Secret Management](#secret-management)
    - [Validation](#validation)
- [Compatibility](#-compatibility)
- [Integrations](#-integrations)
- [Authentication and Authorisation](#-authentication-and-authorisation)
- [Privacy and Data](#-privacy-and-data)
- [Development](#-development)
    - [Requirements](#requirements)
    - [Setup](#setup)
    - [Build](#build)
    - [Test](#test)
    - [Continuous Integration](#continuous-integration)
    - [Release](#release)
    - [Dependencies](#dependencies)
- [GitHub Actions](#-github-actions)
- [Project Structure](#-project-structure)
    - [Projects and Packages](#projects-and-packages)
    - [Directories](#directories)
- [Architecture](#-architecture)
- [Documentation](#-documentation)
- [API Reference](#-api-reference)
- [Data Formats and Protocols](#-data-formats-and-protocols)
- [Troubleshooting](#-troubleshooting)
- [Contributing](#-contributing)
- [Code of Conduct](#-code-of-conduct)
- [Related Projects](#-related-projects)
- [Security](#-security)
- [Support](#-support)
- [Project Engagement](#-project-engagement)
- [License](#-license)

## ✨ Capabilities

- **Async email sending** - Preserve minimal and explicit-sender calls, with optional ordered file attachments.
- **Bearer token authentication** — `ApiKey` sent as `Authorization: Bearer` header
- **HMAC request signing** — `HmacSharedSecretKey` used to sign requests for integrity and authenticity
- **Dependency injection ready** — `INuciNotificationsClient` interface, `ServiceCollectionExtensions` for config binding and singleton registration
- **Strongly-typed requests** — `SendEmailRequest` DTO with `HmacOrder` attributes for deterministic signing
- **Input validation** — Invalid message fields and attachments produce argument exceptions prior to transport.
- **Transport error translation** — Transport exceptions and unsuccessful API responses produce `SmtpException`.
- **Host-controlled secrets** — The library contains no logging; callers control credential storage and exception reporting.

## 🎯 Use Cases

- **Application notifications:** Send transactional emails (welcome, password reset, alerts) from .NET services
- **Microservice communication:** Lightweight notification client for distributed systems
- **Secure API integration:** HMAC-signed requests for tamper-proof notification delivery

## 🚀 Usage

Configure the host's `NuciNotificationsSettings` section using protected credential sources, then register services in its composition root:
```csharp
using NuciNotifications.Client;
using NuciNotifications.Client.Configuration;

builder.Services.AddNuciNotificationsSettings(builder.Configuration);
builder.Services.AddSingleton<INuciNotificationsClient>(serviceProvider =>
    new NuciNotificationsClient(serviceProvider.GetRequiredService<NuciNotificationsSettings>()));
```

**3. Send an email**:
```csharp
public sealed class NotificationsService(INuciNotificationsClient notificationsClient)
{
    public async Task SendWelcomeEmailAsync(string recipient)
    {
        await notificationsClient.SendEmail(
            recipient,
            "Welcome",
            "Welcome to our service.");
    }
}
```

### Examples

With explicit sender name:
```csharp
await notificationsClient.SendEmail(
    "Your App Name",
    "user@example.com",
    "Subject",
    "Message body");
```

For optional attachments, use the five-argument overload. Pass `null` as the sender name for the default sender:
```csharp
using NuciNotifications.Client.Requests;

await notificationsClient.SendEmail(
    null,
    "user@example.com",
    "Report",
    "Please consult the attached report.",
    [new EmailAttachmentRequest { FileName = "report.txt", Content = "SGVsbG8=" }]);
```

`Content` contains Base64-encoded bytes, not a file path. File names must contain 1-255 characters without directory separators or control characters. Null or empty attachment collections send no files. Invalid attachments produce `ArgumentException` before transport. Attachment names, contents, and order participate in HMAC signing; the signing-only property is excluded from JSON. Use an API deployment supporting this attachment contract. Existing three- and four-argument calls remain unchanged and unambiguous.

## ⚠️ Known Limitations

- Attachments require a matching API request/signing contract; no minimum server version is verified.
- No file reading, streaming, malware scanning, or payload-size enforcement is implemented.
- No built-in retry, delivery receipt, scheduling, or batch API is implemented.
- HTTPS is recommended but not enforced by this library. Base64 is encoding, not encryption.

## 📦 Installation

[![Obtain it from NuGet](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/nuget.png)](https://nuget.org/packages/NuciNotifications.Client)
[![Obtain it from GitHub](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/github.png)](https://github.com/hmlendea/nucinotifications.client/releases)

### Package Manager Installation

```bash
dotnet add package NuciNotifications.Client
```

Or, via the `Package Manager Console`:
```powershell
Install-Package NuciNotifications.Client
```

### Manual Installation

Download the `.nupkg` from a [published GitHub release](https://github.com/hmlendea/nucinotifications.client/releases), place it in a local NuGet source, and install `NuciNotifications.Client` from that source. Published 1.1.2 artefacts do not establish attachment availability.

## ⚙️ Configuration

The [settings contract](NuciNotifications.Client/Configuration/NuciNotificationsSettings.cs) can be constructed directly or bound from the `NuciNotificationsSettings` section. The host chooses configuration providers and their precedence; no configuration file is shipped by the library.

### Settings

The subsequent settings are recognised:
| Section | Key | Type | Default | Required | Description |
|---------|-----|------|---------|----------|-------------|
| `NuciNotificationsSettings` | `BaseUrl` | `string` | — | Yes | Base URL of the NuciNotifications API (e.g., `https://api.example.com`) |
| `NuciNotificationsSettings` | `ApiKey` | `string` | — | Yes | Bearer token for API authorization |
| `NuciNotificationsSettings` | `HmacSharedSecretKey` | `string` | — | Yes | Shared secret for HMAC request signing |

### Environment Variables

The subsequent environment variables can be set:
| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `NuciNotificationsSettings__BaseUrl` | Yes | — | Base URL of the NuciNotifications API |
| `NuciNotificationsSettings__ApiKey` | Yes | — | Bearer token for API authorization |
| `NuciNotificationsSettings__HmacSharedSecretKey` | Yes | — | Shared secret for HMAC request signing |

### Secret Management

Store secrets externally and bind via configuration providers:
- **User Secrets (Development):** `dotnet user-secrets set "NuciNotificationsSettings:BaseUrl" "..."`
- **Azure Key Vault / AWS Secrets Manager:** Use respective configuration providers
- **Environment Variables:** As shown above

### Validation

The library does **not** perform automatic validation at startup. Validation occurs at runtime when sending emails. Recommended startup validation:

```csharp
var settings = builder.Configuration.GetSection("NuciNotificationsSettings").Get<NuciNotificationsSettings>();
if (string.IsNullOrWhiteSpace(settings?.BaseUrl))
    throw new InvalidOperationException("NuciNotificationsSettings:BaseUrl is required");
if (string.IsNullOrWhiteSpace(settings?.ApiKey))
    throw new InvalidOperationException("NuciNotificationsSettings:ApiKey is required");
if (string.IsNullOrWhiteSpace(settings?.HmacSharedSecretKey))
    throw new InvalidOperationException("NuciNotificationsSettings:HmacSharedSecretKey is required");
if (!Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _))
    throw new InvalidOperationException("NuciNotificationsSettings:BaseUrl must be a valid absolute URI");
```

## 🧩 Compatibility

| Component | Supported Versions | Notes |
|-----------|--------------------|-------|
| .NET | 10.0 | Target framework |
| NuciNotifications API | Matching `/Email` request and HMAC contract | Verify deployed server support for attachments |

## 🔌 Integrations

| Integration | Compatibility | Purpose | Required |
|-------------|---------------|---------|----------|
| Configured NuciNotifications API | Matching `/Email` contract | Notification submission | Yes |
| Microsoft configuration/options and DI | Direct packages 10.0.13 | Settings-binding helper | No; construct settings directly if preferred |

## 🔐 Authentication and Authorisation

`ApiKey` supplies the Bearer token and `HmacSharedSecretKey` supplies request signing via the transport. Server permissions and signature verification are controlled by the API; this repository does not define roles or scopes. The library contains no logging, but callers must protect credentials and transport/API error details.

## 🛡️ Privacy and Data

NuciNotifications.Client processes and transmits caller-provided message fields and optional attachments to the configured API without implementing local persistence or telemetry.

For the detailed description of how the application handles privacy and personal data, see [PRIVACY.md](./PRIVACY.md).

The host controls input selection and credentials; the API operator controls subsequent delivery and persistence.

| Data | Purpose | Storage | Retention | Optional |
|------|---------|---------|-----------|----------|
| Recipient, subject, body | E-mail submission | Client memory; configured API | No library persistence; external policy applies | No |
| Sender name | Sender selection | Client memory; configured API | External policy applies | Yes |
| Attachment names and Base64 contents | File submission and signing | Client memory; configured API | External policy applies | Yes |
| Credentials | Bearer authentication and HMAC | Host settings and transport memory | Host-controlled | No for authenticated submission |

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git for source checkout.

### Setup

```bash
git clone https://github.com/hmlendea/nucinotifications.client.git
cd nucinotifications.client
dotnet restore NuciNotifications.Client.slnx
```

### Build

```bash
dotnet build NuciNotifications.Client.slnx --no-restore
```

### Test

```bash
dotnet test NuciNotifications.Client.slnx --no-build
```

### Continuous Integration

The [.NET workflow](.github/workflows/dotnet.yml) restores, builds, and tests on pushes and pull requests to `master`. The commands above reproduce those principal checks. Unit tests use a mock transport; no live API is required.

### Release

Publishing a GitHub release activates both packaging workflows. They derive the package version from the tag without its `v` prefix and run `dotnet pack -c Release --output ./nupkg -p:Version=...`. The [GitHub release workflow](.github/workflows/github-release.yml) uploads the package and appends its SHA256 checksum; the [NuGet release workflow](.github/workflows/nuget-release.yml) uses OIDC for publication. No release helper script exists in this repository.

### Dependencies

| Package | Version | Scope | Purpose |
|---------|---------|-------|---------|
| NuciAPI.Client | 1.2.4 | Runtime | HTTP and signing integration |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.13 | Runtime | DI registration contracts |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.13 | Runtime | Settings binding |
| NUnit / NUnit3TestAdapter | 5.0.0 / 6.3.1 | Development | Test framework and adapter |
| Moq / FluentAssertions | 4.21.0 / 8.11.0 | Development | Mock transport and assertions |
| Microsoft.NET.Test.Sdk | 18.11.0 | Development | Test execution |
| Microsoft.Extensions.DependencyInjection | 10.0.13 | Development | Test service container |

## ⚙️ GitHub Actions

| Workflow | Purpose | What it does |
|----------|---------|--------------|
| [ .NET ](https://github.com/hmlendea/nucinotifications.client/blob/master/.github/workflows/dotnet.yml) | Validation | Restore, build, and test |
| [ GitHub Release ](https://github.com/hmlendea/nucinotifications.client/blob/master/.github/workflows/github-release.yml) | Release artefacts | Pack library, upload package, append checksum |
| [ NuGet.org Release ](https://github.com/hmlendea/nucinotifications.client/blob/master/.github/workflows/nuget-release.yml) | Package publication | Pack and publish using OIDC |

## 🗂️ Project Structure

The [solution](NuciNotifications.Client.slnx) contains the library and its unit tests.

### Projects and Packages

| Project | Type | Purpose |
|---------|------|---------|
| [Client project](NuciNotifications.Client/NuciNotifications.Client.csproj) | .NET 10 library | Notification facade and request contracts |
| [Unit-test project](NuciNotifications.Client.UnitTests/NuciNotifications.Client.UnitTests.csproj) | NUnit tests | Validation, signing, transport, and configuration checks |

### Directories

| Directory | Purpose |
|-----------|---------|
| [Requests](NuciNotifications.Client/Requests/) | Message and attachment DTOs |
| [Configuration](NuciNotifications.Client/Configuration/) | Settings model |
| [Documentation](docs/) | Detailed technical reference |

## 🏗️ Architecture

NuciNotifications.Client separates the public facade, request validation/signing contracts, settings composition, and replaceable HTTP transport.

See the [architecture documentation](ARCHITECTURE.md) for the system context, principal components, runtime flows, ownership boundaries, dependencies, constraints, and extension points.

## 📚 Documentation

The [documentation index](docs/README.md) provides detailed technical references.

| Resource | Description |
|----------|-------------|
| [Architecture](ARCHITECTURE.md) | System architecture and component overview |
| [Components](docs/COMPONENTS.md) | Detailed component reference |
| [Execution Flows](docs/EXECUTION_FLOWS.md) | End-to-end flow diagrams and descriptions |
| [Dependencies](docs/DEPENDENCIES.md) | Dependency graph and version compatibility |
| [Configuration](docs/CONFIGURATION.md) | Configuration options and examples |
| [Error Handling](docs/ERROR_HANDLING.md) | Exception types, scenarios, and retry guidance |
| [Security](SECURITY.md) | Security model, threat model, and vulnerability reporting |
| [Privacy](PRIVACY.md) | Data processing and privacy information |

## 🌐 API Reference

Consult the [component reference](docs/COMPONENTS.md), [public interface](NuciNotifications.Client/INuciNotificationsClient.cs), and [attachment DTO](NuciNotifications.Client/Requests/EmailAttachmentRequest.cs).

## 🧾 Data Formats and Protocols

| Format or Protocol | Specification | Purpose |
|--------------------|---------------|---------|
| `/Email` request | [Request DTO](NuciNotifications.Client/Requests/SendEmailRequest.cs) | Message fields and ordered attachments |
| Attachment signing input | [Architecture](ARCHITECTURE.md#-compatibility-contracts) | Ordered Web JSON; zero-length string for absent files |

## 🩺 Troubleshooting

| Symptom | Probable Cause | Resolution |
|---------|----------------|------------|
| Local `ArgumentException` for attachments | Null item, invalid name, or invalid Base64 | Correct input; no request is dispatched |
| `SmtpException` with an inner exception | Transport exception | Inspect details securely; verify connectivity and endpoint |
| `SmtpException` with API message | Unsuccessful API response | Verify credentials and signing-contract agreement |

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Preserve the existing public contract unless a breaking change is intentional
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Include tests for additional or modified functionality
- Raise a new [issue](https://github.com/hmlendea/nucinotifications.client/issues) for problems or suggestions

See the [contributing guidelines](./CONTRIBUTING.md) for details on how to report issues, suggest enhancements, and submit changes.

## 📜 Code of Conduct

See the [Code of Conduct](./CODE_OF_CONDUCT.md) for the standards of conduct expected in all project spaces.

## 🔗 Related Projects

- [NuciNotifications API](https://github.com/hmlendea/nucinotifications-api): notification service integrated by this client; confirm deployment compatibility for attachments.

## 🔒 Security

NuciNotifications.Client validates attachment metadata and content encoding and delegates authenticated transport to `NuciAPI.Client`; hosts must protect credentials, require HTTPS, and enforce content and size policies.

For information on reporting security vulnerabilities, see [SECURITY.md](./SECURITY.md).

## 💬 Support

Use [GitHub Issues](https://github.com/hmlendea/nucinotifications.client/issues) for non-sensitive questions and suggestions. Report vulnerabilities privately under the [security policy](SECURITY.md).

## 💝 Project Engagement

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/nucinotifications.client/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for additional information.
