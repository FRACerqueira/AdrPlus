<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Publish the plugin contract as an independent AdrPlus Abstractions package|
|Version|01|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-08-04)|
|Changed|Accepted (2026-08-04)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Publish the plugin contract as an independent AdrPlus Abstractions package

## Deciders

* Deciders: Fernando Cerqueira (project maintainer)

## Context and Problem Statement

[ADR002](ADR002V01-add-a-plugin-system-for-adr-lifecycle-event-integrations.md) requires a public contract that plugin authors reference to implement `IAdrPlugin` — but that contract needs to be available to a plugin's own project without pulling in the entire AdrPlus CLI, and its release cadence shouldn't be tied to the CLI's own. How should the plugin contract be packaged and made available to third-party plugin authors?

## Considered Options

* Keep the contract internal to the CLI project — plugin authors would need to reference the CLI's own assembly (or copy the interfaces) to implement `IAdrPlugin`
* Publish the contract as its own NuGet package, `AdrPlus.Abstractions`, versioned and released independently of the CLI
* Ship a full plugin SDK: a `dotnet new adrplus-plugin` project template plus a test harness

## Decision Outcome

Chosen option: "Publish the contract as its own NuGet package, `AdrPlus.Abstractions`", because it lets a plugin project depend on exactly the interfaces and immutable DTOs it needs (`IAdrPlugin`, `IPluginContext`, `IPluginConfiguration`, `AdrEventContext`, `PluginResult`, …) without referencing the CLI tool, and lets the contract be versioned and released on its own schedule (tag `abstr-v*.*.*`), independent of the CLI's release cadence.

Key characteristics of the chosen design:

* `AdrPlus.Abstractions` contains only interfaces and immutable DTOs, resolved once by the host and never copied into plugin folders.
* An optional `AdrPluginBase` convenience class is included in the same package — it shields exceptions into `Failed` results and exposes `Success`/`Skip`/`Fail` helpers, but a plugin author may ignore it and implement `IAdrPlugin` directly.
* `AdrPlus.Abstractions.Testing` was added later as a separate namespace with factory helpers (`AdrRecordSnapshotFactory`, `RepoInfoSnapshotFactory`, `AdrEventContextFactory`) so plugin authors can build a valid `AdrEventContext` for their own unit tests in one call, instead of hand-filling every required field.
* A full plugin SDK (a `dotnet new` project template) was considered and rejected outright: a template package is a different NuGet package shape (content + `template.config`, not a compiled library) and would require a fourth separate distribution artifact and release pipeline — unjustified with zero external plugin authors today.

### Positive Consequences

* Plugin authors depend on exactly the interfaces and DTOs they need, without referencing the CLI tool itself.
* The public contract is versioned and released independently of the CLI, so plugin authors aren't forced to track CLI release cadence.
* `AdrPlus.Abstractions.Testing`'s factories let plugin authors unit test their own plugin without hand-building every DTO field.

### Negative Consequences

* One more package to version, document, and keep backward-compatible (`abstractionsVersion` SemVer-major compatibility is enforced by the host at plugin load time) — a breaking change here affects every plugin author, not just the CLI's own users.
* No project-scaffolding SDK exists yet — a new plugin author starts from documentation and the bundled `AdrIndexer` reference plugin's source, not a generated template.

## Links

* Related [ADR002](ADR002V01-add-a-plugin-system-for-adr-lifecycle-event-integrations.md) — the plugin dispatch model this contract serves
* Related [ADR003](ADR003V01-store-plugin-binaries-host-globally-instead-of-per-repository.md) — where plugin binaries built against this contract are discovered and stored
* Testing factories: commit `4c030e6` ("Add AdrPlus.Abstractions.Testing factories (D37); drop plugin SDK template from roadmap")
* Original design spec (preserved in git history, no longer in the working tree): `git show 197cd49:doc/Todo/adrplus-plugin-architecture.md` (commit `197cd49`)
