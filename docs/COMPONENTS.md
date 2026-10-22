# Components

This document provides detailed documentation for each component in the NuciNotifications.Client library.

## INuciNotificationsClient

**Location:** [INuciNotificationsClient.cs](../NuciNotifications.Client/INuciNotificationsClient.cs)

**Purpose:** Defines the contract for sending email notifications through the NuciNotifications API.

**Scope:** Owns the public API surface for email sending. Does not own HTTP transport, authentication, serialization, or configuration.

**Methods:**

| Method | Parameters | Returns | Description |
|--------|------------|---------|-------------|
| `SendEmail` | `recipient: string`, `subject: string`, `body: string` | `Task` | Sends email with default sender (null) |
| `SendEmail` | `senderName: string`, `recipient: string`, `subject: string`, `body: string` | `Task` | Sends email with explicit sender name |
| `SendEmail` | `senderName`, `recipient`, `subject`, `body`, `attachments: IEnumerable<EmailAttachmentRequest>` | `Task` | Submits optional ordered attachments; null sender selects the default sender |

**Entry Points:** Called by application code via DI (`INuciNotificationsClient`).

**Dependencies:** Attachment DTO and standard collection/task contracts.

**Implementation:** `NuciNotificationsClient`.

---

## NuciNotificationsClient

**Location:** [NuciNotificationsClient.cs](../NuciNotifications.Client/NuciNotificationsClient.cs)

**Purpose:** Concrete implementation of `INuciNotificationsClient` that communicates with the NuciNotifications API.

**Scope:** Owns HTTP request construction, authentication, and API communication. Does not own configuration binding, DI registration, or request DTO definitions.

**Constructor:**
```csharp
public NuciNotificationsClient(NuciNotificationsSettings settings, INuciApiClient? apiClient = null)
```
- Captures `settings.BaseUrl`, `settings.ApiKey`, `settings.HmacSharedSecretKey`
- Uses the supplied transport, or creates `NuciApiClient` with `BaseUrl`

**Fields:**
- `readonly INuciApiClient apiClient` — Transport contract from `NuciAPI.Client`

**Method: `SendEmail(recipient, subject, body)`**
- Delegates to `SendEmail(null, recipient, subject, body)`

**Method: `SendEmail(senderName, recipient, subject, body)`**
Delegates to the five-argument overload with null attachments.

**Method: `SendEmail(senderName, recipient, subject, body, attachments)`**
1. Validates required fields, materialises the attachment enumerable once, and validates nested items.
2. Creates `NuciApiRequestAuthorisationInfo` with:
   - `BearerToken = settings.ApiKey`
   - `HmacSharedSecretKey = settings.HmacSharedSecretKey`
3. Constructs `SendEmailRequest` with message fields and optional attachments
4. Calls `apiClient.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>`:
   - HTTP Method: `POST`
   - Endpoint: `/Email`
   - Authorization: `authorisationInfo`
5. On transport exception: throws `SmtpException` wrapping original exception
6. On unsuccessful response: throws `SmtpException` with API error message

**Error Handling:**
- Invalid required fields or attachments produce argument exceptions prior to transport
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

**Location:** [NuciNotificationsSettings.cs](../NuciNotifications.Client/Configuration/NuciNotificationsSettings.cs)

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

**Location:** [SendEmailRequest.cs](../NuciNotifications.Client/Requests/SendEmailRequest.cs)

**Purpose:** Request DTO for the `/Email` API endpoint.

**Scope:** Owns request structure and HMAC signing order. Does not own HTTP transport or response handling.

**Inheritance:** `NuciApiRequest` (from `NuciAPI.Requests`)

**Properties:**

| Property | Type | Required | HmacOrder | Description |
|----------|------|----------|-----------|-------------|
| `Sender` | `string` | No | 1 | Sender name (can be null) |
| `Recipient` | `string` | Yes | 2 | Recipient email address |
| `Subject` | `string` | Yes | 3 | Email subject |
| `Body` | `string` | Yes | 4 | Email body content |
| `Attachments` | `EmailAttachmentRequest[]` | No | Ignored | Ordered file names and Base64 content |
| `AttachmentsSignatureContent` | `string` | Computed | 5 | Web JSON for the ordered files, or an empty string for null/empty attachments; excluded from wire JSON |

**HMAC Signing Order:** Sender, recipient, subject, body, attachment signature content (1-5), matching the attachment-capable API. The attachment array is excluded from direct HMAC processing.

The five-argument `SendEmail(senderName, recipient, subject, body, attachments)` accepts `IEnumerable<EmailAttachmentRequest>`, materialises it once, and validates every item before transport. Existing overloads delegate with no attachments. Pass a null sender for the default sender; no competing four-argument overload is introduced.

[EmailAttachmentRequest](../NuciNotifications.Client/Requests/EmailAttachmentRequest.cs) requires a file name of at most 255 characters with no slash, backslash, or control character, and non-empty valid Base64 content. [SendEmailRequest](../NuciNotifications.Client/Requests/SendEmailRequest.cs) implements `IValidatableObject` to validate nested attachments and reject null elements. Invalid attachments cause `ArgumentException` with parameter name `attachments`, without exposing file contents.

**Validation:** `[Required]` attributes describe Recipient, Subject, and Body. The client explicitly validates these fields and invokes nested attachment validation prior to transport; transport-level validation is not presumed here.

**Dependencies:**
- `NuciAPI.Requests` — `NuciApiRequest`
- `NuciSecurity.HMAC` — `HmacOrderAttribute`
- `System.ComponentModel.DataAnnotations` — `RequiredAttribute`

---

## ServiceCollectionExtensions

**Location:** [ServiceCollectionExtensions.cs](../NuciNotifications.Client/ServiceCollectionExtensions.cs)

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