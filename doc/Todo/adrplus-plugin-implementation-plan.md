# AdrPlus Plugin System — Implementation Plan

> **Based on**: `adrplus-plugin-architecture.md` (final spec, decisions D1–D30).
> **Scope**: Builds every decision tagged **Essential** in §3 of the spec. Decisions tagged **Deferred (v1.1+)** — D23 (dedup), D24 (aggregate dispatch timeout), D18's `maxConcurrency` user-tunability — are explicitly **not** built here; see "Out of scope" below.
> **Status**: Planning only. Nothing in this document has been executed.

---

## Phase 1 — `AdrPlus.Abstractions` project

- New project `AdrPlus.Abstractions.csproj`, `TargetFramework=net8.0` only (spec §4.1 — a deliberate one-way door for the contract itself, not for plugin authors; confirm this trade-off is accepted before locking it in).
- Add to `AdrPlus.sln`.
- Define the public surface: `IAdrPlugin`, `IPluginContext`, `IPluginConfiguration`, `AdrEventContext` (with lazy `GetAdrRenderedContent`), `AdrEventType`, `PluginResult` (incl. `IsRetryable`), `PluginResultStatus`, `AdrRecordSnapshot`, `RepoInfoSnapshot`, `AdrPluginBase` (optional convenience: `Success()`/`Skip()`/`Fail(message)`/`Fail(message, isRetryable:false)`).
- Reference `AdrPlus.Abstractions` from `src\AdrPlus.csproj`.

**Verify:** solution builds under net8/net9/net10; `AdrPlus.Abstractions` itself only targets net8.0.

---

## Phase 2 — Domain snapshots

- `AdrRecordSnapshot`: public immutable record mirroring `src\Domain\AdrRecord.cs` (`Number`, `Version`, `Revision`, `Title`, `Domain`, `Scope`, `StatusCreate`/`StatusUpdate`/`StatusChange`, `CreateRef`/`UpdateRef`/`ChangeRef`, `Superseded`).
- `RepoInfoSnapshot`: public immutable subset of `src\Domain\AdrPlusRepoConfig.cs` relevant to plugins.
- Internal mapping/extension methods `AdrRecord → AdrRecordSnapshot`, `AdrPlusRepoConfig → RepoInfoSnapshot`.

**Verify:** unit tests asserting every snapshot field matches its source field (in particular `Adr.Number`, since D26 depends on plugin authors using it correctly for cross-revision identity).

---

## Phase 3 — Plugin discovery & loading (`IPluginManager` / `PluginLoader`)

