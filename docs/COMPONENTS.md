# Components

This document provides detailed documentation for each component in the NuciNotifications.Client library.

## INuciNotificationsClient

**Location:** `INuciNotificationsClient.cs`

**Purpose:** Defines the contract for sending email notifications through the NuciNotifications API.

**Scope:** Owns the public API surface for email sending. Does not own HTTP transport, authentication, serialization, or configuration.

**Methods:**

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `SendEmail` | `recipient: string`, `subject: string`, `body: string` | `Task` | Sends email with default sender (null) |
| `SendEmail` | `senderName: string`, `recipient: string`, `subject: string`, `body: string` | `Task` | Sends email with explicit sender name |

**Entry Points:** Called by application code via DI (`INuciNotificationsClient`).

**Dependencies:** None (interface only).

**Implementation:** `NuciNotificationsClient`.

---

## NuciNotificationsClient

**Location:** `NuciNotificationsClient.cs`

**Purpose:** Concrete implementation of `INuciNotificationsClient` that communicates with the NuciNotifications API.

**Scope:** Owns HTTP request construction, authentication, and API communication. Does not own configuration binding, DI registration, or request DTO definitions.

**Constructor:**
```csharp
public NuciNotificationsClient(NuciNotificationsSettings settings)
```
- Captures `settings.BaseUrl`, `settings.ApiKey`, `settings.HmacSharedSecretKey`
- Creates `NuciApiClient` with `BaseUrl`

**Fields:**
- `readonly NuciApiClient apiClient` — HTTP client from `NuciAPI.Client`

**Method: `SendEmail(recipient, subject, body)`**
- Delegates to `SendEmail(null, recipient, subject, body)`

**Method: `SendEmail(senderName, recipient, subject, body)`**
1. Creates `NuciApiRequestAuthorisationInfo` with:
   - `BearerToken = settings.ApiKey`
   - `HmacSharedSecretKey = settings.HmacSharedSecretKey`
2. Constructs `SendEmailRequest` with all four properties
3. Calls `apiClient.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>`:
   - HTTP Method: `POST`
   - Endpoint: `/Email`
   - Authorization: `authorisationInfo`
4. On exception: throws `SmtpException` wrapping original exception
5. On unsuccessful response: throws `SmtpException` with API error message

**Error Handling:**
- Network/HTTP errors → `SmtpException` with inner exception
- API error responses (non-2xx) → `SmtpException` with API message
- No retry logic

**Dependencies:**
- `NuciAPI.Client` — `NuciApiClient`, `NuciApiRequestAuthorisationInfo`, `NuciApiResponse`, `NuciApiSuccessResponse`, `NuciApiErrorResponse`
- `NuciNotifications.Client.Requests` — `SendEmailRequest`
- `NuciNotifications.Client.Configuration` — `NuciNotificationsSettings`
- `System.Net.Mail` — `SmtpException`
- `System.Net.Http` — `HttpMethod`

---

## NuciNotificationsSettings

**Location:** `Configuration/NuciNotificationsSettings.cs`

**Purpose:** Configuration POCO for the client.

**Scope:** Owns configuration data structure. Does not own configuration binding, validation, or DI registration.

**Properties:**

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `BaseUrl` | `string` | Yes | Base URL of the NuciNotifications API (e.g., `https://api.example.com`) |
| `ApiKey` | `string` | Yes | Bearer token for API authorization |
| `HmacSharedSecretKey` | `string` | Yes | Shared secret for HMAC request signing |

**Configuration Section Name:** `NuciNotificationsSettings` (matches class name via `nameof`)

**Usage:**
```json
{
  "NuciNotificationsSettings": {
    "BaseUrl": "https://your-nuci-notifications-api",
    "ApiKey": "your-api-key",
    "HmacSharedSecretKey": "your-hmac-shared-secret"
  }
}
```

**Dependencies:** None.

---

## SendEmailRequest

**Location:** `Requests/SendEmailRequest.cs`

**Purpose:** Request DTO for the `/Email` API endpoint.

**Scope:** Owns request structure and HMAC signing order. Does not own HTTP transport or response handling.

**Inheritance:** `NuciApiRequest` (from `NuciAPI.Requests`)

**Properties:**

| Property | Type | Required | HmacOrder | Description |
|----------|------|----------|-----------|-------------|
| `Sender` | `string` | No | 1 | Sender name (can be null) |
| `Recipient` | `string` | Yes | 2 | Recipient email address |
| `Subject` | `string` | Yes | 5 | Email subject |
| `Body` | `string` | Yes | 6 | Email body content |

**HMAC Signing Order:** Properties are signed in `HmacOrder` sequence (1, 2, 5, 6). Gaps in sequence (3, 4) are intentional for compatibility with API expectations.

**Validation:** `[Required]` attributes on Recipient, Subject, Body (enforced by `NuciAPI` serialization/validation pipeline).

**Dependencies:**
- `NuciAPI.Requests` — `NuciApiRequest`
- `NuciSecurity.HMAC` — `HmacOrderAttribute`
- `System.ComponentModel.DataAnnotations` — `RequiredAttribute`

---

## ServiceCollectionExtensions

**Location:** `ServiceCollectionExtensions.cs`

**Purpose:** DI registration helpers for the library.

**Scope:** Owns DI registration logic. Does not own configuration schema or client implementation.

**Method: `AddNuciNotificationsSettings(IServiceCollection, IConfiguration)`**
1. Binds `NuciNotificationsSettings` from configuration section `NuciNotificationsSettings`
2. Registers `NuciNotificationsSettings` as singleton (resolved from `IOptions<NuciNotificationsSettings>`)
3. Returns `IServiceCollection` for chaining

**Usage:**
```csharp
builder.Services.AddNuciNotificationsSettings(builder.Configuration);
builder.Services.AddSingleton<INuciNotificationsClient>(sp =>
    new NuciNotificationsClient(sp.GetRequiredService<NuciNotificationsSettings>()));
```

**Dependencies:**
- `Microsoft.Extensions.Configuration` — `IConfiguration`
- `Microsoft.Extensions.DependencyInjection` — `IServiceCollection`
- `Microsoft.Extensions.Options` — `IOptions<T>`
- `NuciNotifications.Client.Configuration` — `NuciNotificationsSettings`