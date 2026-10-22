# Privacy and Personal Data

This technical description covers NuciNotifications.Client as inspected on 22 October 2026, including optional e-mail attachments in the working tree. The library executes within a calling application and processes potentially personal message data for submission to its configured API.

## 📑 Table of Contents

- [Table of Contents](#-table-of-contents)
- [What This Document Covers](#-what-this-document-covers)
- [Self-Hosted Deployments](#-self-hosted-deployments)
- [Data We Handle](#-data-we-handle)
  - [Data Provided to the Application](#data-provided-to-the-application)
  - [Data Generated or Collected by the Application](#data-generated-or-collected-by-the-application)
  - [Data Received from Integrations](#data-received-from-integrations)
- [Processing and Use](#-processing-and-use)
- [Storage, Retention, and Deletion](#-storage-retention-and-deletion)
- [External Processing and Integrations](#-external-processing-and-integrations)
- [User Controls and Requests](#-user-controls-and-requests)
- [International Transfers](#-international-transfers)
- [Data Protection and Security](#-data-protection-and-security)
- [Document Changes](#-document-changes)
- [Contact](#-contact)

## 🔎 What This Document Covers

This document describes how NuciNotifications.Client at https://github.com/hmlendea/nucinotifications.client handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

The library can be incorporated into operator-controlled applications. It does not itself host an API instance. Application and API operators control their configuration, local storage, logs, backups, access controls, retention, and request handling; this repository does not implement those operational policies.

The client submits supplied message data to the configured `BaseUrl` using its transport. No maintainer telemetry, analytics, crash reporting, or update-check integration is implemented in this library. A replacement transport or the host application may implement separate processing. Omit attachments to prevent their submission; the library provides no general transmission-deactivation setting beyond not invoking submission.

## 📥 Data We Handle

### Data Provided to the Application

- Recipient address, subject, and body are required; the optional sender name may also identify a person.
- Optional attachment file names and Base64-encoded contents may contain personal or confidential information.
- The host supplies `BaseUrl`, `ApiKey`, and `HmacSharedSecretKey`; credentials are sensitive irrespective of whether they identify a person.

### Data Generated or Collected by the Application

- Per-call DTOs, an ordered attachment array, attachment signing JSON, and authorisation information are constructed in memory. The transport computes authentication material. Signing uses message/attachment data; it is not encryption or anonymisation.
- This library contains no logging, telemetry, analytics, or persistence implementation. Host and transport diagnostics are separate responsibilities.

### Data Received from Integrations

- API success/error responses and transport exceptions are received. API error messages are exposed via `SmtpException`, and original transport exceptions may be retained as inner exceptions. Their contents are not sanitised by the library.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- E-mail validation and submission — Recipient, optional sender, subject, body, attachment names, and Base64 contents.
- Authentication and integrity signing — Bearer credentials, shared HMAC secret, ordered message fields, and computed attachment JSON.

## 🗄️ Storage, Retention, and Deletion

The library uses in-memory settings and per-call request objects. It does not read attachment paths, create local files, operate a database, persist message content, or provide deletion/retention controls. Object lifetime and garbage collection do not guarantee secure erasure. Hosts, replacement transports, API operators, and delivery systems determine any subsequent storage, logs, backups, and retention; no period is established by this repository.

## 🔗 External Processing and Integrations

The built-in client delegates outbound submission to `NuciAPI.Client`, directed at the configured API endpoint. The API's processing, delivery providers, storage, and deployment region are not established by this client repository.

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| Configured NuciNotifications API via `NuciAPI.Client` | Notification submission | Message fields, optional attachments, Bearer token, and signature; shared secret supplied to transport for signing | [Settings contract](NuciNotifications.Client/Configuration/NuciNotificationsSettings.cs) |

## ⚙️ User Controls and Requests

The following documented controls or request procedures are available:
- Attachment selection — Omit files or pass null/zero-length collections to the five-argument overload.
- Endpoint and credentials — Configure host settings and protected providers; consult the [configuration reference](docs/CONFIGURATION.md).
- Data requests — Contact the calling application's or API deployment's operator for stored messages, logs, backups, and retention; the library does not provide a data-subject request API.

## 🌍 International Transfers

The host selects `BaseUrl`; the API may reside in another country or region. No deployment location or subsequent delivery route is confirmed here. Operators must identify the applicable destinations and responsibilities for their deployments.

## 🛡️ Data Protection and Security

The facade validates required fields, attachment names, and Base64 encoding prior to transport. Attachment names, contents, and order participate in HMAC signing. These checks do not inspect file contents for malware, enforce payload limits, or make Base64 confidential. HTTPS is recommended but not enforced by this library. Operators must protect secrets and logs, manage access and updates, select secure transport, and review API responses prior to logging them. See the [security policy](SECURITY.md) for disclosure and operational recommendations. No legal compliance certification or age-related filtering is asserted.

## 🔄 Document Changes

Update this document when application data flows, storage, integrations, or deployment responsibilities change. The current version is published at [PRIVACY.md](./PRIVACY.md).

## 📬 Contact

For questions about application data handling, contact the project maintainers. For a self-hosted instance, contact the instance operator, unless the project explicitly handles the request. Include the client version and a non-sensitive description of the deployment and affected submission; do not send passwords, access tokens, or other secrets.

The existing maintainer contact is privacy@hmlendea.go.ro. Do not include personal message contents or attachment files in an initial inquiry.