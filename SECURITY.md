# Security Policy

This policy covers responsible disclosure for NuciNotifications.Client, including optional attachment validation and signing. Security maintenance covers the latest official distribution; working-tree changes are not evidence of a published release.

## 📑 Table of Contents

- [Table of Contents](#-table-of-contents)
- [Supported Versions](#-supported-versions)
- [Reporting a Vulnerability](#-reporting-a-vulnerability)
- [Scope](#-scope)
- [Disclosure Policy](#-disclosure-policy)
- [Security Model](#-security-model)
    - [Threat Model](#threat-model)
    - [Secrets Handling](#secrets-handling)
    - [HMAC Signing](#hmac-signing)
    - [Transport Security](#transport-security)
    - [Input Validation](#input-validation)
    - [Dependency Security](#dependency-security)
    - [Secure Development Practices](#secure-development-practices)
    - [Incident Response](#incident-response)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | NuGet | ✅ |
| Latest version | GitHub Releases | ✅ |
| Latest version | Unofficial third-party distribution channels | ❌ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/nucinotifications.client/security/advisories)
- Contact the maintainers directly

The existing maintainer contact is security@hmlendea.go.ro. Include affected versions, reproduction steps, and potential impact without credentials, personal message content, or sensitive attachments. The previously documented response objective is acknowledgement within 48 hours; this is not a guaranteed remediation interval. Do not use public issues for vulnerability reports.

## 📌 Scope

The subsequent report categories are in scope for this repository:
- Client request integrity, credential exposure, and transport integration defects.
- Attachment validation bypasses, signing discrepancies, and unintended message-data disclosure.

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Vulnerabilities solely in independently operated API deployments or their delivery infrastructure; report those to their operators.
- Unauthorised testing of third-party systems or disclosure of genuine credentials and message data.

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.

## 🔐 Security Model

### Threat Model

| Asset | Threat | Mitigation |
|-------|--------|------------|
| `ApiKey` and `HmacSharedSecretKey` | Exposure via configuration, memory, or host diagnostics | Use protected host configuration; library has no logging, but transport exceptions require scrutiny |
| Request integrity | Message or attachment alteration | HMAC signing contract; deployed API must verify it |
| Request authenticity | Replay of a valid signed request | No client replay-protection guarantee; server policy is external |
| Message and attachment confidentiality | Interception or excessive diagnostics | Configure HTTPS and protect host/server logs; Base64 is not encryption |
| Attachment resources | Excessive input or malicious content | Host must enforce payload limits and content policy |

### Secrets Handling

**This library:**
- Does not log `ApiKey` or `HmacSharedSecretKey`
- Uses a generic attachment-validation message; API messages and inner transport exceptions are not sanitised
- Does not persist secrets
- Passes authorisation information to the configured `INuciApiClient` for authentication and signing

**Caller must:**
- Store secrets in secure configuration (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, user secrets)
- Use environment variables or secret providers in production
- Never commit secrets to source control
- Rotate secrets periodically

### HMAC Signing

Signing is delegated to `NuciAPI.Client` and its HMAC integration. The locally verified contract is defined by the attributes and computed content in [SendEmailRequest](NuciNotifications.Client/Requests/SendEmailRequest.cs); this document does not prescribe an unverified token delimiter or header format.

**Signed Properties (in order):**
1. `Sender` (HmacOrder 1) — can be null
2. `Recipient` (HmacOrder 2) — required
3. `Subject` (HmacOrder 3) - required
4. `Body` (HmacOrder 4) - required
5. `AttachmentsSignatureContent` (HmacOrder 5) - ordered Web JSON of file names and Base64 contents, or an empty string for absent/empty attachments

The attachment array is excluded from direct signing; its computed signature content is excluded from wire JSON. Names, contents, order, and file removal affect the token. Nested validation rejects null elements, invalid Base64, and unsafe file names before transport. Attachments remain caller-provided data in memory; the library performs no file access, persistence, scanning, or payload-size enforcement. Use HTTPS and apply application-specific content and size restrictions. Base64 is encoding, not encryption.

Signature verification and rejection responses are responsibilities of the configured API. HMAC does not encrypt message data or establish replay protection.

### Transport Security

- **HTTPS recommendation:** Configure an HTTPS `BaseUrl`; this library does not enforce the URI scheme.
- **Transport boundary:** TLS configuration, certificate validation, and connection policy belong to the transport and host environment; no certificate-pinning API is exposed here.

### Input Validation

**Client-side:**
- `SendEmailRequest` uses `[Required]` on Recipient, Subject, Body
- The facade explicitly validates required message fields and nested attachments prior to transport
- Null `Sender` is permitted for default sender selection
- Attachment names require 1-255 characters without directory separators or control characters; contents require non-empty valid Base64

**API-side:** NuciNotifications API performs authoritative validation.

### Dependency Security

The [project manifest](NuciNotifications.Client/NuciNotifications.Client.csproj) references `NuciAPI.Client` 1.2.4 and Microsoft dependency-injection/options packages 10.0.13. HMAC contracts are transitive dependencies. No vulnerability-free assertion is made here.

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

### Incident Response

If secrets are compromised:
1. Rotate `ApiKey` and `HmacSharedSecretKey` immediately in NuciNotifications API
2. Update configuration in all deployments
3. Audit access logs for unauthorized usage
4. Review this library's usage for additional exposure

Message fields and attachments may contain personal data. Consult [PRIVACY.md](PRIVACY.md); this policy does not assert legal compliance or security certification.