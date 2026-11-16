# Dependencies

This document describes all dependencies of NuciNotifications.Client, their roles, and version requirements.

## Direct Dependencies (PackageReference)

| Package | Version | Purpose | Transitive Dependencies |
|---------|---------|---------|------------------------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 11.0.0 | DI abstractions (`IServiceCollection`, `IServiceProvider`) | `Microsoft.Extensions.DependencyInjection` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | 11.0.0 | Configuration binding (`services.Configure<TOptions>`, `IOptions<T>`) | `Microsoft.Extensions.Options`, `Microsoft.Extensions.Configuration.Binder` |
| `NuciAPI.Client` | 1.2.4 | HTTP client, request/response base classes, HMAC signing integration | See below |

## Transitive Dependencies (via NuciAPI.Client 1.2.4)

| Package | Version | Purpose |
|---------|---------|---------|
| `NuciAPI` | 3.6.2 | Core API abstractions: `NuciApiRequest`, `NuciApiResponse`, `NuciApiSuccessResponse`, `NuciApiErrorResponse`, `NuciApiRequestAuthorisationInfo` |
| `NuciSecurity.HMAC` | 4.1.4 | HMAC signing: `HmacOrderAttribute`, signing logic |
| `NuciExtensions` | 6.0.0 | Utility extensions used by NuciAPI/NuciSecurity |
| `Microsoft.AspNetCore.WebUtilities` | 10.0.13 | HTTP utilities, query helpers |

## Transitive Dependencies (via NuciAPI 3.6.2)

| Package | Version | Purpose |
|---------|---------|---------|
| `NuciSecurity.HMAC` | 4.1.4 | HMAC signing (shared with NuciAPI.Client) |

## Transitive Dependencies (via NuciSecurity.HMAC 4.1.4)

| Package | Version | Purpose |
|---------|---------|---------|
| `NuciExtensions` | 6.0.0 | Base extensions (shared with NuciAPI.Client) |

## Framework Dependencies

| Framework | Version | Notes |
|-----------|---------|-------|
| `net10.0` | 10.0 | Target framework; uses .NET 10 APIs |

## Dependency Graph

```
NuciNotifications.Client (net10.0)
├── Microsoft.Extensions.DependencyInjection.Abstractions (11.0.0)
│   └── Microsoft.Extensions.DependencyInjection
├── Microsoft.Extensions.Options.ConfigurationExtensions (11.0.0)
│   ├── Microsoft.Extensions.Options
│   └── Microsoft.Extensions.Configuration.Binder
└── NuciAPI.Client (1.2.4)
    ├── NuciAPI (3.6.2)
    │   └── NuciSecurity.HMAC (4.1.4)
    │       └── NuciExtensions (6.0.0)
    ├── NuciSecurity.HMAC (4.1.4)
    │   └── NuciExtensions (6.0.0)
    ├── NuciExtensions (6.0.0)
    └── Microsoft.AspNetCore.WebUtilities (10.0.13)
```

## Version Compatibility Notes

- **NuciAPI.Client 1.2.4** requires **NuciAPI 3.6.2** and **NuciSecurity.HMAC 4.1.4**
- **NuciSecurity.HMAC 4.1.4** requires **NuciExtensions 6.0.0**, matching the version brought by **NuciAPI.Client 1.2.4**
- **Microsoft.Extensions.* 11.0.0** resolves against the **net10.0** target framework
- **Microsoft.AspNetCore.WebUtilities 10.0.13**, brought transitively, is compatible with net10.0

## Security-Critical Dependencies

| Dependency | Security Role | Sensitivity |
|------------|---------------|-------------|
| `NuciSecurity.HMAC` | Computes request signatures using `HmacSharedSecretKey` | High — secret key handling |
| `NuciAPI.Client` | Adds `Authorization: Bearer` header with `ApiKey` | High — token handling |
| `NuciAPI` | Defines `NuciApiRequestAuthorisationInfo` carrying both secrets | High — secret container |

**Security Notes:**
- `ApiKey` and `HmacSharedSecretKey` flow through `NuciNotificationsSettings` → `NuciApiRequestAuthorisationInfo` → `NuciApiClient`
- No logging of sensitive values in this library
- Caller responsible for secure configuration storage (user secrets, Key Vault, env vars)

## Dependency Usage by Component

| Component | Direct Dependencies | Transitive Dependencies Used |
|-----------|---------------------|------------------------------|
| `INuciNotificationsClient` | None | None |
| `NuciNotificationsClient` | `NuciAPI.Client`, `NuciNotifications.Client.Requests`, `NuciNotifications.Client.Configuration`, `System.Net.Mail`, `System.Net.Http` | `NuciAPI`, `NuciSecurity.HMAC` |
| `NuciNotificationsSettings` | None | None |
| `SendEmailRequest` | `NuciAPI.Requests`, `NuciSecurity.HMAC`, `System.ComponentModel.DataAnnotations` | None |
| `ServiceCollectionExtensions` | `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Options`, `NuciNotifications.Client.Configuration` | None |

## NuGet Package Metadata

From `NuciNotifications.Client.csproj`:
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="11.0.0" />
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="11.0.0" />
<PackageReference Include="NuciAPI.Client" Version="1.2.4" />
```

## License Compatibility

| Package | License | Compatible with GPL-3.0-or-later? |
|---------|---------|-----------------------------------|
| Microsoft.Extensions.* | MIT | Yes |
| NuciAPI.Client | Unknown (assumed compatible) | Verify |
| NuciAPI | Unknown (assumed compatible) | Verify |
| NuciSecurity.HMAC | Unknown (assumed compatible) | Verify |
| NuciExtensions | Unknown (assumed compatible) | Verify |
| Microsoft.AspNetCore.* | Apache-2.0 | Yes |

**Note:** This library is licensed GPL-3.0-or-later. All dependencies must be compatible. Verify Nuci* package licenses before distribution.

## Upgrade Considerations

| Dependency | Current | Latest (as of 2026) | Notes |
|------------|---------|---------------------|-------|
| Microsoft.Extensions.DependencyInjection.Abstractions | 11.0.0 | Check NuGet | Align with net10.0 |
| Microsoft.Extensions.Options.ConfigurationExtensions | 11.0.0 | Check NuGet | Align with net10.0 |
| NuciAPI.Client | 1.2.4 | Check NuGet | Breaking changes possible |
| NuciAPI | 3.6.2 (transitive) | Check NuGet | Coupled with NuciAPI.Client |
| NuciSecurity.HMAC | 4.1.4 (transitive) | Check NuGet | Coupled with NuciAPI |

**Recommendation:** Update NuciAPI.Client and its transitive dependencies together to maintain compatibility.