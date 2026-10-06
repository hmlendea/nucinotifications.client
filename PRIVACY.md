# Privacy

## Data Processing

NuciNotifications.Client is a client library for sending email notifications. It **does not collect, process, store, or transmit personal data** on its own.

### Data Flow

```
Application Code
       │
       ├─► Recipient email address (personal data)
       ├─► Sender name (optional, may be personal data)
       ├─► Subject (may contain personal data)
       └─► Body (may contain personal data)
              │
              ▼
NuciNotificationsClient
       │
       ├─► Constructs SendEmailRequest
       ├─► Signs request with HMAC (no personal data in signature)
       ├─► Sends via HTTPS to configured API endpoint
       │
       ▼
NuciNotifications API (external service)
```

### Responsibility

| Party | Responsibility |
|-------|----------------|
| **Application Developer** | Controls what personal data is sent in email fields; ensures lawful basis for processing; configures API endpoint |
| **NuciNotifications API** | Processes, stores, delivers email; separate privacy policy applies |
| **This Library** | Transmits data provided by application to configured API endpoint; no independent processing |

### No Local Storage

- No databases
- No local files
- No caching of email content
- No telemetry or analytics
- No logging of email content

### Configuration Data

| Setting | Personal Data? | Notes |
|---------|----------------|-------|
| `BaseUrl` | No | API endpoint URL |
| `ApiKey` | No | Authentication token |
| `HmacSharedSecretKey` | No | Signing secret |

### Third-Party Services

This library communicates **only** with the NuciNotifications API endpoint configured via `BaseUrl`.

No other third-party services are contacted.

### Data Subject Rights

Since this library does not store personal data, data subject rights (access, rectification, erasure, portability, restriction, objection) are exercised against:

1. **The Application** — which collects and initiates sending
2. **The NuciNotifications API** — which processes and delivers emails

### Retention

- **This library:** No retention (stateless)
- **Application:** Per application policy
- **NuciNotifications API:** Per API provider policy

### International Transfers

Data is sent to the `BaseUrl` endpoint. Ensure the API endpoint jurisdiction complies with your data transfer requirements (e.g., EU Standard Contractual Clauses, UK IDTA, etc.).

### Children's Data

This library does not process children's data. Application developers must ensure compliance with COPPA, GDPR Article 8, etc., for data they send.

### Security Measures

See [SECURITY.md](SECURITY.md) for transport encryption, secret handling, and HMAC signing.

### Contact

Privacy questions: privacy@hmlendea.go.ro