# AdrPlus Plugin System — Implementation Plan

> **Based on**: `adrplus-plugin-architecture.md` (final spec, decisions D1–D31).
> **Post-v1 addendum (2026-08-03)**: D31 (active-plugin baseline + `DisablePlugins` kill switch, spec §3) shipped
> after all 11 phases below were already done — see spec §3's D31 row for the design. D32 (CLI visibility: warn
> on `Missing` only, no success-path noise) and D33 (install-level "Bundled with this AdrPlus install" line in
> `adrplus wizard`'s menu) followed shortly after, refining D31's display down to its final shape. None of D31–D33
> are original phases; noted here so this plan's own history stays accurate.
> **Scope**: Builds every decision tagged **Essential** in §3 of the spec. D23 (dedup) and D24 (aggregate dispatch timeout) were tagged "Deferred (v1.1+)" originally but are now **Rejected outright** (2026-08-03); D18's `maxConcurrency` is no longer a manifest field at all, having been removed rather than left as a future user-tunable knob. See "Out of scope" below.
> **Pivot (2026-08-03, same day as Phase 12/13): Phase 14 (D36) rewrites plugin discovery from per-repo to host-global.** Phases 12/13 below were implemented once against the per-repo `./plugins/<name>/` model and committed as a checkpoint (`fff9de7`) before the pivot was decided; this doc's Phase 12/13 sections still describe that pre-pivot shape verbatim as a historical record, and Phase 14 documents what changes on top of them. Read Phase 14 to know the post-pivot behavior; it does not re-implement 12/13 from scratch — it retargets specific pieces of what they already built, and is itself now implemented (committed `21d443a`).
> **Status**: All 15 phases implemented and tested, committed as `fff9de7` (Phases 1–13, pre-pivot model), `21d443a` (Phase 14/D36, the host-global pivot), and Phase 15/D37 (test-harness factories, not yet committed as of this writing — see spec §14/D37). Combined suite: `AdrPlus.Tests` 1322 green, `AdrPlus.Abstractions.Tests` 13 green (was 5). Phase 12 (D34, activation flags): `PluginsCommandHandler.SetActivePluginAsync`/`WriteActivePluginsAndReportAsync`, 5 new tests. Phase 13 (D35, install/uninstall): `InstallPluginAsync`/`UninstallPluginAsync`, zip extracted to a same-volume staging folder under the repo's own `./plugins/` then `Directory.Move`d into place, 8 new tests exercising real disk I/O (`PluginsCommandHandlerInstallTests.cs`). Both phases done 2026-08-03; `PluginsWizardMode` gained `Install`/`Uninstall` (with `PromptInputPluginZipPath`/`PromptSelectPluginsToUninstall`, the latter a `MultiSelect` — uninstall can remove several plugins in one wizard run, each processed one at a time via `UninstallPluginAsync`) so `adrplus plugins --wizard` covers both new flags too — full suite at 1323 tests green. Phase 10 shipped as `PluginDevelopmentGuide.md` (repo root), linked from `README.md`'s Table of Contents. Phase 5 built a real in-process retry loop against each plugin's `pending.json` (not a 1-attempt-per-invocation model) — `IPluginManager.RetryPendingAsync`, invoked by `SyncCommandHandler`'s default mode. Phase 6 added `--backfill` (full repo sweep, `IPluginManager.BackfillAsync`), reusing the same attempt-loop mechanics via a further-extracted `RunAttemptLoopAsync` shared with Phase 5's retry engine. `PluginManager` now has three layers of shared helpers: `EnsureInitializedAsync`/`InvokeOnceAsync` (Phase 4/5, per-attempt mechanics) and `RunAttemptLoopAsync` (Phase 5/6, the backoff loop around them). **Phase 11 deviated from plan**: instead of a `tests\`-only fixture, `AdrIndexer` shipped as a real bundled plugin project (`AdrPlus.Plugins.AdrIndexer`) staged into the adrplus package under `plugins-builtin\` and auto-installed into every new repo's `./plugins/adr-indexer/` by `adrplus init` (never overwriting an existing install) — verified end-to-end via `dotnet pack` contents and a real `init` + `plugins --list` run, in addition to the plan's fixture-based tests. **Bug found and fixed 2026-08-03**: the plugin's own `.csproj` file was on disk under a mistyped name, silently breaking the `ReferenceOutputAssembly="false"` build-order dependency in `AdrPlus.csproj` (MSB9008, swallowed by a stale `bin/` output during regular solution builds) — fixed by renaming the file back to `AdrPlus.Plugins.AdrIndexer.csproj` to match the `.slnx`/`AdrPlus.csproj`/`AdrPlus.Tests.csproj` references, which were always correct.

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

## Phase 12 — Non-interactive activation management (D34)

- `Arguments.PluginsActivate`/`PluginsDeactivate` on `adrplus plugins` (mutually exclusive with `--list`/`--validate`/`--wizard`, same style as the existing flags).
- Extract `RunManageActivePluginsAsync`'s read-config → compute-new-set → `ActivePluginsWriter.WriteAsync` → report sequence into a shared private helper that takes the already-computed target set; the wizard path builds that set from its multi-select (full replace, unchanged), `--activate`/`--deactivate` build it via union/except of the current `ActivePlugins` with the one given name.
- Neither flag validates that the name matches a loaded plugin — a typo is caught later by D31/D32's existing `Missing` warning, not duplicated here.
- One name per invocation; no comma-separated list (symmetry with Phase 13's `--install`/`--uninstall <name>`).

**Verify:** unit tests mirroring `PluginsCommandHandlerTests.cs`'s existing wizard-manage coverage — activating a name adds it (idempotent if already present), deactivating removes it (no-op if absent), and the wizard's own multi-select path stays green against the shared helper (behavior-preserving refactor, same bar as Phase 5/6's extractions).

---

## Phase 13 — Plugin distribution via zip: `--install`/`--uninstall` (D35)

- `Arguments.PluginsInstall <path>`/`PluginsUninstall <name>` on `adrplus plugins`.
- **Install**: validate the zip filename matches `<name>-<version>.zip`; extract to a temp location guarding every entry against path traversal (mirrors Phase 3's `entryAssembly` guard); read the `plugin.json` inside and fail fast if its `Name`/`Version` don't match the filename; if `./plugins/<name>/` doesn't exist, copy everything in; if it does exist, refuse unless `--force`, in which case overwrite unconditionally (including `plugin.json`/`state/` — accepted trade-off, no surgical merge). Never writes to `activeplugins`. On success, runs the same validation `PluginLoader.ValidateManifestAsync`/`LoadAssembly` uses for `--validate` against the new folder and prints the result; prints the zip's SHA256 for optional allowlist use.
- **Uninstall**: delete `./plugins/<name>/` recursively, then reuse Phase 12's deactivate helper to remove `name` from `activeplugins` (cleanup, not a trust decision — leaving it would only produce a permanent `Missing` warning).
- Local file path only for `--install`'s `<path>` — no URL/registry fetch.

**Verify:** end-to-end tests building an in-memory `ZipArchive` fixture (valid manifest, mismatched filename, path-traversal entry, missing manifest) and asserting install success/failure per case; a round-trip install-then-uninstall test confirming both the folder and the `activeplugins` entry are gone; an upgrade-with-`--force` test confirming a customized `plugin.json` is NOT preserved (documents the accepted trade-off, not a bug).

**Superseded by Phase 14** — the round-trip test's "and the `activeplugins` entry are gone" assertion no longer holds post-pivot (D36: `--uninstall` no longer touches `activeplugins`, see Phase 14). Kept here as a historical record of the pre-pivot design; Phase 14 lists the concrete test changes.

---

## Phase 14 — Host-global plugin store pivot (D36)

Retargets Phase 3 (discovery), Phase 5/6 (pending state location), Phase 11 (builtin-plugin install-into-repo), and Phase 12/13 (activation source, install/uninstall target) to the host-global model in spec D2/D36. Does not redo Phase 4/7/8/9/10's own work — those are unaffected in shape, only in where `PluginManager` looks.

- **New stable folder**: `%UserProfile%/AdrPlus.Plugins/<name>/` — user-installed plugins, one version per name. New, not reused from `%UserProfile%/AdrPlus.History/` (different purpose — see D36's rationale).
- **`PluginManager.LoadPluginsAsync` discovers from two merged roots** instead of one: `plugins-builtin/` (unchanged, D33) and the new user-installed root. Existing Stage-1/Stage-2 validation (manifest, allowlist, duplicate-name dedup) runs once over the merged candidate set — no change to the dedup logic itself, just what feeds it. Signature likely becomes `LoadPluginsAsync(IEnumerable<string> pluginsRoots, ...)` or two explicit root parameters; either way, callers (every one of the 8 dispatching handlers, `InitCommandHandler`, `PluginsCommandHandler`, `SyncCommandHandler`) need updating.
- **Pending-state root split (the correctness-critical piece — see D36's rationale for why this can't be skipped)**: `PendingStateStore` calls currently pass `plugin.FolderPath` (now host-global, shared). `DispatchAsync`/`RetryPendingAsync`/`BackfillAsync`/`HandleFailureAsync` take a new explicit repo-scoped parameter (e.g. `pendingStateRoot`) resolving to `<repo>/plugins-state/<name>/pending.json` (was `<repo>/plugins/<name>/state/pending.json`). Every one of the 8 dispatching handlers plus `SyncCommandHandler` passes this through — mechanical but touches every call site Phase 4/5/6 built.
- **Testability seam**, matching the existing `builtinPluginsRoot` pattern (`InitCommandHandler`, wired via `ServiceCollectionExtensions`'s factory registration with `Path.Combine(AppContext.BaseDirectory, "plugins-builtin")`): a new `userPluginsRoot`-style parameter, default `""`, resolving to the real `%UserProfile%/AdrPlus.Plugins` only in the production factory registration — never touched by tests unless they explicitly set it.
- **`InitCommandHandler.InstallBuiltinPlugins` (Phase 11) is removed** — copying the bundled AdrIndexer into a new repo's `./plugins/adr-indexer/` no longer makes sense once bundled plugins are discovered host-globally without any per-repo copy. `WriteActivePluginsBaselineAsync` still runs (seeds a fresh repo's `ActivePlugins` baseline), but now calls `LoadPluginsAsync` against the merged host-global roots instead of a freshly-populated `./plugins/`. Retire `InitCommandHandlerBuiltinPluginsTests.cs`'s copy-assertion tests; keep/adapt the baseline-seeding tests.
- **`PluginsCommandHandler.InstallPluginAsync`/`UninstallPluginAsync` (Phase 13) retarget** to `%UserProfile%/AdrPlus.Plugins/<name>/`; **drop the `--path`/`TargetRepo` requirement entirely for `--install`/`--uninstall`** (host-global, no repo in scope) — only `--list`/`--validate`/`--activate`/`--deactivate` still need `--path`. `UninstallPluginAsync` **no longer calls `SetActivePluginAsync`** — see D35's revised row for why (no safe repo to edit). Wizard's Install/Uninstall branches (`PluginsWizardMode.Install`/`Uninstall`) no longer need the shared folder-selection prompt that List/Validate/Manage still use — install/uninstall become the two modes that skip straight to their own prompt (zip path / multi-select) without ever asking for a repo folder.
- **`SetActivePluginAsync`/`--activate`/`--deactivate` (Phase 12) are unchanged in shape** — still per-repo, still edit `adr-config.adrplus`'s `ActivePlugins`, still require `--path`. Only the thing being activated (what `LoadPluginsAsync` finds) moves.
- **`init` on an existing repo**: no new code path — `Missing` (D31/D32) already fires when `ActivePlugins` references a name the (now host-global) discovery doesn't find; nothing to add beyond re-pointing discovery.
- **Documentation**: README/FAQ gain a "removing AdrPlus completely" note listing both `%UserProfile%/AdrPlus.Plugins/` and `%UserProfile%/AdrPlus.History/` as folders `dotnet tool uninstall -g adrplus` does not clean up.

**Verify:**
- `PluginManagerTests`/`PluginManagerDispatchTests`/`PluginManagerRetryTests`/`PluginManagerBackfillTests`/`PluginManagerDisposalTests` updated to seed the new merged-roots discovery and the new pending-state-root parameter; all existing scenarios (dedup, timeout, retry backoff, backfill sweep) re-verified unchanged in *behavior*, only in *where paths point*.
- New test: two repos with independently failing pending entries for plugins of the same name do not cross-contaminate (`plugins-state/` isolation) — the regression D36 exists to prevent.
- `InitCommandHandlerBuiltinPluginsTests.cs` no longer asserts a file copy into the repo; asserts the baseline is still seeded from merged host-global discovery.
- `PluginsCommandHandlerInstallTests.cs` updated: install/uninstall tests drop the repo-path setup entirely; the round-trip uninstall test's `activeplugins`-removed assertion is deleted (Phase 13's note above) and replaced with an assertion that `activeplugins` is untouched.
- End-to-end smoke test repeated (as Phase 12/13 originally did): build the real CLI, `--install` a zip, confirm it lands under a test-overridden `userPluginsRoot`, `init` a fresh repo and confirm the baseline includes it, `--uninstall` and confirm the repo's next command warns `Missing` rather than silently losing the entry.

---

## Phase 15 — Test-harness factories for plugin authors (D37)

- New namespace `AdrPlus.Abstractions.Testing` (`src/AdrPlus.Abstractions/Testing/`): `AdrRecordSnapshotFactory`, `RepoInfoSnapshotFactory`, `AdrEventContextFactory`, each a static `Create(...)` method with a default for every parameter, letting a plugin author's test construct a fully valid `AdrEventContext` (nested `AdrRecordSnapshot`/`RepoInfoSnapshot` included) in one call.
- Kept out of the production DTO types themselves (no factory methods added to `AdrEventContext`/`AdrRecordSnapshot`/`RepoInfoSnapshot`) — a separate namespace makes clear this is test-construction sugar, not part of the runtime contract the host resolves.
- No new `PackageReference` — `AdrPlus.Abstractions` had none before this (only `DefaultDocumentation`, and only under the `ReleaseDoc` configuration, never shipped in the package) and still has none; the factories are plain object construction.
- Ships in the same package/version as the rest of `AdrPlus.Abstractions` — no separate `.Testing` package, unlike the rejected `dotnet new` template (§ Out of scope, above) which genuinely needed its own distribution shape.

**Verify:** `tests/AdrPlus.Abstractions.Tests/Testing/` — one test file per factory (`AdrRecordSnapshotFactoryTests`, `RepoInfoSnapshotFactoryTests`, `AdrEventContextFactoryTests`), asserting the no-argument defaults are valid and that individual overrides are honored. `AdrPlus.Abstractions.Tests` suite: 13 tests (was 5).

---

## Suggested sequencing

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6
                                   ↘ Phase 7 (needs Phase 3)
Phase 8 (cross-cutting, start once Phase 3/4 land)
Phase 9 (needs Phase 3/4)
Phase 11 (validates everything — do last, but stub early to unblock testing Phase 4 onward)
Phase 10 (last — documents the finished behavior)
Phase 12 (needs Phase 3/7 — reuses loaded-plugin listing and manifest validation)
                                   ↘ Phase 13 (needs Phase 12 — install/uninstall reuse its activate/deactivate helper)
Phase 14 (needs Phase 3/5/6/11/12/13 — retargets discovery, pending-state location, and install/uninstall on top of all of them; do last)
```

---

## Definition of Done for v1

- Every decision tagged **Essential** in the spec's §3 is implemented and covered by a test.
- Decisions tagged **Deferred (v1.1+)** are **not** implemented — tracked as backlog, not silently dropped.
- The only changes to the 8 existing command handlers are the single `DispatchAsync` call each (Phase 4), plus (post-Phase 14) passing the repo-scoped pending-state root alongside it — no other modifications.
- Regression: with nothing installed under either host-global root (`plugins-builtin/` empty, `%UserProfile%/AdrPlus.Plugins/` absent — pre-Phase-14: an empty `./plugins` folder), all existing command behavior is byte-for-byte unchanged (dispatch is a no-op).

---

## Out of scope for this plan (do not build)

- D23 — dedup engine (`adrKey + eventType` before dispatch). **Rejected 2026-08-03**, not deferred — see spec §3.
- D24 — aggregate dispatch timeout across plugins. **Rejected 2026-08-03**, not deferred — see spec §3.
- D18 — `maxConcurrency` no longer exists as a concept to defer: the manifest field was removed entirely 2026-08-03 (it was never read by the host). Per-plugin concurrency stays fixed at 1.
- `dotnet new adrplus-plugin` project template — **rejected outright 2026-08-03**, not deferred: it would require a fourth separate distribution artifact (own package id, `.template.config`, release pipeline), unjustified with zero external plugin authors today. See spec §14. (The zip-based `adrplus plugins --install`/`--uninstall` distribution flow originally listed alongside this in an earlier version of this section shipped as D35/Phase 13, then D36/Phase 14 — no longer out of scope. The test-harness half of the original "Plugin SDK" bullet also shipped — see Phase 15/D37 below, not out of scope either.)

---

## Risk — confirmed 2026-07-31

`AdrPlus.Abstractions` targeting `net10.0` only is a one-way door for the contract itself (not for plugin authors — see spec §4.1): if it ever needs a future-TFM-only BCL type internally, the floor must move, breaking existing plugins. Low probability given the project is only interfaces and immutable records. **Superseded decision, also confirmed 2026-07-31:** the host (`src/AdrPlus/AdrPlus.csproj`, `tests/AdrPlus.Tests/AdrPlus.Tests.csproj`) dropped its `net10.0;net9.0;net8.0` multi-target down to `net10.0` only — .NET 9 (STS) is already past EOL and .NET 8 (LTS) reaches EOL November 2026. `AdrPlus.Abstractions` matches at `net10.0` only; there is no lower-TFM host build left to stay compatible with.
