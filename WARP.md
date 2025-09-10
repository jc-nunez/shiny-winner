# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

Project: Trossitec Azure Functions - Document Processing PoC

What matters
- This is a .NET 8, Azure Functions v4 (isolated worker) app.
- The current repo contains the function app scaffold (Program.cs, host.json). README.md and spec.md define the intended architecture (EventGrid + Timer triggers, services, providers), but most implementation files are not yet present.
- Application Insights is wired up via Program.cs and host.json.

Commands you’ll use often
- Restore and build (run from the function project directory):
  - cd src/Trossitec.Azure.Function
  - dotnet restore
  - dotnet build
- Run locally with Azure Functions Core Tools:
  - func start
  - With file watch during development: func start --watch
  - Verbose diagnostics: AZURE_FUNCTIONS_ENVIRONMENT=Development func start --verbose
- Tests:
  - Currently, there is no tests/ project in the repo. When tests are added, run: dotnet test
  - Run a single test (when tests exist): dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName"
- Formatting and analyzers (optional, if dotnet-format is installed):
  - dotnet format

Local configuration
- Create src/Trossitec.Azure.Function/local.settings.json for local runs (this file is git-ignored). See README.md for a complete example with the following keys:
  - AzureWebJobsStorage, FUNCTIONS_WORKER_RUNTIME
  - Storage connection strings (Source, Destination, Table)
  - ServiceBusConnection and topic names
  - ExternalApi BaseUrl/ApiKey/TimeoutSeconds
  - Monitoring TimerInterval/MaxCheckCount/MaxAge

High-level architecture (big picture)
- Triggers:
  - EventGrid-triggered function (DocumentProcessingFunction): reacts to blob created/updated events in source storage.
  - Timer-triggered function (DocumentStatusMonitorFunction): periodically checks status of in-flight document requests.
- Core workflow:
  1) EventGrid delivers a blob event → the processing function reads blob + metadata, transfers to destination storage, calls an external API, and persists the returned requestId to Table Storage.
  2) A status “Submitted” message is emitted to Service Bus.
  3) A timer function periodically reads pending requests from Table Storage, queries the external API for status, and then either updates timestamps (still processing) or sends “Completed/Failed” notifications and removes tracking entries.
- Logical layers (as designed in spec.md and referenced in README.md):
  - Functions: boundary/adapters for EventGrid and Timer triggers.
  - Services: business logic orchestration (DocumentHubService, NotificationService).
  - Providers: infrastructure abstractions for Blob/Table storage, Service Bus, and resilient HTTP (Polly via Microsoft.Extensions.Http.Resilience).
  - Models: DTOs and storage entities (DocumentRequest, ProcessingStatus, RequestTrackingEntity, StatusNotification, etc.).
  - Configuration & DI: strongly-typed options; ServiceCollection extensions to wire up providers/services and resilience.
- Observability:
  - Application Insights is enabled (Program.cs + host.json). host.json configures sampling and live metrics filters.

Repo reality vs. spec
- Present code: Program.cs, host.json, project file; no Functions/, Services/, Providers/, Models/ yet.
- Intended structure and behaviors are in README.md and spec.md; use them when generating or navigating code.

Deployment (from README.md)
- az login
- Example publish: func azure functionapp publish trossitec-document-processor
- Configure Azure resources: Storage (source/destination/table), Service Bus topics, Function App, optionally Application Insights; set app settings to match local.settings.json keys and create an EventGrid subscription to the function endpoint.

Notes for Warp
- Prefer operating within src/Trossitec.Azure.Function when building/running.
- If you need to scaffold missing components (Functions/Services/Providers/Models), follow the contracts and flows in spec.md to stay aligned with the intended design.
- No CLAUDE, Cursor, or Copilot rule files were found; defer to README.md and spec.md for authoritative guidance.

