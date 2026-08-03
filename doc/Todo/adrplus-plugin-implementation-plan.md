# AdrPlus Plugin System — Implementation Plan

> **Based on**: `adrplus-plugin-architecture.md` (final spec, decisions D1–D31).
> **Post-v1 addendum (2026-08-03)**: D31 (active-plugin baseline + `DisablePlugins` kill switch, spec §3) shipped
> after all 11 phases below were already done — see spec §3's D31 row for the design. Not one of the original
> phases; noted here so this plan's own history stays accurate.
> **Scope**: Builds every decision tagged **Essential** in §3 of the spec. D23 (dedup) and D24 (aggregate dispatch timeout) were tagged "Deferred (v1.1+)" originally but are now **Rejected outright** (2026-08-03); D18's `maxConcurrency` is no longer a manifest field at all, having been removed rather than left as a future user-tunable knob. See "Out of scope" below.
> **Status**: All phases (1–11) implemented. Phase 10 shipped as `PluginDevelopmentGuide.md` (repo root), linked from `README.md`'s Table of Contents. Phase 5 built a real in-process retry loop against each plugin's `pending.json` (not a 1-attempt-per-invocation model) — `IPluginManager.RetryPendingAsync`, invoked by `SyncCommandHandler`'s default mode. Phase 6 added `--backfill` (full repo sweep, `IPluginManager.BackfillAsync`), reusing the same attempt-loop mechanics via a further-extracted `RunAttemptLoopAsync` shared with Phase 5's retry engine. `PluginManager` now has three layers of shared helpers: `EnsureInitializedAsync`/`InvokeOnceAsync` (Phase 4/5, per-attempt mechanics) and `RunAttemptLoopAsync` (Phase 5/6, the backoff loop around them). **Phase 11 deviated from plan**: instead of a `tests\`-only fixture, `AdrIndexer` shipped as a real bundled plugin project (`AdrPlus.Plugins.AdrIndexer`) staged into the adrplus package under `plugins-builtin\` and auto-installed into every new repo's `./plugins/adr-indexer/` by `adrplus init` (never overwriting an existing install) — verified end-to-end via `dotnet pack` contents and a real `init` + `plugins --list` run, in addition to the plan's fixture-based tests.

---

## Phase 1 — `AdrPlus.Abstractions` project

- New project `AdrPlus.Abstractions.csproj`, `TargetFramework=net10.0` only, matching the host (spec §4.1 — the host itself dropped its `net10.0;net9.0;net8.0` multi-target down to `net10.0` only; see risk note below).
- Add to `AdrPlus.slnx`.
- Define the public surface: `IAdrPlugin`, `IPluginContext`, `IPluginConfiguration`, `AdrEventContext` (with lazy `GetAdrRenderedContent`), `AdrEventType`, `PluginResult` (incl. `IsRetryable`), `PluginResultStatus`, `AdrRecordSnapshot`, `RepoInfoSnapshot`, `AdrPluginBase` (optional convenience: `Success()`/`Skip()`/`Fail(message)`/`Fail(message, isRetryable:false)`).
- Reference `AdrPlus.Abstractions` from `src\AdrPlus\AdrPlus.csproj`.

**Verify:** solution builds under net10.0; `AdrPlus.Abstractions` itself only targets net10.0.

---

## Phase 2 — Domain snapshots

- `AdrRecordSnapshot`: public immutable record mirroring `src\AdrPlus\Domain\AdrRecord.cs` (`Number`, `Version`, `Revision`, `Title`, `Domain`, `Scope`, `StatusCreate`/`StatusUpdate`/`StatusChange`, `CreateRef`/`UpdateRef`/`ChangeRef`, `Superseded`).
- `RepoInfoSnapshot`: public immutable subset of `src\AdrPlus\Domain\AdrPlusRepoConfig.cs` relevant to plugins.
- Internal mapping/extension methods `AdrRecord → AdrRecordSnapshot`, `AdrPlusRepoConfig → RepoInfoSnapshot`.

**Verify:** unit tests asserting every snapshot field matches its source field (in particular `Adr.Number`, since D26 depends on plugin authors using it correctly for cross-revision identity).

---

## Phase 3 — Plugin discovery & loading (`IPluginManager` / `PluginLoader`)

- New internal types (suggested location: `src\AdrPlus\Plugins\`): `IPluginManager`, `PluginManager`, `PluginLoader`.
- Folder-based discovery: enumerate `./plugins/<name>/` subfolders (D2).
- Parse `plugin.json` per the final schema (D12) — including `subscribedEvents`, `maxConcurrency`, `foregroundTimeoutMs`, `timeoutMs`, `retryPolicy`, `settings` (D27).
- One `AssemblyLoadContext` per subfolder; private deps via `AssemblyDependencyResolver` over `.deps.json` (D2/D6).
- Structural load validation (cheap, no plugin code runs): manifest schema, `entryType` implements `IAdrPlugin`, `Name`/`Version` match manifest, `abstractionsVersion` SemVer-major compatible (D13), allowlist check against `adrplus.json` (D22/§10), duplicate-`name` rejection (D22, both rejected).
- Register `IPluginManager` in `src\AdrPlus\Extensions\ServiceCollectionExtensions.cs` (`AddAdrPlusServices`).

**Verify:** fixture-based tests — valid plugin loads; each rejection case (bad manifest, `abstractionsVersion` major mismatch, missing/wrong `entryType`, duplicate name, not on allowlist) is rejected with a warning and does not crash the host.

---

## Phase 4 — Foreground dispatch

- `IPluginManager.DispatchAsync(AdrEventType, AdrRecordSnapshot, ...)` builds `AdrEventContext` (content stays lazy — `GetAdrRenderedContent` only invoked if a plugin's filter passes).
- Filter: `subscribedEvents` (manifest) + `ShouldHandle` (plugin code). **No dedup in v1** — D23 is deferred, and per-command foreground dispatch only ever emits one event per invocation, so there's nothing to deduplicate here; dedup only becomes relevant for `--backfill` (Phase 6, deferred).
- Lazy `InitializeAsync` (D30): call once per plugin per process, only right before the first subscribed event is about to dispatch to it. If it throws: skip this plugin for the rest of the run, one distinct warning, **no** `pending.json` entry.
- Enforce `foregroundTimeoutMs` (D27) via forced abandonment: race the hook's `Task` against `Task.Delay(foregroundTimeoutMs)` with `Task.WhenAny`; do not rely on the plugin honoring `CancellationToken` alone.
- Single-shot, no retry, in the foreground.
- Outcome handling: `Success`/`Skipped` → done. `Failed` with `IsRetryable: true` (default) and timeout → write `pending.json` entry (Phase 5's schema). `Failed` with `IsRetryable: false` (or `InitializeAsync` throwing) → distinct "permanent failure" warning, no `pending.json` entry (D30).
- Wire `await _pluginManager.LoadPluginsAsync(...)` (once) + `DispatchAsync(...)` into each of the 8 handlers, at the point the ADR write is confirmed successful. Two deviations from the original "one call each" phrasing, both deliberate: **`RejectCommandHandler` dispatches twice** — `Rejected` for the rejected ADR, and `StatusUndone` for a secondary ADR it had superseded, when applicable (otherwise that secondary ADR's plugins never learn it became active again). **`MigrateCommandHandler` dispatches once per migrated file inside its loop**, since one invocation can migrate N files. `SupersedeCommandHandler` dispatches only `Superseded` for the superseded ADR — the newly-created successor stays `Proposed` (pre-content, D26), so no event fires for it. Getting `AdrRecordSnapshot`/rendered content for `ApproveCommandHandler`/`RejectCommandHandler`/`UndoStatusCommandHandler`/`SupersedeCommandHandler` required extending `IAdrServices.StatusUpdateAdrAsync`/`StatusChangeSupersedeAdrAsync`/`StatusChangeAdrAsync` to also return the `AdrRecord`/content they already build internally (previously only `(bool, string)`).

**Verify:** per-handler integration test asserting the correct `AdrEventType` is dispatched; a fixture plugin that hangs past `foregroundTimeoutMs` doesn't block the test; a fixture plugin returning `IsRetryable:false` produces a warning and no pending entry; existing handler tests remain green (regression).

---

## Phase 5 — Pending state & background re-drive (`adrplus sync`, default mode)

- `./plugins/<name>/state/pending.json` read/write, schema per §7 (`adrKey`, `eventType`, `correlationId`, `lastError`, `attempts`, `timestamp`).
- Background retry engine implementing `retryPolicy` (`maxAttempts`, `Fixed`/`Exponential` backoff, `jitter`) — §4.4's formula.
- New `SyncCommandHandler`, default (no-flag) mode: for each plugin, iterate its `pending.json`, re-attempt via `retryPolicy`, remove entry on success, update `attempts`/`lastError` on failure.
- Register `sync` in `src\AdrPlus\Commands\CommandsAdr.cs` (enum + routing).

**Verify:** unit tests for the backoff/jitter formula; integration test seeding a `pending.json` fixture, running `sync`, asserting successful items are removed and failed ones have updated `attempts`.

**Implemented as:** a real in-process retry loop per pending entry within one `sync` invocation (not one attempt per invocation) — `Task.Delay` is honored between attempts, matching §4.4's "timeout honored between attempts" and the jitter formula's `random(0, delay(n))`. Key decisions locked in during implementation:
- **Esgotamento não é permanente**: quando `maxAttempts` se esgota dentro de uma execução, a entrada continua em `pending.json` (não removida, não convertida em falha permanente) com `attempts`/`lastError`/`timestamp` atualizados — `attempts` é cumulativo entre execuções. Sustenta a promessa do §6 de entrega eventual via `sync` em cron sem daemon.
- **Toda entrada recebe ao menos 1 tentativa por execução**, mesmo que `attempts` acumulado já tenha atingido `maxAttempts` em execuções anteriores — sem essa garantia, a política acima vira silenciosamente "nunca mais tenta".
- **`ShouldHandle` é reavaliado no retry** (o contexto é reconstruído do estado atual do arquivo, que pode ter mudado desde a falha original); `false` é tratado como `Skipped`.
- **ADR que não resolve mais para um arquivo** (deletado/renomeado) descarta a entrada com aviso distinto, contabilizado separadamente no resumo do comando.
- **`PluginRetryPolicy.Backoff`/`Jitter` defaults corrigidos** de `Fixed`/`false` (bug introduzido na Fase 3, nunca batia com o spec) para `Exponential`/`true`, batendo com §4.4.
- **`PluginManager.DispatchAsync`/`DispatchToPluginAsync` (Fase 4) refatorados** para extrair `EnsureInitializedAsync`/`InvokeOnceAsync`, reusados por `RetryPendingAsync` — mesma mecânica de init-once-por-processo e corrida de timeout, parametrizada por `timeoutMs` (`ForegroundTimeoutMs` no caminho síncrono, `TimeoutMs` no motor de retry). Refatoração comportamento-preservada, confirmada pelos testes da Fase 4 continuando 100% verdes.
- **`IPluginManager.RetryPendingAsync` recebe um resolver `Func<string, (AdrRecordSnapshot, string, string)?>`** em vez de depender de `IAdrServices`/`AdrPlusRepoConfig` diretamente — quem resolve `adrKey → arquivo` é o `SyncCommandHandler` (que já tem `IAdrServices`), mantendo `IPluginManager` só dependente de tipos de `AdrPlus.Abstractions`.
- **`adrKey` format extraído** para `AdrKeyFormatter.Format(number, version, revision)` (`src/AdrPlus/Plugins/AdrKeyFormatter.cs`), compartilhado entre `PluginManager` e `SyncCommandHandler`.
- **`PendingStateWriter` renomeado para `PendingStateStore`** (passou a ler, não só escrever) — ganhou `ReadAllAsync`/`WriteAllAsync` (lista inteira, um read/write por plugin por execução) ao lado do `UpsertAsync` já existente da Fase 4.

---

## Phase 6 — Full backfill (`adrplus sync --backfill`) — **Deferred? No — Essential.**

- `SyncCommandHandler --backfill` mode: enumerate every ADR (reuse `IAdrServices.ReadAllAdr`), determine each one's current settled status (`Approved`/`Rejected`/`Superseded`/`Migrated`; skip `Proposed`), build `AdrEventContext` with `IsReplay=true`, dispatch to each plugin using the **full** `retryPolicy` per item (not the foreground single-shot path — the user chose to wait by running this command).
- On exhausted retries during `--backfill`: **log only, do not write `pending.json`** (§6 — `--backfill` is itself idempotent and rediscovers everything on the next run; recovery is "run `--backfill` again," not accumulating hundreds of pending entries from one bad sweep).
- Enforce `maxConcurrency=1` (fixed, D18) across the ADRs being swept for a given plugin — serializes the sweep so it doesn't burst-hammer the external system.
- **Never wire `--backfill` into an automated scheduler** — document this prominently in user-facing help text (`adrplus sync --help`) and any cron/CI setup guidance, since it's the one footgun in this design that's easy to get wrong operationally.

**Verify:** fixture repo with ADRs across all statuses — assert only settled ones dispatch; assert plugin invocations for this sweep are serialized (no concurrent calls); assert exhausted-retry failures are logged, not written to `pending.json`.

**Implemented as:** `IPluginManager.BackfillAsync(IEnumerable<(AdrEventType, AdrRecordSnapshot, string FilePath, Func<string> GetContent)>, RepoInfoSnapshot, CancellationToken)` — `SyncCommandHandler` owns reading the repo and mapping each ADR's `AdrHeader` to a settled `AdrEventType` (`DetermineSettledEventType`: `StatusChange==Superseded` > `StatusUpdate==Rejected` > `StatusUpdate==Accepted` > `IsMigrated` > skip), same separation of concerns as Phase 5's `resolveAdr` callback. Key decisions:
- **`RunAttemptLoopAsync` extracted from Phase 5's `RetryEntryAsync`** — the backoff loop itself (using `ComputeDelay`/`InvokeOnceAsync`) is identical between "retry a pending entry" and "backfill one settled ADR"; only the bookkeeping around the loop differs (Phase 5: update/remove a `PendingEntry`; Phase 6: log-only via `AttemptLoopResult.Exhausted`, never persisted). Verified behavior-preserving against Phase 5's own test suite.
- **D18 concurrency**: different plugins swept in parallel (`Task.WhenAll`, same pattern as Phase 4's `DispatchAsync`), but each plugin processes its own ADR list sequentially — no `Task.WhenAll` inside a single plugin's sweep.
- **Real bug caught in plan review, fixed before coding**: `_initializedPlugins`/`_initFailedPlugins` are plain `HashSet<string>`, not thread-safe. `BackfillAsync` runs `EnsureInitializedAsync` for every plugin **sequentially before** the parallel per-plugin sweep starts — those sets are only ever read (never mutated) once concurrency begins.
- **Cancellation mid-sweep returns the partial `SyncSummary`**, doesn't lose it — `BackfillPluginAsync` catches `OperationCanceledException` per item and returns whatever that plugin already accomplished, since a backfill sweep (full `retryPolicy` per item, sequential per plugin) can run for a long time and Ctrl-C shouldn't erase prior progress.
- **`SyncSummary` gained an `Exhausted` field** (backfill-only; `StillPending`/`Dropped` are default-mode-only) and a distinct `BackfillSummaryReport` string — avoids showing always-zero fields depending on which mode ran.
- D23 (dedup) and D18's user-tunability remain out of scope, as before.

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

- D23 — dedup engine (`adrKey + eventType` before dispatch). **Rejected 2026-08-03**, not deferred — see spec §3.
- D24 — aggregate dispatch timeout across plugins. **Rejected 2026-08-03**, not deferred — see spec §3.
- D18 — `maxConcurrency` no longer exists as a concept to defer: the manifest field was removed entirely 2026-08-03 (it was never read by the host). Per-plugin concurrency stays fixed at 1.
- Everything in the spec's §14 Roadmap: host secrets API, sandboxing beyond timeout, hot-reload, plugin-to-plugin communication, Plugin SDK/template + test harness, `adrplus plugin install <package>` distribution flow.

---

## Risk — confirmed 2026-07-31

`AdrPlus.Abstractions` targeting `net10.0` only is a one-way door for the contract itself (not for plugin authors — see spec §4.1): if it ever needs a future-TFM-only BCL type internally, the floor must move, breaking existing plugins. Low probability given the project is only interfaces and immutable records. **Superseded decision, also confirmed 2026-07-31:** the host (`src/AdrPlus/AdrPlus.csproj`, `tests/AdrPlus.Tests/AdrPlus.Tests.csproj`) dropped its `net10.0;net9.0;net8.0` multi-target down to `net10.0` only — .NET 9 (STS) is already past EOL and .NET 8 (LTS) reaches EOL November 2026. `AdrPlus.Abstractions` matches at `net10.0` only; there is no lower-TFM host build left to stay compatible with.
