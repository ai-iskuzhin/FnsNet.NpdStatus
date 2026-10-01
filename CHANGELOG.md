# Changelog

All notable changes to `FnsNet.NpdStatus` are documented in this file.

The project uses Semantic Versioning.

## 1.0.0

### Added

- `FnsNet.NpdStatus`: a typed client for the FNS public service that reports whether an ИНН belongs to a self-employed taxpayer (налог на профессиональный доход) on a given date.
- `AddNpdStatusClient` registers the client through `IHttpClientFactory` and returns the `IHttpClientBuilder`, so retries, rate limiting and logging stay the application's choice.
- `NpdStatusException` carries the service's own `Code` and `ServiceMessage`, and an `IsTransient` flag distinguishing a rejected input from a service that is merely busy.
