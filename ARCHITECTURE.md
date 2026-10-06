# Architecture

## Overview

NuciNotifications.Client is a lightweight .NET client library for sending notifications through the NuciNotifications API. It provides an abstraction over HTTP communication with the API, handling authentication (Bearer token and HMAC signing) and request/response serialization.

## Components

### INuciNotificationsClient
Interface defining the contract for sending email notifications. Two overloads:
- `SendEmail(recipient, subject, body)` — minimal signature
- `SendEmail(senderName, recipient, subject, body)` — explicit sender name

### NuciNotificationsClient
Concrete implementation of `INuciNotificationsClient`.
- Constructed with `NuciNotificationsSettings` (BaseUrl, ApiKey, HmacSharedSecretKey).
- Uses `NuciApiClient` from `NuciAPI.Client` for HTTP transport.
- Builds `SendEmailRequest` with HMAC ordering attributes.
- Sends `POST /Email` with authorization info (Bearer token + HMAC secret).
- Throws `SmtpException` on HTTP failure or unsuccessful API response.

### NuciNotificationsSettings
Configuration POCO with three properties:
- `BaseUrl` — API base URL
- `ApiKey` — Bearer token for authorization
- `HmacSharedSecretKey` — Shared secret for HMAC request signing

### SendEmailRequest
Request DTO inheriting from `NuciApiRequest`. Properties annotated with `[HmacOrder]` for deterministic HMAC signing:
1. Sender
2. Recipient
3. Subject
4. Body

### ServiceCollectionExtensions
DI registration helpers:
- `AddNuciNotificationsSettings(IServiceCollection, IConfiguration)` — binds settings section and registers singleton `NuciNotificationsSettings`.

## Data Flow

```
Caller
  → INuciNotificationsClient.SendEmail(...)
  → NuciNotificationsClient.SendEmail(...)
  → Build SendEmailRequest
  → NuciApiClient.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(POST, "/Email", authInfo)
  → HTTP POST to {BaseUrl}/Email with Authorization: Bearer {ApiKey} and HMAC signature
  → Deserialize response
  → Throw SmtpException on failure
```

## Dependencies

- **NuciAPI.Client** (1.2.2) — HTTP client, request/response base classes, HMAC signing
- **Microsoft.Extensions.DependencyInjection.Abstractions** (10.0.5) — DI abstractions
- **Microsoft.Extensions.Options.ConfigurationExtensions** (10.0.5) — Configuration binding

## Target Framework

- .NET 10.0 (`net10.0`)

## Error Handling

- Network/HTTP errors → `SmtpException` wrapping original exception
- Unsuccessful API response (non-2xx) → `SmtpException` with API error message
- No retry logic; caller handles retries

## Security

- `ApiKey` and `HmacSharedSecretKey` are sensitive; should be stored in secure configuration (e.g., user secrets, Key Vault).
- HMAC signing ensures request integrity and authenticity.
- No logging of sensitive values.