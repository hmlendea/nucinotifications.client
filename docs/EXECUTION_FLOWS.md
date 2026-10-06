# Execution Flows

This document describes the end-to-end execution flows in NuciNotifications.Client.

## Send Email Flow

### Sequence Diagram

```
Application Code
       │
       ▼
INuciNotificationsClient.SendEmail(senderName, recipient, subject, body)
       │
       ▼
NuciNotificationsClient.SendEmail(senderName, recipient, subject, body)
       │
       ├─► Create NuciApiRequestAuthorisationInfo
       │     ├─ BearerToken = settings.ApiKey
       │     └─ HmacSharedSecretKey = settings.HmacSharedSecretKey
       │
       ├─► Create SendEmailRequest
       │     ├─ Sender = senderName
       │     ├─ Recipient = recipient
       │     ├─ Subject = subject
       │     └─ Body = body
       │
       ├─► apiClient.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(
       │       HttpMethod.Post,
       │       request,
       │       authorisationInfo,
       │       "/Email"
       │     )
       │       │
       │       ▼
       │  NuciApiClient (NuciAPI.Client)
       │       │
       │       ├─► Serialize request to JSON
       │       ├─► Compute HMAC signature over ordered properties
       │       │     (Sender→Recipient→Subject→Body per HmacOrder)
       │       ├─► Add Authorization: Bearer {ApiKey} header
       │       ├─► Add X-HMAC-Signature header
       │       ├─► POST {BaseUrl}/Email
       │       ├─► Deserialize response to NuciApiResponse
       │       └─► Return response
       │
       ▼
Response Handling
       │
       ├─► If exception thrown:
       │     └─► throw SmtpException("Error while sending the e-mail notification.", ex)
       │
       ├─► If !response.IsSuccessful:
       │     └─► throw SmtpException(((NuciApiErrorResponse)response).Message)
       │
       └─► Success: return (void Task completes)
```

### Detailed Steps

#### 1. Client Invocation
Application code calls `INuciNotificationsClient.SendEmail(...)` via DI.

#### 2. Authorization Info Construction
```csharp
NuciApiRequestAuthorisationInfo authorisationInfo = new()
{
    BearerToken = settings.ApiKey,
    HmacSharedSecretKey = settings.HmacSharedSecretKey
};
```
- `BearerToken` → HTTP `Authorization: Bearer {ApiKey}` header
- `HmacSharedSecretKey` → Used by `NuciApiClient` to compute request signature

#### 3. Request DTO Construction
```csharp
new SendEmailRequest()
{
    Sender = senderName,      // HmacOrder(1) - can be null
    Recipient = recipient,    // HmacOrder(2) - required
    Subject = subject,        // HmacOrder(5) - required
    Body = body               // HmacOrder(6) - required
}
```

#### 4. HTTP Request Execution (inside NuciApiClient)
The `NuciApiClient.SendRequestAsync<TRequest, TResponse>` method:
1. Serializes `TRequest` to JSON
2. Computes HMAC-SHA256 signature over canonicalized request:
   - Orders properties by `HmacOrder` attribute
   - Concatenates values with delimiter
   - Signs with `HmacSharedSecretKey`
3. Adds headers:
   - `Authorization: Bearer {BearerToken}`
   - `X-HMAC-Signature: {computed-signature}`
   - `Content-Type: application/json`
4. Sends `POST {BaseUrl}/Email`
5. Reads response
6. Deserializes to `NuciApiResponse` (success or error)

#### 5. Response Handling
```csharp
if (!response.IsSuccessful)
{
    throw new SmtpException(((NuciApiErrorResponse)response).Message);
}
```
- `IsSuccessful` = true for 2xx responses
- On failure: casts to `NuciApiErrorResponse` and extracts `Message`
- Throws `SmtpException` (chosen for semantic alignment with email sending)

#### 6. Exception Handling
```csharp
catch (Exception ex)
{
    throw new SmtpException("Error while sending the e-mail notification.", ex);
}
```
- Catches any exception from HTTP layer (network, timeout, serialization)
- Wraps in `SmtpException` with descriptive message
- Preserves original exception as `InnerException`

## Configuration Flow

### Startup Registration

