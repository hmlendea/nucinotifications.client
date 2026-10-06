# Roadmap

## Version 1.2.0 (Planned)

### Features
- [ ] **Async enumeration support** — `SendEmailsAsync(IEnumerable<EmailRequest>)` for batch sending
- [ ] **Template support** — `SendTemplatedEmail(templateId, recipient, parameters)`
- [ ] **Attachment support** — `SendEmailWithAttachments(attachments)`
- [ ] **Delivery receipts** — Optional webhook/callback for delivery status

### Improvements
- [ ] **Retry policy configuration** — Built-in Polly integration with configurable retry
- [ ] **Timeout configuration** — Per-request timeout via settings
- [ ] **Custom HttpClient** — Allow injecting pre-configured `HttpClient`/`HttpMessageHandler`
- [ ] **Rate limit handling** — Automatic backoff on 429 with `Retry-After` header parsing

### Developer Experience
- [ ] **Source generators** — Compile-time validation of `SendEmailRequest` properties
- [ ] **Analyzers** — Roslyn analyzers for common misconfigurations
- [ ] **Improved XML docs** — Full IntelliSense documentation for all public APIs

## Version 1.3.0 (Future)

### Features
- [ ] **Multi-channel support** — SMS, Push, Webhook channels via same client
- [ ] **Message scheduling** — `SendAt` / `SendAfter` parameters
- [ ] **Idempotency keys** — Client-generated idempotency for safe retries
- [ ] **Batch API** — Single HTTP request for multiple emails

### Architecture
- [ ] **Interface segregation** — `IEmailClient`, `ISmsClient`, `IPushClient` from `INuciNotificationsClient`
- [ ] **Pipeline behaviors** — Middleware for logging, metrics, correlation IDs
- [ ] **OpenTelemetry integration** — Built-in tracing and metrics

## Version 2.0.0 (Breaking Changes)

### Breaking Changes
- [ ] **Target .NET 11+** — Drop net10.0, require net11.0+
- [ ] **Primary constructors only** — Remove parameterless constructors
- [ ] **Result<T> pattern** — Replace exceptions with `Result<Success, Error>` for non-exceptional failures
- [ ] **Nullable reference types** — Full NRT annotations, non-nullable by default
- [ ] **Interface rename** — `INuciNotificationsClient` → `INotificationsClient` (namespace change)

### Modernization
- [ ] **Native AOT compatibility** — Trim-safe, AOT-friendly
- [ ] **System.Text.Json only** — Remove Newtonsoft.Json transitive dependency
- [ ] **Minimal APIs** — Extension methods for `WebApplication.MapNotificationsEndpoints()`

## Maintenance

### Ongoing
- [ ] **Dependency updates** — Monthly NuGet dependency updates
- [ ] **Security scans** — Weekly `dotnet list package --vulnerable`
- [ ] **Framework updates** — Update to new .NET versions within 3 months of release

### Technical Debt
- [ ] **Add unit tests** — Current coverage: 0%
- [ ] **Add integration tests** — Against test API instance
- [ ] **Benchmark suite** — Performance regression detection
- [ ] **Documentation site** — DocFX or similar for docs.nuci.io

## Release Cadence

| Version Type | Frequency | Branch |
|--------------|-----------|--------|
| Patch (1.x.y) | As needed | `master` |
| Minor (1.y.0) | Quarterly | `master` |
| Major (x.0.0) | Annually | `major/x` |

## Support Policy

| Version | Support Status | End of Life |
|---------|----------------|-------------|
| 1.1.x | Active | TBD |
| 1.0.x | Security only | 2026-12-31 |

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) (to be created) for:
- Development setup
- Coding standards
- PR process
- Release process