- New internal types (suggested location: `src\Plugins\`): `IPluginManager`, `PluginManager`, `PluginLoader`.
- Folder-based discovery: enumerate `./plugins/<name>/` subfolders (D2).
- Parse `plugin.json` per the final schema (D12) — including `subscribedEvents`, `maxConcurrency`, `foregroundTimeoutMs`, `timeoutMs`, `retryPolicy`, `settings` (D27).
- One `AssemblyLoadContext` per subfolder; private deps via `AssemblyDependencyResolver` over `.deps.json` (D2/D6).
- Structural load validation (cheap, no plugin code runs): manifest schema, `entryType` implements `IAdrPlugin`, `Name`/`Version` match manifest, `abstractionsVersion` SemVer-major compatible (D13), allowlist check against `adrplus.json` (D22/§10), duplicate-`name` rejection (D22, both rejected).
- Register `IPluginManager` in `src\Extensions\ServiceCollectionExtensions.cs` (`AddAdrPlusServices`).

**Verify:** fixture-based tests — valid plugin loads; each rejection case (bad manifest, `abstractionsVersion` major mismatch, missing/wrong `entryType`, duplicate name, not on allowlist) is rejected with a warning and does not crash the host.

---

## Phase 4 — Foreground dispatch

- `IPluginManager.DispatchAsync(AdrEventType, AdrRecordSnapshot, ...)` builds `AdrEventContext` (content stays lazy — `GetAdrRenderedContent` only invoked if a plugin's filter passes).
- Filter: `subscribedEvents` (manifest) + `ShouldHandle` (plugin code). **No dedup in v1** — D23 is deferred, and per-command foreground dispatch only ever emits one event per invocation, so there's nothing to deduplicate here; dedup only becomes relevant for `--backfill` (Phase 6, deferred).
- Lazy `InitializeAsync` (D30): call once per plugin per process, only right before the first subscribed event is about to dispatch to it. If it throws: skip this plugin for the rest of the run, one distinct warning, **no** `pending.json` entry.
- Enforce `foregroundTimeoutMs` (D27) via forced abandonment: race the hook's `Task` against `Task.Delay(foregroundTimeoutMs)` with `Task.WhenAny`; do not rely on the plugin honoring `CancellationToken` alone.
- Single-shot, no retry, in the foreground.
- Outcome handling: `Success`/`Skipped` → done. `Failed` with `IsRetryable: true` (default) and timeout → write `pending.json` entry (Phase 5's schema). `Failed` with `IsRetryable: false` (or `InitializeAsync` throwing) → distinct "permanent failure" warning, no `pending.json` entry (D30).
- Wire **one** `await _pluginManager.DispatchAsync(...)` at the end of `ExecuteAsync` in each of: `NewAdrCommandHandler`, `VersionCommandHandler`, `ReviseCommandHandler`, `SupersedeCommandHandler`, `ApproveCommandHandler`, `RejectCommandHandler`, `UndoStatusCommandHandler`, `MigrateCommandHandler` (§5/§11 — the only touch points in existing handlers).

**Verify:** per-handler integration test asserting the correct `AdrEventType` is dispatched; a fixture plugin that hangs past `foregroundTimeoutMs` doesn't block the test; a fixture plugin returning `IsRetryable:false` produces a warning and no pending entry; existing handler tests remain green (regression).

---

## Phase 5 — Pending state & background re-drive (`adrplus sync`, default mode)

- `./plugins/<name>/state/pending.json` read/write, schema per §7 (`adrKey`, `eventType`, `correlationId`, `lastError`, `attempts`, `timestamp`).
- Background retry engine implementing `retryPolicy` (`maxAttempts`, `Fixed`/`Exponential` backoff, `jitter`) — §4.4's formula.
- New `SyncCommandHandler`, default (no-flag) mode: for each plugin, iterate its `pending.json`, re-attempt via `retryPolicy`, remove entry on success, update `attempts`/`lastError` on failure.
- Register `sync` in `src\Commands\CommandsAdr.cs` (enum + routing).

**Verify:** unit tests for the backoff/jitter formula; integration test seeding a `pending.json` fixture, running `sync`, asserting successful items are removed and failed ones have updated `attempts`.

---

## Phase 6 — Full backfill (`adrplus sync --backfill`) — **Deferred? No — Essential.**

- `SyncCommandHandler --backfill` mode: enumerate every ADR (reuse `IAdrServices.ReadAllAdr`), determine each one's current settled status (`Approved`/`Rejected`/`Superseded`/`Migrated`; skip `Proposed`), build `AdrEventContext` with `IsReplay=true`, dispatch to each plugin using the **full** `retryPolicy` per item (not the foreground single-shot path — the user chose to wait by running this command).
- On exhausted retries during `--backfill`: **log only, do not write `pending.json`** (§6 — `--backfill` is itself idempotent and rediscovers everything on the next run; recovery is "run `--backfill` again," not accumulating hundreds of pending entries from one bad sweep).
- Enforce `maxConcurrency=1` (fixed, D18) across the ADRs being swept for a given plugin — serializes the sweep so it doesn't burst-hammer the external system.
- **Never wire `--backfill` into an automated scheduler** — document this prominently in user-facing help text (`adrplus sync --help`) and any cron/CI setup guidance, since it's the one footgun in this design that's easy to get wrong operationally.

**Verify:** fixture repo with ADRs across all statuses — assert only settled ones dispatch; assert plugin invocations for this sweep are serialized (no concurrent calls); assert exhausted-retry failures are logged, not written to `pending.json`.

---

## Phase 7 — Diagnostics commands

- `adrplus plugins list`: loaded plugins, versions, `subscribedEvents`, allowlist status, and a pending-item count per plugin (read from each `pending.json`).
- `adrplus plugins validate`: re-runs Phase 3's structural validation and reports without dispatching any real event.
- Register `plugins` (with `list`/`validate` subcommands) in `CommandsAdr.cs`.

**Verify:** fixture-based tests mirroring Phase 3's cases, asserting `validate` reports each correctly without side effects.

---

## Phase 8 — Logging, localization, security

- `IPluginLogger`/`IPluginContext` (D20): `ILogger`-backed, carries `CorrelationId`.
- New host-message strings for the two distinct warnings (D30): "queued for retry" (transient) vs. "permanent failure, not queued — fix configuration and run `adrplus sync --backfill`" — add to `Resources\AdrPlus.resx` and all 10 translated `.resx` files, consistent with the existing localization pattern (flag new strings as pending native-speaker review per `TRANSLATIONS.md`, same as other recent additions).
- Allowlist: extend `adrplus.json` schema (names and/or assembly hashes) and wire into Phase 3.
- Security note (§10): document that `plugin.json` may be committed to the repo (D29) and that `settings` must never contain real credential values — this is a docs/process safeguard, not host-enforced.

**Verify:** existing localization test pattern extended to cover the new strings.

---

## Phase 9 — Shutdown & disposal

- Graceful-shutdown hook: `DisposeAsync` every loaded plugin; flush any pending-state writes still in flight; ALC unload as a best-effort "good citizen" step (not required for correctness in a short-lived CLI process, D19).

**Verify:** test that `DisposeAsync` is invoked for every loaded plugin on process exit, including exception/cancellation paths.

---

## Phase 10 — Plugin-author documentation

- New guide covering: the `IAdrPlugin` contract; which events to subscribe to and why (D26 — `Approved`/`Rejected`/`Superseded`/`StatusUndone`/`Migrated` for "sync the decision," not `Created`/`Revised`/`Versioned`); `ExternalKey`/`adrKey` scope guidance (D26 — use `Adr.Number` for cross-revision continuity, not `adrKey`); the retryable-vs-permanent-failure contract (D30) and that it's protocol-agnostic (works the same whether the plugin talks HTTP, gRPC, UDP, or anything else); the deployment-model implication (D29 — credentials must be present on every developer machine that might run a subscribed command); the secrets-in-`plugin.json` risk (§10).

**Verify:** N/A (documentation).

---

## Phase 11 — Reference/example plugin

- A minimal example plugin (fixture under `tests\`, or a small sample project) implementing `IAdrPlugin` against a fake HTTP endpoint — serves as an end-to-end smoke test and a living example for future plugin authors, standing in for the full Plugin SDK (which is Roadmap/v2 scope, not this plan).

**Verify:** end-to-end test — `adrplus approve` against a fixture repo with this plugin installed dispatches to it and handles its `PluginResult` correctly, including a simulated timeout and a simulated permanent failure.

---

## Suggested sequencing

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6
                                   ↘ Phase 7 (needs Phase 3)
Phase 8 (cross-cutting, start once Phase 3/4 land)
Phase 9 (needs Phase 3/4)
Phase 11 (validates everything — do last, but stub early to unblock testing Phase 4 onward)
Phase 10 (last — documents the finished behavior)
```

---

## Definition of Done for v1

- Every decision tagged **Essential** in the spec's §3 is implemented and covered by a test.
- Decisions tagged **Deferred (v1.1+)** are **not** implemented — tracked as backlog, not silently dropped.
- The only changes to the 8 existing command handlers are the single `DispatchAsync` call each (Phase 4) — no other modifications.
- Regression: with an empty `./plugins` folder, all existing command behavior is byte-for-byte unchanged (dispatch is a no-op).

---

## Out of scope for this plan (do not build)

- D23 — dedup engine (`adrKey + eventType` before dispatch).
- D24 — aggregate dispatch timeout across plugins.
- D18 — `maxConcurrency` as a user-configurable value (stays fixed at 1).
- Everything in the spec's §14 Roadmap: host secrets API, sandboxing beyond timeout, hot-reload, plugin-to-plugin communication, Plugin SDK/template + test harness, `adrplus plugin install <package>` distribution flow.

---

## Risk to confirm before starting Phase 1

`AdrPlus.Abstractions` targeting `net8.0` only is a one-way door for the contract itself (not for plugin authors — see spec §4.1): if it ever needs a net9.0/net10.0-only BCL type internally, the floor must move, breaking existing plugins. Low probability given the project is only interfaces and immutable records, but worth an explicit go/no-go before Phase 1 starts.
