<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Add a plugin system for ADR lifecycle event integrations|
|Version|01|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-08-04)|
|Changed|Accepted (2026-08-04)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Add a plugin system for ADR lifecycle event integrations

## Deciders

* Deciders: Fernando Cerqueira (project maintainer)

## Context and Problem Statement

External tools (Confluence, Jira, Teams, a search index, …) often need to react when an ADR's status settles — for example, mirroring an approved decision into a Confluence page. Without an extensibility mechanism, every new integration requires a bespoke change to the AdrPlus core, coupling it to integrations it shouldn't need to know about. How should AdrPlus let external systems react to ADR lifecycle events without the core depending on any of them?

## Considered Options

* No extensibility mechanism — hard-code each integration into the core
* A plugin system with a single, non-retried, timeout-bounded dispatch inline in the foreground, and full retry only during a separate background re-drive
* A plugin system with full synchronous retries running inline in the foreground of every command

## Decision Outcome

Chosen option: "A plugin system with a single, non-retried, timeout-bounded dispatch inline in the foreground, and full retry only during a separate background re-drive", because it lets the core stay integration-agnostic (it only emits events to a public contract, `IAdrPlugin`) while keeping interactive commands fast regardless of how unhealthy an external system is.

Key characteristics of the chosen design:

* **Fail-soft**: a plugin failure warns and logs, but never aborts the local ADR operation; the process exit code reflects only the local file operation, never plugin outcomes.
* **Foreground/background dispatch split**: when a command settles an ADR's status, each subscribed plugin gets exactly one bounded attempt (`foregroundTimeoutMs`, default 5000ms) inline. If that attempt doesn't succeed, the host immediately records per-plugin pending state and returns control to the user — it does not block on retries.
* **`adrplus sync`** (no flags) re-drives whatever is sitting in pending state, running the full retry policy (`maxAttempts`, `Fixed`/`Exponential` backoff, jitter). It's self-limiting, so it's safe to automate via cron/CI.
* **`adrplus sync --backfill`** sweeps every existing ADR and re-emits its current settled-status event — the only way a plugin installed on a repo that already has ADRs ever sees the history. It is deliberately manual and must never be wired into a scheduler.
* **Retryable vs. permanent failures are distinguished** (`PluginResult.IsRetryable`): a bad credential fails identically on every retry, so permanent failures get one prominent warning instead of being queued for automatic retry.
* **Diagnostics**: `adrplus plugins --list`/`--validate` report loaded plugins, subscribed events, allowlist status, and pending-item counts.
* **Security**: an optional allowlist gates which plugin names may load; plugins run with the user's own OS permissions; secrets are entirely the plugin's own responsibility — the host provides no secrets API, only a logger and correlation id.
* A reference plugin, `AdrIndexer`, ships bundled with the tool as a working example and to auto-generate a per-repo ADR index.

### Positive Consequences

* External integrations can react to ADR lifecycle events without any core change per integration.
* Interactive command latency is bounded (at most `foregroundTimeoutMs` per plugin) regardless of how unhealthy an external system is, instead of blocking on a full retry schedule.
* A bundled reference plugin (`AdrIndexer`) gives plugin authors a working example and gives every repo using AdrPlus an auto-maintained ADR index for free.

### Negative Consequences

* Synchronization with external systems is eventual (retry + idempotency + reconcilable pending state), not transactional — a developer without valid plugin credentials configured locally simply doesn't sync, silently from the command's own exit code (though loudly via warnings), until someone runs `adrplus sync --backfill`.
* Delivery is not automatic over time — there is no background daemon; a failed dispatch sits in pending state until something explicitly runs `adrplus sync` again (a human, a script, or a scheduler the deploying team must set up themselves).
* Coverage depends on which developer's machine ran the command and what plugin credentials are configured there (v1 is local/per-developer, not CI-authoritative) — accepted as a v1 trade-off; a CI-authoritative model (a pipeline running `adrplus sync --backfill` centrally with one shared credential) is a coherent alternative but a larger scope change, deferred pending real usage data.
* Secrets management is entirely delegated to each plugin, so plugin configuration UX is heterogeneous across plugins by design.

## Links

* Original design spec (preserved in git history, no longer in the working tree): `git show 197cd49:doc/Todo/adrplus-plugin-architecture.md` (commit `197cd49`, "Add reviewed plugin-architecture spec and implementation plan")
* Related [ADR003](ADR003V01-store-plugin-binaries-host-globally-instead-of-per-repository.md) — where plugin binaries are discovered and stored
* Related [ADR004](ADR004V01-publish-the-plugin-contract-as-an-independent-adr-plus-abstractions-package.md) — how the plugin contract is packaged and distributed