```
Program.cs / Startup.cs
       │
       ▼
builder.Services.AddNuciNotificationsSettings(builder.Configuration)
       │
       ├─► services.Configure<NuciNotificationsSettings>(
       │       configuration.GetSection("NuciNotificationsSettings"))
       │
       └─► services.AddSingleton(sp =>
               sp.GetRequiredService<IOptions<NuciNotificationsSettings>>().Value)
       │
       ▼
builder.Services.AddSingleton<INuciNotificationsClient>(sp =>
       new NuciNotificationsClient(sp.GetRequiredService<NuciNotificationsSettings>()))
```

### Configuration Binding

1. `IConfiguration.GetSection("NuciNotificationsSettings")` retrieves section
2. `services.Configure<TOptions>` binds section to `NuciNotificationsSettings` instance
3. `IOptions<NuciNotificationsSettings>` registered in DI
4. Singleton `NuciNotificationsSettings` registered by extracting `.Value` from `IOptions`

### Client Instantiation

1. DI resolves `NuciNotificationsSettings` singleton
2. `NuciNotificationsClient` constructed with settings
3. `NuciApiClient` created with `settings.BaseUrl`
4. `INuciNotificationsClient` registered as singleton

## Dependency Injection Flow

```
IServiceCollection
       │
       ├─► AddNuciNotificationsSettings(configuration)
       │       │
       │       ├─► Configure<NuciNotificationsSettings>
       │       └─► AddSingleton<NuciNotificationsSettings>
       │
       ├─► AddSingleton<INuciNotificationsClient>(factory)
       │       └─► factory(sp) → new NuciNotificationsClient(sp.GetRequiredService<NuciNotificationsSettings>())
       │
       ▼
IServiceProvider
       │
       ├─► GetRequiredService<NuciNotificationsSettings>() → settings instance
       ├─► GetRequiredService<INuciNotificationsClient>() → client instance
       │
       ▼
Application Code
       │
       └─► constructor injection of INuciNotificationsClient
```

## Error Propagation Flow

```
NuciApiClient.SendRequestAsync
       │
       ├─► HttpRequestException (network, DNS, timeout)
       │       │
       │       ▼
       │  catch (Exception ex)
       │       │
       │       ▼
       │  throw SmtpException("Error while sending...", ex)
       │
       ├─► TaskCanceledException (timeout)
       │       │
       │       ▼
       │  catch (Exception ex)
       │       │
       │       ▼
       │  throw SmtpException("Error while sending...", ex)
       │
       ├─► JsonException (serialization)
       │       │
       │       ▼
       │  catch (Exception ex)
       │       │
       │       ▼
       │  throw SmtpException("Error while sending...", ex)
       │
       └─► Successful HTTP response but API error (4xx, 5xx)
               │
               ▼
         response.IsSuccessful = false
               │
               ▼
         throw SmtpException(apiErrorResponse.Message)
```

All paths converge to `SmtpException` thrown to caller.

## HMAC Signing Flow

```
SendEmailRequest instance
       │
       ▼
NuciApiClient.SendRequestAsync
       │
       ├─► Get all properties with [HmacOrder] attribute
       ├─► Sort by HmacOrder value (1, 2, 5, 6)
       ├─► For each property in order:
       │       └─► Get value, convert to string, append to canonical string
       ├─► Compute HMAC-SHA256(canonicalString, HmacSharedSecretKey)
       ├─► Base64 encode signature
       └─► Add X-HMAC-Signature header
```

**Canonical String Format** (inferred from `NuciSecurity.HMAC`):
```
{value1}|{value2}|{value5}|{value6}
```
Where missing/null values are represented as empty strings.

## Thread Safety

- `NuciNotificationsClient` is stateless after construction (readonly `apiClient`, captured `settings`)
- `NuciApiClient` from `NuciAPI.Client` — thread safety depends on its implementation
- `HttpClient` inside `NuciApiClient` — designed for concurrent use
- Safe to register as singleton and use concurrently

## Async Flow

All operations are fully asynchronous:
- `SendEmail` returns `Task`
- `apiClient.SendRequestAsync` returns `Task<NuciApiResponse>`
- No synchronous blocking calls
- Suitable for high-throughput scenarios