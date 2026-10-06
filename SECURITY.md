# Security

## Reporting Security Vulnerabilities

If you discover a security vulnerability in NuciNotifications.Client, please report it responsibly:

**Email:** security@hmlendea.go.ro

**Do not** create public GitHub issues for security vulnerabilities.

We will acknowledge receipt within 48 hours and provide a timeline for fix.

## Security Model

### Threat Model

| Asset | Threat | Mitigation |
|-------|--------|------------|
| `ApiKey` (Bearer token) | Theft from config/logs/memory | Never logged; stored in secure config (Key Vault, user secrets) |
| `HmacSharedSecretKey` | Theft from config/logs/memory | Never logged; stored in secure config |
| Request integrity | Tampering in transit | HMAC-SHA256 signing of request body |
| Request authenticity | Replay attacks | HMAC includes all request parameters; API should enforce nonce/timestamp |
| API communication | MITM | HTTPS enforced (BaseUrl should be https://) |

### Data Flow Security

```
Application
    │
    ├─► NuciNotificationsSettings (in memory)
    │       ├─ BaseUrl (low sensitivity)
    │       ├─ ApiKey (HIGH sensitivity)
    │       └─ HmacSharedSecretKey (HIGH sensitivity)
    │
    ▼
NuciNotificationsClient
    │
    ├─► NuciApiRequestAuthorisationInfo
    │       ├─ BearerToken = ApiKey
    │       └─ HmacSharedSecretKey = HmacSharedSecretKey
    │
    ▼
NuciApiClient (NuciAPI.Client)
    │
    ├─► Authorization: Bearer {ApiKey} header
    ├─► X-HMAC-Signature header (computed from request + HmacSharedSecretKey)
    ├─► HTTPS POST to {BaseUrl}/Email
    │
    ▼
NuciNotifications API
```

### Secrets Handling

**This library:**
- Does not log `ApiKey` or `HmacSharedSecretKey`
- Does not include secrets in exception messages
- Does not persist secrets
- Passes secrets only to `NuciApiClient` for request signing

**Caller must:**
- Store secrets in secure configuration (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, user secrets)
- Use environment variables or secret providers in production
- Never commit secrets to source control
- Rotate secrets periodically

### HMAC Signing

**Algorithm:** HMAC-SHA256 (via `NuciSecurity.HMAC`)

**Signed Properties (in order):**
1. `Sender` (HmacOrder 1) — can be null
2. `Recipient` (HmacOrder 2) — required
3. `Subject` (HmacOrder 5) — required
4. `Body` (HmacOrder 6) — required

**Signature Header:** `X-HMAC-Signature: <base64-hmac>`

**Verification:** Performed by NuciNotifications API. Invalid signature → 401/403.

### Transport Security

- **HTTPS Required:** `BaseUrl` must use `https://` scheme
- **Certificate Validation:** Uses system default (validates against trusted CAs)
- **No Certificate Pinning:** Relies on PKI; caller can configure custom `HttpClientHandler` if needed

### Input Validation

**Client-side:**
- `SendEmailRequest` uses `[Required]` on Recipient, Subject, Body
- Validation enforced by `NuciAPI` serialization pipeline
- Null `Sender` allowed (omitted from HMAC or sent as empty)

**API-side:** NuciNotifications API performs authoritative validation.

### Dependency Security

| Dependency | Version | Security Notes |
|------------|---------|----------------|
| `NuciAPI.Client` | 1.2.2 | Handles secrets in `NuciApiRequestAuthorisationInfo` |
| `NuciSecurity.HMAC` | 4.1.2 | Implements HMAC signing; secret never leaves signing function |
| `Microsoft.Extensions.*` | 10.0.5 | No known vulnerabilities |
| `Microsoft.AspNetCore.*` | 2.3.9 / 10.0.5 | No known vulnerabilities |

**Recommendation:** Regularly scan for vulnerabilities:
```bash
dotnet list package --vulnerable --include-transitive
```

### Secure Development Practices

**For Contributors:**
- Never log sensitive data
- Use `ILogger` with structured logging; avoid string interpolation with secrets
- Review PRs for accidental secret exposure
- Run security scans before release

**Code Review Checklist:**
- [ ] No `ApiKey` or `HmacSharedSecretKey` in logs
- [ ] No secrets in exception messages
- [ ] No secrets in comments or documentation
- [ ] HTTPS used for all external calls
- [ ] Dependencies updated for security patches

### Compliance

- **GDPR:** No personal data processed by this library (only passes through)
- **SOC 2:** Compatible with secure configuration practices
- **PCI DSS:** No cardholder data handled

### Security Headers (API Response)

The NuciNotifications API should return:
- `Strict-Transport-Security`
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Content-Security-Policy: default-src 'none'`

### Incident Response

If secrets are compromised:
1. Rotate `ApiKey` and `HmacSharedSecretKey` immediately in NuciNotifications API
2. Update configuration in all deployments
3. Audit access logs for unauthorized usage
4. Review this library's usage for additional exposure

## Contact

Security questions: security@hmlendea.go.ro