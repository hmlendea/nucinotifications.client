[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html) [![Latest GitHub release](https://img.shields.io/github/v/release/hmlendea/nucinotifications.client)](https://github.com/hmlendea/nucinotifications.client/releases/latest) [![Build Status](https://github.com/hmlendea/nucinotifications.client/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/nucinotifications.client/actions/workflows/dotnet.yml)

# NuciNotifications.Client

## About

NuciNotifications.Client is a lightweight .NET client library for sending notifications through the [NuciNotifications API](https://github.com/hmlendea/nucinotifications-api).

It currently exposes an `INuciNotificationsClient` abstraction with asynchronous email sending methods and supports authenticated requests using:
- Bearer token authentication (`ApiKey`)
- HMAC shared secret signing (`HmacSharedSecretKey`)

The client sends requests to the API email endpoint (`/Email`) using the configured base URL.

## Installation

[![Get it from NuGet](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/nuget.png)](https://nuget.org/packages/NuciNotifications.Client)

### NET CLI
```bash
dotnet add package NuciNotifications.Client
```

### Package Manager
```powershell
Install-Package NuciNotifications.Client
```

## Quick Start

### 1) Configure settings

Add your settings in `appsettings.json`:

```json
{
	"NuciNotificationsSettings": {
		"BaseUrl": "https://your-nuci-notifications-api",
		"ApiKey": "your-api-key",
		"HmacSharedSecretKey": "your-hmac-shared-secret"
	}
}
```

### 2) Register services

```csharp
using NuciNotifications.Client;
using NuciNotifications.Client.Configuration;

builder.Services.AddNuciNotificationsSettings(builder.Configuration);
builder.Services.AddSingleton<INuciNotificationsClient>(serviceProvider =>
		new NuciNotificationsClient(serviceProvider.GetRequiredService<NuciNotificationsSettings>()));
```

### 3) Send an email

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

You can also send with an explicit sender name:

```csharp
await notificationsClient.SendEmail(
		"Your App Name",
		"user@example.com",
		"Subject",
		"Message body");
```

## Configuration

The client uses `NuciNotificationsSettings`:

- `BaseUrl`: Base URL of the NuciNotifications API.
- `ApiKey`: Bearer token used to authorize API requests.
- `HmacSharedSecretKey`: Shared secret used for HMAC request signing.

You can bind these from configuration using:

```csharp
services.AddNuciNotificationsSettings(configuration);
```

## Behavior Notes

- All operations are asynchronous.
- `SendEmail(...)` throws `SmtpException` when:
	- the HTTP/API request fails, or
	- the API returns an unsuccessful response.
- Validation requirements for email requests:
	- `Recipient` is required.
	- `Subject` is required.
	- `Body` is required.
- `Sender` is optional.

## Related Projects

- [NuciNotifications API](https://github.com/hmlendea/nucinotifications-api) for the server API
- [NuciNotifications Client](https://github.com/hmlendea/nucinotifications.client) for the client NuGet package

## Target Framework

The current package targets `.NET 10.0`.

## License

This project is licensed under the `GNU General Public License v3.0` or later. See [LICENSE](./LICENSE) for details.
