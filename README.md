[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/nucinotifications.client)](https://github.com/hmlendea/nucinotifications.client/releases/latest)
[![Build Status](https://github.com/hmlendea/nucinotifications.client/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/nucinotifications.client/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/NuciNotifications.Client)](https://nuget.org/packages/NuciNotifications.Client)
[![License](https://img.shields.io/github/license/hmlendea/nucinotifications.client)](https://github.com/hmlendea/nucinotifications.client/blob/master/LICENSE)

# NuciNotifications.Client

NuciNotifications.Client is a lightweight .NET client library for sending notifications through the [NuciNotifications API](https://github.com/hmlendea/nucinotifications-api). It provides a strongly-typed, DI-friendly abstraction with asynchronous email sending, Bearer token authentication, and HMAC request signing.

## 📑 Table of Contents

- [Capabilities](#-capabilities)
- [Use Cases](#-use-cases)
- [Usage](#-usage)
- [Installation](#-installation)
- [Configuration](#️-configuration)
- [Compatibility](#-compatibility)
- [Integrations](#-integrations)
- [Authentication and Authorisation](#-authentication-and-authorisation)
- [Privacy and Data](#️-privacy-and-data)
- [Architecture](#️-architecture)
- [Roadmap](#️-roadmap)
- [Documentation](#-documentation)
- [Security](#-security)
- [Contributing](#-contributing)
- [Project Engagement](#-project-engagement)
- [License](#-license)

## ✨ Capabilities

- **Async email sending** — Two overloads: minimal (`recipient`, `subject`, `body`) and explicit sender (`senderName`, `recipient`, `subject`, `body`)
- **Bearer token authentication** — `ApiKey` sent as `Authorization: Bearer` header
- **HMAC request signing** — `HmacSharedSecretKey` used to sign requests for integrity and authenticity
- **Dependency injection ready** — `INuciNotificationsClient` interface, `ServiceCollectionExtensions` for config binding and singleton registration
- **Strongly-typed requests** — `SendEmailRequest` DTO with `HmacOrder` attributes for deterministic signing
- **Comprehensive error handling** — All failures surfaced as `SmtpException` with inner exception or API error message
- **Secure by default** — Secrets never logged; caller controls storage (user secrets, Key Vault, env vars)

## 🎯 Use Cases

- **Application notifications:** Send transactional emails (welcome, password reset, alerts) from .NET services
- **Microservice communication:** Lightweight notification client for distributed systems
- **Secure API integration:** HMAC-signed requests for tamper-proof notification delivery

## 🚀 Usage

### Quick Start

**1. Configure settings** (`appsettings.json`):
```json
{
  "NuciNotificationsSettings": {
    "BaseUrl": "https://your-nuci-notifications-api",
    "ApiKey": "your-api-key",
    "HmacSharedSecretKey": "your-hmac-shared-secret"
  }
}
```

**2. Register services** (`Program.cs`):
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

With explicit sender name:
```csharp
await notificationsClient.SendEmail(
    "Your App Name",
    "user@example.com",
    "Subject",
    "Message body");
```

## 📦 Installation

[![Obtain it from NuGet](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/nuget.png)](https://nuget.org/packages/NuciNotifications.Client)

### Package Manager Installation

```bash
dotnet add package NuciNotifications.Client
```

Or, via the `Package Manager Console`:
```powershell
Install-Package NuciNotifications.Client
```

## ⚙️ Configuration

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
| NuciNotifications API | Compatible with API v1+ | Server-side API |

## 🔌 Integrations

| Integration | Compatibility | Purpose | Required |
|-------------|---------------|---------|----------|
| NuciNotifications API | v1+ | Notification delivery backend | Yes |
| Microsoft.Extensions.DependencyInjection | 10.0.x | DI container integration | Yes |
| Microsoft.Extensions.Configuration | 10.0.x | Configuration binding | Yes |

## 🔐 Authentication and Authorisation

| Role or Scope | Access | Intended Audience |
|---------------|--------|-------------------|
| `ApiKey` (Bearer token) | Full API access | Application services |
| `HmacSharedSecretKey` | Request signing | Application services |

Both credentials are configured via `NuciNotificationsSettings` and passed to the API on each request. The client never logs these values.

## 🛡️ Privacy and Data

For the detailed description of how the application handles privacy and personal data, see [PRIVACY.md](./PRIVACY.md).

## 🏗️ Architecture

See the [architecture documentation](ARCHITECTURE.md) for the system context, principal components, runtime flows, ownership boundaries, dependencies, constraints, and extension points.

## 🗺️ Roadmap

See [ROADMAP.md](./ROADMAP.md) for planned work and forthcoming changes.

## 📚 Documentation

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
| [Roadmap](ROADMAP.md) | Planned features and release cadence |

## 🔒 Security

For information on reporting security vulnerabilities, see [SECURITY.md](./SECURITY.md).

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Preserve the existing public contract unless a breaking change is intentional
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Add tests for additional or modified functionality
- Raise a new [issue](https://github.com/hmlendea/nucinotifications.client/issues) for problems or suggestions

## 💝 Project Engagement

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/nucinotifications.client/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for further information.
