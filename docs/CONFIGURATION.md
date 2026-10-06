# Configuration

This document describes all configuration options for NuciNotifications.Client.

## NuciNotificationsSettings

**Class:** `NuciNotifications.Client.Configuration.NuciNotificationsSettings`
**Configuration Section:** `NuciNotificationsSettings`

### Properties

| Property | Type | Required | Default | Description |
|----------|------|----------|---------|-------------|
| `BaseUrl` | `string` | Yes | — | Base URL of the NuciNotifications API (e.g., `https://api.example.com`) |
| `ApiKey` | `string` | Yes | — | Bearer token for API authorization |
| `HmacSharedSecretKey` | `string` | Yes | — | Shared secret for HMAC request signing |

### Example Configuration

**appsettings.json:**
```json
{
  "NuciNotificationsSettings": {
    "BaseUrl": "https://notifications.example.com",
    "ApiKey": "sk_live_abcdef123456",
    "HmacSharedSecretKey": "hmac_shared_secret_xyz789"
  }
}
```

**appsettings.Development.json:**
```json
{
  "NuciNotificationsSettings": {
    "BaseUrl": "https://dev-notifications.example.com",
    "ApiKey": "sk_dev_abcdef123456",
    "HmacSharedSecretKey": "dev_hmac_shared_secret_xyz789"
  }
}
```

**Environment Variables:**
```bash
export NuciNotificationsSettings__BaseUrl="https://notifications.example.com"
export NuciNotificationsSettings__ApiKey="sk_live_abcdef123456"
export NuciNotificationsSettings__HmacSharedSecretKey="hmac_shared_secret_xyz789"
```

**User Secrets (Development):**
```bash
dotnet user-secrets set "NuciNotificationsSettings:BaseUrl" "https://dev-notifications.example.com"
dotnet user-secrets set "NuciNotificationsSettings:ApiKey" "sk_dev_abcdef123456"
dotnet user-secrets set "NuciNotificationsSettings:HmacSharedSecretKey" "dev_hmac_shared_secret_xyz789"
```

**Azure Key Vault / AWS Secrets Manager:**
Store secrets externally and bind via configuration providers.

### Registration

```csharp
using NuciNotifications.Client;
using NuciNotifications.Client.Configuration;

// In Program.cs / Startup.cs
builder.Services.AddNuciNotificationsSettings(builder.Configuration);
builder.Services.AddSingleton<INuciNotificationsClient>(serviceProvider =>
    new NuciNotificationsClient(serviceProvider.GetRequiredService<NuciNotificationsSettings>()));
```

**What `AddNuciNotificationsSettings` does:**
1. `services.Configure<NuciNotificationsSettings>(configuration.GetSection("NuciNotificationsSettings"))`
   - Binds configuration section to `NuciNotificationsSettings` instance
   - Registers `IOptions<NuciNotificationsSettings>` in DI
2. `services.AddSingleton(sp => sp.GetRequiredService<IOptions<NuciNotificationsSettings>>().Value)`
   - Registers `NuciNotificationsSettings` itself as singleton
   - Allows direct injection of `NuciNotificationsSettings` (not `IOptions<>`)

### Validation

The library does **not** perform automatic validation of settings at startup. Validation occurs at runtime when sending emails:

- `BaseUrl` — Used to construct `NuciApiClient`; invalid URL causes `HttpRequestException` on first send
- `ApiKey` — Sent as Bearer token; invalid key causes API 401 → `SmtpException`
- `HmacSharedSecretKey` — Used for HMAC signing; invalid secret causes API 401 → `SmtpException`

**Recommended:** Add startup validation in your application:

```csharp
builder.Services.AddNuciNotificationsSettings(builder.Configuration);

// Validate at startup
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

### Security Considerations

| Setting | Sensitivity | Storage Recommendation |
|---------|-------------|------------------------|
| `BaseUrl` | Low | Configuration file, environment variable |
| `ApiKey` | **High** | User secrets (dev), Key Vault / Secrets Manager (prod), environment variable |
| `HmacSharedSecretKey` | **High** | User secrets (dev), Key Vault / Secrets Manager (prod), environment variable |

**Never:**
- Commit `ApiKey` or `HmacSharedSecretKey` to source control
- Log these values
- Include in error messages or telemetry

**The library does not log sensitive values.**

### Multiple Environments

Use standard ASP.NET Core configuration layering:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configuration loads in order (later overrides earlier):
// 1. appsettings.json
// 2. appsettings.{Environment}.json
// 3. User secrets (Development only)
// 4. Environment variables
// 5. Command line arguments

builder.Services.AddNuciNotificationsSettings(builder.Configuration);
```

### Configuration Binding Details

The binding uses `Microsoft.Extensions.Configuration.Binder`:
- Case-insensitive property matching
- Supports nested objects (not used here)
- Collections not used
- Null values leave properties as null (no defaults)

### DI Registration Options

**Option 1: Singleton Settings + Singleton Client (Recommended)**
```csharp
builder.Services.AddNuciNotificationsSettings(builder.Configuration);
builder.Services.AddSingleton<INuciNotificationsClient>(sp =>
    new NuciNotificationsClient(sp.GetRequiredService<NuciNotificationsSettings>()));
```

**Option 2: Scoped Client (if settings change at runtime)**
```csharp
builder.Services.AddNuciNotificationsSettings(builder.Configuration);
builder.Services.AddScoped<INuciNotificationsClient>(sp =>
    new NuciNotificationsClient(sp.GetRequiredService<NuciNotificationsSettings>()));
```

**Option 3: Factory with Options Monitoring (for dynamic config reload)**
```csharp
builder.Services.AddNuciNotificationsSettings(builder.Configuration);
builder.Services.AddSingleton<INuciNotificationsClient>(sp => {
    var options = sp.GetRequiredService<IOptionsMonitor<NuciNotificationsSettings>>();
    return new NuciNotificationsClient(options.CurrentValue);
});
// Note: Client won't pick up config changes unless recreated
```

### Testing Configuration

**Unit Test with Mock Settings:**
```csharp
var settings = new NuciNotificationsSettings
{
    BaseUrl = "https://test-api.example.com",
    ApiKey = "test-key",
    HmacSharedSecretKey = "test-secret"
};

var client = new NuciNotificationsClient(settings);
// Test with mocked NuciApiClient or integration test against test API
```

**Integration Test with TestServer:**
```csharp
var builder = new WebHostBuilder()
    .ConfigureAppConfiguration(c => c.AddInMemoryCollection(new[]
    {
        new KeyValuePair<string, string>("NuciNotificationsSettings:BaseUrl", "https://localhost:5001"),
        new KeyValuePair<string, string>("NuciNotificationsSettings:ApiKey", "test-key"),
        new KeyValuePair<string, string>("NuciNotificationsSettings:HmacSharedSecretKey", "test-secret")
    }))
    .ConfigureServices(s => s.AddNuciNotificationsSettings(Configuration));
```

### Configuration Schema (JSON Schema)

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "NuciNotificationsSettings",
  "type": "object",
  "required": ["BaseUrl", "ApiKey", "HmacSharedSecretKey"],
  "properties": {
    "BaseUrl": {
      "type": "string",
      "format": "uri",
      "description": "Base URL of the NuciNotifications API"
    },
    "ApiKey": {
      "type": "string",
      "minLength": 1,
      "description": "Bearer token for API authorization"
    },
    "HmacSharedSecretKey": {
      "type": "string",
      "minLength": 1,
      "description": "Shared secret for HMAC request signing"
    }
  },
  "additionalProperties": false
}
```