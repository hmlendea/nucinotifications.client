# Error Handling

This document describes error handling behavior in NuciNotifications.Client.

## Exception Types

### SmtpException

**Namespace:** `System.Net.Mail`
**Thrown by:** `NuciNotificationsClient.SendEmail`

**Scenarios:**
1. **Network/HTTP failure** — Wraps original exception
2. **API error response** — Contains API error message

**Properties:**
- `Message` — Human-readable error description
- `InnerException` — Original exception (network failures only)
- `StatusCode` — Not populated (always 0)

**Example Messages:**
```
"Error while sending the e-mail notification." (network failure)
"Invalid API key" (API error response)
"Recipient email is required" (API validation error)
```

## Error Scenarios

### 1. Network Failures

**Causes:**
- DNS resolution failure
- Connection timeout
- Connection refused
- TLS/SSL handshake failure
- Network partition

**Behavior:**
```csharp
try
{
    response = await apiClient.SendRequestAsync(...);
}
catch (Exception ex)
{
    throw new SmtpException("Error while sending the e-mail notification.", ex);
}
```

**Exception Types Caught:**
- `HttpRequestException` — Most network errors
- `TaskCanceledException` — Timeout (wraps `TimeoutException`)
- `SocketException` — Low-level socket errors
- `JsonException` — Serialization/deserialization errors
- Any other `Exception`

**Caller Handling:**
```csharp
try
{
    await client.SendEmail("user@example.com", "Subject", "Body");
}
catch (SmtpException ex) when (ex.InnerException != null)
{
    // Network failure - ex.InnerException contains root cause
    logger.LogError(ex, "Network error sending email");
    // Retry logic here
}
```

### 2. API Error Responses

**HTTP Status Codes → SmtpException:**

| HTTP Status | API Response | SmtpException.Message |
|-------------|--------------|----------------------|
| 400 | `{"message": "Recipient email is required"}` | `"Recipient email is required"` |
| 401 | `{"message": "Invalid API key"}` | `"Invalid API key"` |
| 403 | `{"message": "HMAC signature invalid"}` | `"HMAC signature invalid"` |
| 404 | `{"message": "Endpoint not found"}` | `"Endpoint not found"` |
| 429 | `{"message": "Rate limit exceeded"}` | `"Rate limit exceeded"` |
| 500 | `{"message": "Internal server error"}` | `"Internal server error"` |
| 502 | `{"message": "Bad gateway"}` | `"Bad gateway"` |
| 503 | `{"message": "Service unavailable"}` | `"Service unavailable"` |

**Behavior:**
```csharp
if (!response.IsSuccessful)
{
    throw new SmtpException(((NuciApiErrorResponse)response).Message);
}
```

**Caller Handling:**
```csharp
try
{
    await client.SendEmail("user@example.com", "Subject", "Body");
}
catch (SmtpException ex) when (ex.InnerException == null)
{
    // API error - ex.Message contains API error message
    if (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
    {
        // Handle rate limiting
    }
    else if (ex.Message.Contains("Invalid API key", StringComparison.OrdinalIgnoreCase))
    {
        // Handle auth failure
    }
    logger.LogWarning(ex, "API error sending email: {Message}", ex.Message);
}
```

### 3. Configuration Errors

**Not validated at startup.** Errors surface on first `SendEmail` call:

| Missing Setting | Failure Point | Exception |
|-----------------|---------------|-----------|
| `BaseUrl` | `new NuciApiClient(settings.BaseUrl)` | `ArgumentException` / `UriFormatException` from `HttpClient` |
| `ApiKey` | API returns 401 | `SmtpException("Invalid API key")` |
| `HmacSharedSecretKey` | API returns 401/403 | `SmtpException("HMAC signature invalid")` |

**Recommendation:** Validate at startup (see CONFIGURATION.md).

## Exception Hierarchy

```
System.Exception
└── System.Net.Mail.SmtpException
    ├── Network failure: InnerException = HttpRequestException, TaskCanceledException, etc.
    └── API error: InnerException = null, Message = API error message
```

## Retry Guidance

### Retryable Errors (Network)
- `HttpRequestException` (transient network issues)
- `TaskCanceledException` (timeout)
- `SocketException` (transient)
- 5xx API errors (server errors)

### Non-Retryable Errors
- 400 (Bad Request) — Fix request data
- 401 (Unauthorized) — Fix credentials
- 403 (Forbidden) — Fix permissions/HMAC secret
- 404 (Not Found) — Fix endpoint/BaseUrl
- 429 (Rate Limited) — Retry with backoff after `Retry-After` header

### Recommended Retry Policy (Polly)

```csharp
var retryPolicy = Policy
    .Handle<SmtpException>(ex =>
        ex.InnerException != null || // Network error
        ex.Message.Contains("500") || ex.Message.Contains("502") || ex.Message.Contains("503"))
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (ex, delay) => logger.LogWarning(ex, "Retrying email send in {Delay}", delay));

await retryPolicy.ExecuteAsync(() => client.SendEmail(...));
```

## Logging

**Library does not log.** Caller responsible for logging.

**Recommended Logging:**
```csharp
public async Task SendWelcomeEmailAsync(string recipient)
{
    try
    {
        await _notificationsClient.SendEmail(recipient, "Welcome", "Welcome to our service.");
        _logger.LogInformation("Welcome email sent to {Recipient}", recipient);
    }
    catch (SmtpException ex) when (ex.InnerException != null)
    {
        _logger.LogError(ex, "Network error sending welcome email to {Recipient}", recipient);
        throw;
    }
    catch (SmtpException ex)
    {
        _logger.LogWarning(ex, "API error sending welcome email to {Recipient}: {Error}", recipient, ex.Message);
        throw;
    }
}
```

## Error Codes / Classification

| Category | Detection | Action |
|----------|-----------|--------|
| Transient Network | `ex.InnerException is HttpRequestException` or `TaskCanceledException` | Retry with exponential backoff |
| Transient Server | `ex.Message` contains 5xx | Retry with exponential backoff |
| Rate Limited | `ex.Message` contains "rate limit" | Retry after delay |
| Authentication | `ex.Message` contains "API key" or "HMAC" | Alert ops, check secrets |
| Validation | `ex.Message` contains "required" or "invalid" | Fix request data |
| Configuration | `ex.Message` on first call, `InnerException` is `ArgumentException` | Fix configuration |

## Testing Error Scenarios

### Unit Test: Network Failure
```csharp
[Fact]
public async Task SendEmail_NetworkFailure_ThrowsSmtpException()
{
    // Arrange
    var mockApiClient = new Mock<INuciApiClient>();
    mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(...))
        .ThrowsAsync(new HttpRequestException("Network down"));

    var client = new NuciNotificationsClient(settings) { ApiClient = mockApiClient.Object };

    // Act & Assert
    await Assert.ThrowsAsync<SmtpException>(() =>
        client.SendEmail("test@example.com", "Subject", "Body"));
}
```

### Unit Test: API Error Response
```csharp
[Fact]
public async Task SendEmail_ApiError_ThrowsSmtpExceptionWithApiMessage()
{
    // Arrange
    var errorResponse = new NuciApiErrorResponse { Message = "Invalid recipient" };
    var mockApiClient = new Mock<INuciApiClient>();
    mockApiClient.Setup(x => x.SendRequestAsync<SendEmailRequest, NuciApiSuccessResponse>(...))
        .ReturnsAsync(errorResponse);

    var client = new NuciNotificationsClient(settings) { ApiClient = mockApiClient.Object };

    // Act & Assert
    var ex = await Assert.ThrowsAsync<SmtpException>(() =>
        client.SendEmail("test@example.com", "Subject", "Body"));

    Assert.Equal("Invalid recipient", ex.Message);
    Assert.Null(ex.InnerException);
}
```

## Thread Safety During Errors

- `NuciNotificationsClient` is stateless — concurrent calls safe
- Each `SendEmail` call creates new `SendEmailRequest` and `NuciApiRequestAuthorisationInfo`
- No shared mutable state during error handling
- `SmtpException` instances are independent per call

## No Silent Failures

- All failures throw `SmtpException`
- No swallowed exceptions
- No fallback behavior
- Caller must handle all exceptions