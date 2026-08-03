# AdrPlus — Plugin Architecture Specification (Final)

> **Purpose**: Technical specification for adding a plugin system to the AdrPlus CLI.
> **Audience**: An LLM (Claude) that will implement or reason about this feature.
> **Status**: Implemented (v1). All decisions tagged Essential are built and tested; D23/D24 are rejected outright (not deferred); D29's CI-authoritative alternative is likewise rejected outright, not deferred — local/per-developer execution is the permanent model; D31–D33 (§3) are post-v1 refinements shipped after the original 11 implementation phases — see `adrplus-plugin-implementation-plan.md`'s addendum. **D34–D35 (2026-08-03) were implemented once against a per-repo `./plugins/<name>/` model, committed as a checkpoint (`fff9de7`), then D2/D29/D31/D35 were revised the same day (D36) to pivot discovery to a host-global store — see D36 for the pivot and D35's row for the post-pivot behavior it now describes.** D36 (Phase 14) is now also implemented and tested, committed as `21d443a`.
> **Guiding constraint**: **Minimal, surgical impact on the main product.** The core emits events; everything else lives in a new isolated extensibility layer.

---

## 1. Context

### 1.1 What AdrPlus is
AdrPlus is a .NET CLI tool that manages **Architecture Decision Records (ADRs)** — Markdown files describing architectural decisions. It creates, versions, revises, and changes the status of ADR files following configurable naming conventions and templates.

- **Solution**: `C:\Sources\AdrPlus\AdrPlus.slnx`
- **Main project**: `src\AdrPlus\AdrPlus.csproj`
- **Tests**: `tests\AdrPlus.Tests\AdrPlus.Tests.csproj`
- **Target framework**: .NET 10 only (dropped .NET 8/.NET 9 — see §4.1 note)
- **Repo**: https://github.com/FRACerqueira/AdrPlus

### 1.2 Feature goal
Allow external tools (e.g., **Confluence**, Jira, Teams) to **react to ADR lifecycle events** so ADRs can be summarized/synced elsewhere, **without changing the core** for each new integration. Motivating case: when an ADR is created/updated, a plugin copies/summarizes it to Confluence in a structured, synchronized way.

---

## 2. Current architecture (relevant facts)

Existing traits that make the plugin system feasible with minimal core change:

| Element | Location | Relevance |
|---|---|---|
| Dependency Injection | `src\AdrPlus\Extensions\ServiceCollectionExtensions.cs` (`AddAdrPlusServices`) | Plugin manager registers here |
| `IConfiguration` injected | `src\AdrPlus\Commands\CommandRouter.cs`, `src\AdrPlus\adrplus.json` | Source for allowlist / global plugin settings |
| Decoupled handlers | `ICommandHandler` (`src\AdrPlus\Commands\ICommandHandler.cs`) + `CommandRouter` | Central place to emit post-command events |
| Command registry | `src\AdrPlus\Commands\CommandsAdr.cs` (enum + `[Command(...)]`) | New `sync` / `plugins` commands added here |
| Domain model | `src\AdrPlus\Domain\AdrRecord.cs`, `AdrPlusRepoConfig.cs`, `AdrStatus.cs` | Source for immutable event snapshots |
| Async status ops | `src\AdrPlus\Core\AdrService.cs` / `IAdrServices` | Natural hook points for status events |
| Logging + localization | `src\AdrPlus\Infrastructure\Logging\LogMessages.cs`, `Resources\*.resx` (10+ languages) | File logging + localized host messages |

**Limitations to overcome:** everything is `internal` (no public contract); commands are static (no dynamic loading); no lifecycle-event mechanism.

**Conclusion:** this is an **added extensibility layer**, not a rewrite.

---

## 3. Approved design decisions (authoritative)

| # | Decision | Value | Tier |
|---|---|---|---|
| D1 | Failure behavior | **Fail-soft**: warn + log to file; never abort the local operation | Essential |
| D2 | Discovery (revised 2026-08-03 — see D36) | Folder-based, but **host-global, not per-repo**: the host merges two roots — `plugins-builtin/` (ships with the AdrPlus install, D33) and `%UserProfile%/AdrPlus.Plugins/<name>/` (user-installed via D35's `--install`) — each plugin still in its own subfolder with its own artifacts. There is no more `./plugins/<name>/` inside a repository; a repo only ever holds `activeplugins`/`disableplugins` (D31) plus its own `plugins-state/<name>/` (D36). One installed version per name on a given host — reinstalling a name always requires `--force` (D35); multi-version-per-host was considered and explicitly rejected 2026-08-03 (see D36's rationale) as disproportionate to the one real plugin that exists today. | Essential |
| D3 | Multiplicity | Multiple independent plugins; fan-out of the same event to all | Essential |
| D4 | Autonomy | Plugins receive events and decide whether to act | Essential |
| D5 | Reconciliation | Failures produce a reconcilable **pending state (per-plugin)**, not just a log line — without this, D1's fail-soft promise is just silent data loss against a flaky network dependency | Essential |
| D6 | Contract sharing | The host depends **only** on the public interface `IAdrPlugin` in `AdrPlus.Abstractions`, resolved by the host, versioned via SemVer | Essential |
| D7 | AOT | Not used → dynamic assembly loading allowed | Essential (context) |
| D8 | `migrate` event | `migrate` is not only infra; it emits an event | Essential |
| D9 | Pending state location | Per-plugin (each plugin owns its state) | Essential |
| D10 | Secrets/config | 100% the plugin's responsibility; host provides no secrets API | Essential |
| D11 | Hook result | Structured `PluginResult` (`Success`/`Skipped`/`Failed`, extended by D30's `IsRetryable`) | Essential |
| D12 | Discovery metadata | Manifest file (`plugin.json`) per subfolder | Essential |
| D13 | Contract versioning | Plugin declares `abstractionsVersion`; host validates **SemVer major**; incompatible → ignore with warning | Essential |
| D14 | Replay/backfill | `adrplus sync` has two modes (§6): **default** = re-drive `pending.json` only (self-limiting, safe to automate via cron/CI); **`--backfill`** = full repo sweep, re-emitting each existing ADR's current settled-status event with `IsReplay=true` (not self-limiting, not cron-safe, deliberate/manual). Both are load-bearing: `--backfill` is the *only* path for a plugin installed on a pre-existing repo (the realistic adoption case) to ever receive historical ADRs. | Essential |
| D15 | Retry policy | Host-managed retry: `maxAttempts`, base delay, backoff `Fixed`/`Exponential`, optional `jitter`. **Scoped to background re-drive only** (D27) — the foreground path is a single non-retried attempt; pending state is written on that attempt's failure, not after `retryPolicy`'s attempts are exhausted. | Essential |
| D16 | Mandatory contract | Every plugin **MUST** implement the single interface `IAdrPlugin`. Host validates the manifest `entryType` implements it and `Name`/`Version` match the manifest; otherwise not loaded + warning | Essential |
| D17 | Optional base class | `AdrPluginBase` is an **optional** convenience implementing `IAdrPlugin` (result helpers including D30's `Fail(message)`/`Fail(message, isRetryable:false)`, exception→`Failed` shielding, manifest validation). Host never depends on it | Essential |
| D18 | Instance lifetime & concurrency | One singleton instance per plugin, reused across events. `OnAdrEventAsync` must be reentrant; host caps concurrency per plugin at a fixed 1 — this serializes dispatch to a plugin across the many ADRs a `--backfill` sweep touches, so it doesn't burst-hammer the external system. Not manifest-configurable: `maxConcurrency` was removed from `plugin.json` entirely (confirmed 2026-08-03 — it was never read by the host, only deserialized) rather than kept as an unenforced field. | Essential |
| D19 | Disposal & unload | `IAdrPlugin : IAsyncDisposable`; host disposes plugins and flushes pending state on graceful shutdown. Explicit `AssemblyLoadContext` unload is a "good citizen" step, not a correctness requirement, for a short-lived CLI process that's about to exit and reclaim everything anyway (§4.2). | Essential |
| D20 | Host-provided services | Host injects an `ILogger`-backed `IPluginLogger` (with `CorrelationId`) via `IPluginContext`. Still no secrets. | Essential |
| D21 | Event schema forward-compat | Unknown `AdrEventType` values MUST be treated as `Skipped`; a plugin must never throw on an unknown event | Essential |
| D22 | Name collision | Two subfolders with the same `name` → both rejected + warning (avoids duplicate sync) | Essential |
| D23 | Replay dedup | Host deduplicates by `adrKey + eventType` before dispatch — relevant mainly within/across repeated `--backfill` runs, since the default re-drive mode is already self-limiting and normal lifecycle events can't recur on the same `adrKey+eventType` (AdrPlus's own status guards, e.g. `ApproveCommandHandler.SelectionCondition`, already prevent that). Correctness doesn't depend on it — the plugin's own idempotent upsert (§7/D11) already covers that; dedup only saves redundant API calls on a `--backfill` re-run. **Rejected 2026-08-03**, not deferred: with a single real plugin (AdrIndexer, a cheap local file rewrite) there's no evidence this is worth the added complexity; revisit only if a future plugin's `--backfill` re-run cost actually motivates it. | Rejected |
| D24 | Global dispatch timeout | An aggregate dispatch timeout, bounding total wait when several plugins run in parallel, is largely redundant once `foregroundTimeoutMs` (D27) is enforced via forced task abandonment (`Task.WhenAny` against `Task.Delay`) — parallel dispatch's total wait is `max()`, not `sum()`, of per-plugin timeouts regardless of plugin count. Adds value only against pathological slowness in dispatch orchestration itself, a low-probability case for a handful of folder-based plugins. **Rejected 2026-08-03**, not deferred: the per-plugin timeout already gives the practical guarantee that matters (bounded command latency regardless of plugin count); nothing about the current design motivates adding this. | Rejected |
| D25 | Diagnostics commands | `adrplus plugins list` / `adrplus plugins validate` report loaded plugins, versions, SemVer compatibility, manifest errors, allowlist status, and a pending-item count per plugin (§8) | Essential |
| D26 | `adrKey` / `ExternalKey` scope | `adrKey` (e.g. `"0007-v1-r0"`) identifies a specific **version+revision file** and changes on every `Revised`/`Versioned` event — it is **not** a stable identity for an ADR's lifetime. The host's dedup (D23) and pending-state (D5/D9) keys use this scoped `adrKey` on purpose (they track one file's delivery, not the decision's history). If a plugin wants **one external artifact that persists across revisions** (e.g. a single Confluence page), it must derive its own external identity from the ADR's stable sequence number (`Adr.Number` in `AdrRecordSnapshot`, mirroring `AdrRecord.Number`), not from `adrKey`. The host makes no cross-revision continuity guarantee — see §6 and §7 for why this matters. | Essential |
| D27 | Foreground/background dispatch split | The CLI command makes exactly **one bounded, non-retried attempt per plugin** inline, in the foreground, bounded by a new `foregroundTimeoutMs` (default `5000`). If that single attempt doesn't return `Success`/`Skipped` within budget, the host **immediately** writes pending state (§7) and returns control to the user. The full `retryPolicy` (§4.4: `maxAttempts`, exponential backoff, jitter) runs **only** during background re-drive of pending items (§6/§7) — never in the foreground. This bounds interactive CLI latency to ~`foregroundTimeoutMs` per plugin (plus D24's aggregate cap across plugins) regardless of how unhealthy the external system is, instead of the ~90s worst case (3 × 30s timeout + backoff) the original single-schedule design allowed. | Essential |
| D28 | Exit code semantics | The process exit code reflects **only the local ADR operation** (file write / status change) — plugin dispatch outcomes (`Success`/`Skipped`/`Failed`/queued-pending) never change it, consistent with D1's fail-soft guarantee. Scripts/CI that need to know whether external sync succeeded must check `adrplus plugins list` (pending counts, D25) or the file log — not the exit code. | Essential |
| D29 | Deployment model (revised 2026-08-03 — see D36) | v1 targets **local, per-developer execution**: dispatch happens on whichever developer's machine ran the state-changing command (`approve`/`reject`/etc.), using whatever plugin credentials are configured on *that* machine (D10). Coverage of the external system (e.g. Confluence) is therefore only as consistent as credential provisioning **and plugin installation** across the team's machines — accepted as a v1 trade-off, not treated as a bug; D36 makes this slightly more pronounced than before (a plugin must be `--install`ed on each developer's host, not just present via `git clone`). A CI-authoritative model (a pipeline running `adrplus sync --backfill` on merge, with one centralized credential and one shared pending state) was considered and **rejected outright, not deferred**: the local, per-developer model is v1's permanent design, not a placeholder awaiting usage data. **The "commit plugin.json/DLLs to the repo" clause is retired by D36**: there is no more `./plugins/<name>/` inside a repository for the team to share via clone — plugin binaries are host-global (D2/D36), installed once per machine via D35's `--install`. What each repo still commits is only `activeplugins`/`disableplugins` (D31) in `adr-config.adrplus` — the on/off override, not the code. `<repo>/plugins-state/<name>/pending.json` (D36, was `<repo>/plugins/<name>/state/pending.json`) remains local, per-machine, per-developer runtime state (should be gitignored) — nobody but that developer can see or re-drive their own pending items. | Essential (accepted constraint) |
| D30 | Retryable vs. permanent failure | Not every `Failed` result is worth retrying: a missing/invalid credential will fail identically on every retry attempt, no matter the backoff. `PluginResult` gets an `IsRetryable` flag (default `true`, preserving current behavior for plugins that don't think about the distinction). `Failed` + `IsRetryable: false` — and `InitializeAsync` throwing, treated the same way — is **not** written to `pending.json` at all (there is nothing productive to retry automatically). Instead: one distinct, prominent warning + file log entry telling the developer to fix configuration, then run `adrplus sync --backfill` manually once fixed (reusing the essential backfill mechanism, D14, rather than inventing a second recovery path). `InitializeAsync` is called **lazily** — only the first time a plugin is about to receive an event it actually subscribes to in this process run, not unconditionally on every command — so a developer running e.g. `adrplus new` never sees a Confluence-credential warning for a plugin that isn't even subscribed to `Created`. **Protocol-agnostic by construction (follows from D6/D16):** the host never sees HTTP status codes, socket errors, or any transport detail — only `PluginResult`. Classifying "this specific failure is permanent" (a 401 over HTTP, an auth rejection over gRPC, a timeout on a custom UDP protocol, whatever a future non-Confluence plugin uses) is entirely the plugin's own job, same as credential resolution (D10). D30 therefore generalizes to any future plugin without the host or `AdrPlus.Abstractions` ever needing protocol-specific knowledge. | Essential |
| D31 | Active-plugin baseline & kill switch (added 2026-08-03; source of "loaded" revised same day — see D36) | `AdrPlusRepoConfig` gains `ActivePlugins` (names expected active, written by `adrplus init`, editable via `adrplus plugins --wizard`'s manage mode) and `DisablePlugins` (repo-wide kill switch, default `false`). Per-plugin dispatch state: `Active` (loaded + listed — dispatched), `Inactive` (loaded but deliberately unlisted — silently skipped, no warning; this is what unchecking in the manage wizard does), `Missing` (listed but not loaded — the real drift case, warns once per run, dispatch to everything else proceeds unaffected), `Disabled` (repo-wide, overrides all of the above). Implemented as an optional `Func<LoadedPlugin, bool> isActive` filter threaded through `DispatchAsync`/`RetryPendingAsync`/`BackfillAsync` — `LoadedPlugins` itself stays unfiltered so `adrplus plugins --list` can still report every state. Both fields are **required** in `adr-config.adrplus` (beta, breaking changes accepted) — no migration path for pre-existing repos' already-created config files; they adopt the fields by re-running `init` or editing the file by hand. **"Loaded" now means loaded from D36's merged host-global roots, not a per-repo `./plugins/` scan** — the Active/Inactive/Missing/Disabled state machine and the gate's logic are otherwise unchanged; only where `PluginManager.LoadPluginsAsync` looks moved. `init` re-run against an existing repo does not auto-resync `ActivePlugins` against whatever is now discovered — it only surfaces drift via this same `Missing` warning; syncing is the developer's explicit choice (re-run `init` deliberately, or hand-edit). | Essential |
| D32 | Active-plugin visibility on the CLI (final, 2026-08-03) | `PluginActivationGate.Resolve` is **side-effect-free** — it only returns the `isActive` predicate and the `Missing` names; it never prints. Printing is a separate call, `IConsoleWriter.PromptWarnMissingActivePlugins(missingNames)`, invoked by each of the 9 dispatching handlers **right before their own result message** (not right after `LoadPluginsAsync`) — a wizard flow's `PromptMovePosition` after its own confirm step can otherwise land the print on a screen position that gets visually overwritten, making it invisible even though it was technically written. The happy path (everything expected is loaded) prints **nothing** — an earlier "Plugins active: {0}" success line was tried and then dropped as noise repeated on every single command; full Active/Inactive/Missing/Disabled status for every plugin already lives in `adrplus plugins --list`/`--wizard`, so the inline check only needs to surface the one actionable case. Message: `"Missing plugin(s): {0} — check ./plugins or run 'adrplus plugins --wizard'"`. The manage-wizard's own confirmation was reworded to `"Repository active-plugin list updated: {0}"` (was `"Active plugins updated: {0}"`) to disambiguate from other uses of "active" across the CLI. | Essential |
| D33 | Install-level (builtin) plugin visibility in the top-level wizard (2026-08-03) | `adrplus wizard`'s main menu loop shows a line — `"Bundled with this AdrPlus install: {0}"` — listing every plugin found under the install's `plugins-builtin/` folder (read directly from each subfolder's `plugin.json`, display-only, no manifest validation/loading), on **every** menu screen, right after the version line. Deliberately a different concept from D31/D32: this is about what ships with the AdrPlus *installation* (independent of any repository), not a repo's `ActivePlugins` baseline — no repo is even chosen yet at the top-level menu. Named "Bundled with this AdrPlus install" (not "active"/"installed") specifically to avoid colliding with D32's vocabulary. | Essential |
| D34 | Non-interactive activation management (implemented 2026-08-03) | Adds `--activate <name>`/`--deactivate <name>` to `adrplus plugins`, doing incrementally (union/except over `ActivePlugins`) what the wizard's Manage mode already does as a full-set replace. Both share one extracted helper (read config → compute new set → `ActivePluginsWriter.WriteAsync` → report, reusing D32's "Repository active-plugin list updated: {0}" message) so wizard behavior is unchanged. Neither flag requires the named plugin to already be loaded — a typo surfaces later as `Missing` via D31/D32's existing warning; no new validation needed. One name per invocation, not a comma-separated list, for symmetry with D35's `--install`/`--uninstall <name>`. **Prerequisite for D35**: without this, `--install`/`--uninstall` would still need the interactive wizard just to flip activation, defeating their own scriptability. | Essential |
| D35 | Plugin distribution via zip: `--install`/`--uninstall` (implemented 2026-08-03 against per-repo `./plugins/<name>/`; **retargeted the same day to D36's host-global store — this row describes the post-pivot behavior**) | `--install <path-to-zip>` / `--uninstall <name>` on `adrplus plugins` are now **host-global operations — neither takes `--path`/a repo target at all**, since D36 moved the destination out of any repository. Zip must be named `<name>-<version>.zip`; contents are exactly what belongs in the destination folder (dll, resolved deps, `plugin.json`, loose assets) — no new packaging format. The filename's `<name>` becomes the destination folder name under `%UserProfile%/AdrPlus.Plugins/` (not the manifest `Name`, which may legitimately differ — see `AdrIndexer`/`adr-indexer`); filename `<name>`/`<version>` are cross-checked against the `plugin.json` inside the zip, failing fast on mismatch. Every zip entry is guarded against path traversal before extraction (Phase 3's `entryAssembly` guard). Local file path only — no URL/registry fetch (§14). **One version per name on a host (D2, D36):** destination-exists policy is unchanged in shape — refuses without `--force`, and `--force` overwrites everything unconditionally including `plugin.json` and any per-install state — but now "exists" means "this name has any version installed," since there's nowhere for a second version to coexist. `--uninstall <name>` deletes `%UserProfile%/AdrPlus.Plugins/<name>/` recursively. **Reversed from the pre-pivot design: `--uninstall` no longer touches any repo's `activeplugins`.** Under the old per-repo model uninstall had exactly one repo in scope, so deactivating there was unambiguous; under the host-global model a name can be referenced by `activeplugins` in any number of repos the host has no registry of, so there is nothing for `--uninstall` to safely edit. Drift falls back to D31/D32's existing `Missing` warning the next time any affected repo runs a dispatching command — consistent with the project's established "warn, don't auto-fix" philosophy, not a new mechanism. Still runs the equivalent of `--validate` against the freshly installed plugin (against the merged host-global root, no repo needed) and prints its result immediately. Still prints the zip's SHA256 for the allowlist `Hash` field. | Essential |
| D36 | Host-global plugin store pivot: rationale, location, and pending-state split (2026-08-03) | **Trigger**: the developer's own executable and plugins live host-globally, not per-repo — a repo's `./plugins/<name>/` binaries were redundant with what's already on the machine, and every developer who clones a repo with plugins committed (D29) re-downloads/rebuilds the same DLLs the host already needs for every other repo too. **Location**: user-installed plugins move to `%UserProfile%/AdrPlus.Plugins/<name>/` — a **new, dedicated** stable folder, not reused from the existing `%UserProfile%/AdrPlus.History/` (that folder holds `ConfigVersionManager`'s version-snapshot `.txt` files for a different purpose — config migration across `dotnet tool update` — and mixing live plugin binaries into it would conflate the two). Discovery (D2) merges this folder with `plugins-builtin/` (D33, unchanged — already host-global). **Multi-version was considered and rejected**: pinning a specific version per repo (`name@version`) was explored but would have required (a) revising D22's duplicate-name rejection to key on name+version instead of name, (b) a new SemVer-ordering resolution algorithm in `PluginActivationGate` to pick "the latest" for an unpinned bare-name reference — **no SemVer comparer exists anywhere in the codebase today**, only `abstractionsVersion`'s major-only compatibility check (D13) — and (c) still couldn't make "drain pending retries before uninstalling a version" a safe automated check, since the host has no registry of which other repos on the machine might still reference that version. With exactly one real plugin (AdrIndexer, single version) in existence, this cost was judged disproportionate to a hypothetical need; **one installed version per name per host** is the permanent v1 design, not a placeholder — revisit only if a real multi-version need materializes. **The correctness-critical consequence, independent of the version question**: `pending.json` (§7, D5/D9) **must not move with the plugin binaries**. It was always read from `plugin.FolderPath` (`PendingStateStore.ReadAllAsync(_fileSystem, plugin.FolderPath, ...)`); if that folder becomes host-global and shared, one `pending.json` would serve every repo on the machine, and `PendingEntry.AdrKey` (e.g. `"0007-v1-r0"`) carries no repo identifier — repo A's failed `Approve` and repo B's failed `Approve` would collide on an identical key, and `adrplus sync` in repo B would re-drive repo A's entry against repo B's files. **Resolution**: pending/dispatch state stays **per-repo**, relocated to a new sibling folder `<repo>/plugins-state/<name>/pending.json` (was `<repo>/plugins/<name>/state/pending.json`) — local, per-machine, per-developer runtime state exactly as D29 already specifies for the old location, still recommended for `.gitignore`. `PluginManager`'s dispatch-family methods (`DispatchAsync`/`RetryPendingAsync`/`BackfillAsync`/the pending-write path in `HandleFailureAsync`) take this repo-scoped root as an explicit parameter instead of deriving it from `LoadedPlugin.FolderPath`, which now points at shared, host-global, read-mostly plugin code. **Testability seam**: the host-global user-store root and the merged discovery follow the same pattern already established for `plugins-builtin/` (`InitCommandHandler`'s `builtinPluginsRoot` constructor parameter, default `""`, wired via a factory in `ServiceCollectionExtensions` using `Path.Combine(AppContext.BaseDirectory, "plugins-builtin")` for the real run) — a `userPluginsRoot`-style parameter, default `""`/overridable, so tests never touch the real `%UserProfile%`. **`init` on an existing repo**: does not auto-resync `activeplugins` against the merged host-global store on re-run — only warns on drift, via the existing D31/D32 `Missing` mechanism, consistent with the project's standing "warn, don't auto-fix" rule; a fresh repo's baseline is still seeded once from whatever is discovered at `init` time. **Full-removal note**: since dotnet global tools have no uninstall hook, `dotnet tool uninstall -g adrplus` does not clean up `%UserProfile%/AdrPlus.Plugins/` (nor the pre-existing `%UserProfile%/AdrPlus.History/`) — both are orphaned deliberately, an accepted trade-off (matches the existing, already-accepted behavior of `AdrPlus.History`), not a bug. No automated cleanup command is planned; user-facing docs must state where to delete both folders manually for a complete removal. | Essential |

---

## 4. Solution components

### 4.1 New public project: `AdrPlus.Abstractions`
Contains **only interfaces and immutable DTOs**, **resolved by the host** (loaded once), never copied into plugin folders (otherwise types differ and casts fail).

**Target framework:** `net10.0` only — matching the host, which dropped its `net10.0;net9.0;net8.0` multi-target in favor of `net10.0` only (decision made 2026-07-31: .NET 9 (STS) is already past EOL, and .NET 8 (LTS) reaches EOL November 2026 — no reason to keep building/testing against runtimes the project no longer wants to support). With a single-target host, `Abstractions` has no "lowest common TFM" to float below — it simply matches. A plugin project can target `net10.0` or anything with `TargetFramework >= net10.0` and reference this build with zero extra configuration (standard TFM forward-compatibility); a plugin targeting below `net10.0` fails to reference it, which is desired.

Trade-off to accept knowingly (smaller now than the original net8.0-floor version of this decision, but not zero): single-targeting `net10.0` is still a one-way door for `Abstractions` **itself** — if the contract ever needs a future-TFM-only BCL type internally, the floor moves, breaking existing plugins built against the older floor. Given `Abstractions` is only interfaces and immutable records, this is unlikely to bite.

**Public surface:**
```
IAdrPlugin               // the single mandatory contract (D16)
IPluginContext           // host-provided services (logger, correlation id) (D20)
IPluginConfiguration     // typed access to the plugin's own manifest "settings"
AdrEventContext          // immutable event DTO
AdrEventType             // enum
PluginResult             // return DTO
PluginResultStatus       // enum: Success | Skipped | Failed
AdrRecordSnapshot        // public immutable copy of internal AdrRecord
RepoInfoSnapshot         // public immutable copy of relevant AdrPlusRepoConfig data
AdrPluginBase            // OPTIONAL convenience (D17)
```

**Mandatory contract (D16, D18, D19, D20, D21):**
```csharp
public interface IAdrPlugin : IAsyncDisposable
{
	// Identity — must match plugin.json (host validates on load)
	string Name { get; }
	string Version { get; }

	// Lifecycle — receives host services (logger/correlation) and typed settings
	Task InitializeAsync(IPluginContext context, IPluginConfiguration config, CancellationToken ct);

	// Cheap declarative filter — host may skip invocation entirely
	bool ShouldHandle(AdrEventContext context);

	// Reaction — MUST return PluginResult; MUST treat unknown events as Skipped; MUST NOT throw for control flow
	Task<PluginResult> OnAdrEventAsync(AdrEventContext context, CancellationToken ct);
}

public sealed record AdrEventContext
{
	public required AdrEventType EventType { get; init; }
	public required bool IsReplay { get; init; }
	public required AdrRecordSnapshot Adr { get; init; }
	public required string AdrFilePath { get; init; }
	public required Func<string> GetAdrRenderedContent { get; init; }   // lazy — see note below
	public required RepoInfoSnapshot Repo { get; init; }
	public required string CorrelationId { get; init; }
}

public sealed record PluginResult
{
	public required PluginResultStatus Status { get; init; }
	public string? Message { get; init; }
	public string? ExternalKey { get; init; }   // e.g., Confluence pageId (idempotency)
	public bool IsRetryable { get; init; } = true;   // D30 — false for permanent/config failures (e.g. bad credentials)
}

public enum PluginResultStatus { Success, Skipped, Failed }
```

**Note on `GetAdrRenderedContent` (fixes a self-contradiction found in review):** the field was originally `AdrRenderedContent` (a plain `required string`), which forces the host to render/materialize content **before** `subscribedEvents`/`ShouldHandle` get to filter the event — defeating §4.3's own claim that those filters let the host "skip un-subscribed events cheaply." Making it a lazy delegate means rendering only happens if a plugin's filter actually decides to handle the event. **Trade-off:** `AdrEventContext` is a `record`, and a bare `Func<string>`/`Lazy<string>` member breaks the record's structural (value) equality — two events with identical data but different delegate instances would compare unequal. Not a problem today (nothing compares `AdrEventContext` instances), but worth flagging since D23's dedup works on `adrKey + eventType`, not on the event object itself, so it's unaffected — just don't assume `AdrEventContext` supports value equality later without revisiting this.

**Optional convenience base (D17):** `AdrPluginBase : IAdrPlugin` centralizes `try/catch`→`Failed`, exposes `Success()`/`Skip()`/`Fail(message)` (retryable by default, D30) and `Fail(message, isRetryable: false)` (permanent — e.g. bad credentials) helpers, validates the manifest, and turns `OnAdrEventAsync` into a template method calling `ShouldHandle` + an abstract `HandleAsync`. Authors may ignore it and implement `IAdrPlugin` directly.

### 4.2 Host components (internal to AdrPlus)
- **`IPluginManager`** — orchestrates discovery, load, validate, dispatch, retry, reconciliation, and shutdown.
- **`PluginLoader`** — isolated loading and unloading.

**Loading (D2, D6, D13, D16, D22):**
- One isolated **`AssemblyLoadContext` (ALC)** per subfolder; private deps resolved via `AssemblyDependencyResolver` over the plugin's `.deps.json`.
- `AdrPlus.Abstractions` resolved by the **host** only.
- On load, validate: manifest schema, `entryType` implements `IAdrPlugin`, `Name`/`Version` match manifest, `abstractionsVersion` SemVer-major compatible, allowlist, and **duplicate name** (both rejected). Any failure → skip + localized warning. This is structural/cheap validation — no plugin code runs yet.
- **`InitializeAsync` is deferred (D30), not part of load validation above.** It only runs the first time, in this process, that a subscribed event is about to be dispatched to that plugin — a developer running a command the plugin doesn't subscribe to (e.g. `adrplus new` against a plugin only subscribed to `Approved`) never triggers it, and never sees a spurious credential warning for a system they aren't touching right now.

**Dispatch (D1, D3, D18, D23, D24, D27):**
- Deduplicate by `adrKey + eventType` (D23), filter by `subscribedEvents` + `ShouldHandle`, then invoke subscribed hooks **in parallel**, isolated per plugin (`try/catch`).
- Per-plugin **`foregroundTimeoutMs`** (D27, default 5000ms), **enforced by the host** (race the hook's `Task` against `Task.Delay(foregroundTimeoutMs)` via `Task.WhenAny` and abandon it on timeout) — not merely passed as a `CancellationToken` and trusted to be honored. This matters: it's what lets a single plugin's timeout bound the command's wait even if that plugin's own code ignores cancellation (a buggy/misbehaving plugin), without needing a separate aggregate timeout to catch that case. Per-plugin **maxConcurrency** (default 1, fixed — user-tunability is Deferred v1.1+, D18).
- **Foreground attempt is single-shot, no retry** (D27): if a plugin doesn't return `Success`/`Skipped` within `foregroundTimeoutMs`, the host writes pending state (§7) immediately — attempt count `0`/`1` recorded, not "exhausted" — and moves on. The command returns to the user bounded by `foregroundTimeoutMs` × plugin count (or D24's aggregate cap), not by `retryPolicy`'s full schedule.
- **Background re-drive runs the full `retryPolicy`** (§4.4: `maxAttempts`, exponential backoff, jitter) against pending items, outside the interactive command — see §6/§7. This is where `timeoutMs` (per-attempt, can afford to be generous, e.g. `30000`) and the backoff schedule apply.
- **Permanent failures never reach `pending.json` at all (D30):** a `Failed` result with `IsRetryable: false`, or `InitializeAsync` throwing, gets a distinct, prominent warning (not the routine "queued for retry" message) and the file log entry — but no pending-state record and no automatic re-drive, since retrying a bad credential changes nothing. Recovery is manual: fix the configuration, then run `adrplus sync --backfill` to catch up.

**Shutdown (D19):** on graceful exit, cancel in-flight work within the aggregate timeout, `DisposeAsync` each plugin, and flush pending state. `DisposeAsync` matters regardless of process lifetime (it lets a plugin flush an `HttpClient`, close a file handle, etc.). Explicit ALC unload matters less here than it would in a long-running host: an `adrplus` invocation is a short-lived CLI process that is about to exit and reclaim everything anyway — unload is a "be a good citizen" step (frees memory a fraction of a second early), not a correctness requirement the way it would be for a host that hot-swaps plugins across many invocations.

### 4.3 Manifest — `plugin.json` (per subfolder)
```json
{
  "name": "confluence",
  "version": "1.0.0",
  "entryAssembly": "AdrPlus.Plugin.Confluence.dll",
  "entryType": "AdrPlus.Plugin.Confluence.ConfluencePlugin",
  "abstractionsVersion": "1.0.0",
  "subscribedEvents": [ "Approved", "Rejected", "Superseded", "StatusUndone", "Migrated" ],
  "foregroundTimeoutMs": 5000,
  "backgroundTimeoutMs": 30000,
  "retryPolicy": {
	"maxAttempts": 3,
	"delayMs": 2000,
	"backoff": "Exponential",
	"jitter": true
  },
  "settings": { "baseUrl": "https://...", "spaceKey": "ARCH" }
}
```
- **`foregroundTimeoutMs` (new, D27):** bounds the single, non-retried attempt made inline while the CLI command is running. Keep this short (default `5000`) — it directly adds to how long a user waits for `adrplus approve`/etc. to return.
- **`backgroundTimeoutMs`/`retryPolicy` now scope to background re-drive only** (D27): these govern attempts made outside the interactive command (via the pending-item re-drive mechanism, §6/§7), so a generous per-attempt timeout and a multi-step backoff schedule are appropriate there in a way they are not for the foreground path. Named `backgroundTimeoutMs` (not the original `timeoutMs`, renamed 2026-08-03) specifically so its scope is unambiguous next to `foregroundTimeoutMs`.
- `subscribedEvents` lets the host skip un-subscribed events cheaply (complements `ShouldHandle`).
- **Secrets are NOT stored here** (D10) — the plugin resolves credentials itself (env vars, own vault).
- **Why the example above excludes `Created`/`Revised`/`Versioned`** (fixes an issue found in review): `adrplus new`/`revise`/`version` only ever capture metadata (title, domain, scope, date) — never the decision body. `revise` can even start from `--empty`. The `.md` content at those events is template scaffolding or a blank draft, not a finished decision; the user edits it by hand afterward, outside AdrPlus, before running `approve`/`reject`. A plugin that syncs "the decision" should trigger on `Approved`/`Rejected`/`Superseded`/`StatusUndone`/`Migrated` — the events where `Adr.StatusUpdate`/`Adr.StatusChange` (already present in `AdrRecordSnapshot`) indicate settled content. Subscribing to `Revised`/`Versioned` is actively dangerous, not just noisy: per D26, if a plugin upserts by an `ExternalKey` tied to the ADR's stable identity, a `Revised`/`Versioned` event fires *after* the prior content was already synced as final — upserting on it would overwrite a published, approved decision with a blank draft. A plugin author who does want a "draft created" signal can still subscribe to `Created`/`Revised`/`Versioned` deliberately, but should write to a *different* external identity (e.g. a separate "drafts" key) than the one used for approved content, precisely because the host gives no continuity guarantee between them (D26).

### 4.4 Retry policy (D15, D27)
Host-managed so behavior is uniform and feeds the pending `attempts` counter. **Applies only to background re-drive of pending items — not to the single foreground attempt** (D27), which has no retry and is bounded solely by `foregroundTimeoutMs`.

| Field | Type | Meaning | Default |
|---|---|---|---|
| `maxAttempts` | int | Total attempts before `Failed` + pending state | `3` |
| `delayMs` | int | Base delay between attempts (ms) | `2000` |
| `backoff` | enum | `Fixed` or `Exponential` | `Exponential` |
| `jitter` | bool | Randomize delay to avoid retry storms | `true` |

Delay for attempt `n` (1-based): `Fixed` → `delayMs`; `Exponential` → `delayMs * 2^(n-1)` (2s, 4s, 8s…). With `jitter`: `random(0, delay(n))`. `Skipped` is never retried; `CancellationToken`/timeout honored between attempts.

> **Exponential** (not "logarithmic") is used because it progressively relieves a stressed remote (HTTP 429/503); logarithmic growth would be the opposite of desired.

---

## 5. Lifecycle events (`AdrEventType`)

Emitted from existing handlers/service methods:

| Event | Core origin |
|---|---|
| `Created` | `NewAdrCommandHandler` (after file write) |
| `Versioned` | `VersionCommandHandler` |
| `Revised` | `ReviseCommandHandler` |
| `Superseded` | `SupersedeCommandHandler` (calls `IAdrServices.StatusChangeSupersedeAdrAsync`) |
| `Approved` | `ApproveCommandHandler` |
| `Rejected` | `RejectCommandHandler` |
| `StatusUndone` | `UndoStatusCommandHandler` |
| `Migrated` | `MigrateCommandHandler` |

**Do NOT emit:** `help`, `wizard`, `config`, `explore`, and the app `version` command.
**Forward-compat (D21):** new event types may be added later; plugins must treat unknown values as `Skipped`.

**Content readiness varies by event (see §4.3 for the full rationale and D26):** `Created`/`Revised`/`Versioned` fire on metadata-only scaffolding — the decision body is not yet authored at that point. `Approved`/`Rejected`/`StatusUndone`/`Superseded`/`Migrated` fire on files whose content is settled. Plugins that sync "the decision" (not just "a file changed") should subscribe accordingly.

---

## 6. Replay / Backfill (D14, D23)

`adrplus sync` has **two distinct behaviors** (revised in this discussion — the original single-mode design didn't separate them, which made the two safe only in isolation, not together):

- **`adrplus sync` (default): re-drive pending items only.** Reprocesses whatever is already recorded in this repo's `./plugins-state/<name>/pending.json` (§7, D36 — was `./plugins/<name>/state/pending.json` pre-pivot), running the full `retryPolicy`. This is **self-limiting** — a successful item leaves `pending.json`, so repeated runs converge to a no-op — which is what makes it **safe to automate via cron/CI** (§ operational note below).
- **`adrplus sync --backfill` (explicit opt-in): full repo sweep.** For each *existing* ADR, re-emits the event matching its **current settled status** (`Approved`/`Rejected`/`Superseded`, or `Migrated` for migrated files) with `IsReplay = true` — this is how a plugin installed on a repo that already has ADRs gets them for the first time (there is no pending entry for something that was never dispatched, so the default mode above can never reach it). An ADR still in `Proposed` has nothing settled to replay; skipped.
  - **This is essential, not deferrable** (D14): a plugin installed on a pre-existing ADR repo — the realistic adoption case, not the exception — has no other way to receive historical ADRs. Deferring it would mean v1 only works for repos that install the plugin on day one.
  - **This is NOT self-limiting** — it re-dispatches *every* settled ADR every time it runs, relying on the plugin's own idempotent upsert (§7) for correctness. **It must never be wired into a recurring/cron trigger** — it's a deliberate, occasional, human-invoked operation (first install, "I suspect something was missed"), documented as such. `--backfill` on every cron tick would re-upsert the entire ADR history forever.
  - **Retry behavior during `--backfill` (closes an ambiguity found in final review):** each event uses the full `retryPolicy` per item (not the fast single-shot D27 path) — the user explicitly chose to wait by running this command, unlike an interactive `approve`. A failure that exhausts `maxAttempts` during `--backfill` is **only logged, not written to `pending.json`**: since `--backfill` is itself idempotent and rediscovers every settled ADR on each run, "run `--backfill` again" is already the correct recovery path — writing hundreds of pending entries from one bad sweep (e.g. against a temporarily dead endpoint) would just make every subsequent cron-driven default `adrplus sync` grind through all of them with full backoff, for no benefit `--backfill` doesn't already provide on its own.
  - **`maxConcurrency` (D18, default 1, fixed) matters most here:** it serializes dispatch to a given plugin across the ADRs being swept, so a `--backfill` over hundreds of ADRs doesn't burst-hammer the external system with concurrent upserts all at once — the field is not dead weight in the manifest even though it isn't user-tunable yet (D18); it's doing real, load-bearing work at its fixed value.
- Host **deduplicates by `adrKey + eventType`** (D23) to avoid redundant re-dispatch **within a `--backfill` run** (or across repeated manual `--backfill` runs); plugins stay idempotent via `ExternalKey` regardless. Since `--backfill` is manual/rare rather than routine, dedup's value is "saves redundant API calls on an operation the user chose to run" rather than a correctness requirement — genuinely deferrable to v1.1/v2 (D23), unlike the sweep itself.
- **Operational note:** only the default (pending-only) mode belongs in an automated scheduler. `adrplus sync` (no flag) run periodically via cron/Task Scheduler is the recommended way to get eventual delivery without a daemon (§ below); `--backfill` is run by hand when onboarding a plugin or recovering from a known gap.
- **Re-drive is not autonomous (D27):** AdrPlus has no daemon or persistent background process — it's a short-lived CLI invocation like any other command. The `retryPolicy` schedule for a pending item only runs when something explicitly invokes `adrplus sync` again: a user, a script, or an external scheduler the deploying team sets up. AdrPlus does not manage that scheduler itself.

---

## 7. State / Reconciliation (per-plugin, per-repo — D5, D9, D15, D36)

- Location: `./plugins-state/<name>/pending.json` (was `./plugins/<name>/state/pending.json` before D36's host-global pivot). **Deliberately not colocated with the plugin's binaries anymore**: D36 moved plugin code to a host-global, shared-across-repos store, but pending state must stay per-repo — `PendingEntry.AdrKey` has no repo identifier, so a shared location would let one repo's pending item get re-driven against another repo's ADRs.
- Written by the host **after the single foreground attempt fails/times out** (D27) — not after `retryPolicy`'s `maxAttempts` is exhausted, since that schedule no longer runs in the foreground. `attempts` in the record below starts at whatever the foreground made (`0` or `1`) and increments as background re-drive runs `retryPolicy` against it. **Exception (D30):** a `Failed` result with `IsRetryable: false` (or an `InitializeAsync` failure) is never written here — see §4.2.
  ```json
  {
	"adrKey": "0007-v1-r0",
	"eventType": "Approved",
	"correlationId": "…",
	"lastError": "…",
	"attempts": 3,
	"timestamp": "2026-01-01T00:00:00Z"
  }
  ```
- Re-emitted on the next relevant event or via `adrplus sync`. **Idempotency** via `ExternalKey` (e.g., Confluence `pageId`) → upsert.
- **`adrKey` is not decision identity (D26):** it's scoped to one version+revision file. A plugin choosing what `ExternalKey` to upsert against is choosing whether it wants per-file artifacts (safe, but no continuity across revisions) or one artifact for the ADR's whole lifetime (continuity, but only safe if the plugin never upserts on pre-content events — see §4.3/§5).

---

## 8. Diagnostics & Observability (D20, D25)

- **`adrplus plugins list`** — loaded plugins, versions, subscribed events, allowlist status, **and a pending-item count per plugin** (read from this repo's `./plugins-state/<name>/pending.json`, D27/§7/D36) — the cheapest way for a user to answer "did my last sync actually go through?" without a new command, since the foreground path (D27) no longer blocks long enough to guarantee an answer before the command returns.
- **`adrplus plugins validate`** — manifest schema, `IAdrPlugin` implementation, SemVer-major compatibility, duplicate-name and allowlist checks; reports without emitting real events.
- **`IPluginLogger`** (host-provided via `IPluginContext`) unifies plugin logs with the host file log and `CorrelationId`. Host also records per-plugin timing and result counts.

---

## 9. Logging & Localization (D1)

- Failure/timeout/skip → **file log** with `CorrelationId`, plugin name, event, attempt count, message.
- Console shows a concise **warning** (fail-soft), never breaking the command — including when the foreground attempt (D27) times out and the item is queued to pending, so the user gets immediate feedback (e.g. "confluence: queued for retry") even though the command itself doesn't wait for the eventual outcome.
- **Permanent failures get a visibly different message** (D30) — e.g. "confluence: permanent failure, not queued for retry — fix configuration and run `adrplus sync --backfill`" — distinguishable at a glance from the routine "queued for retry" transient case, so a developer doesn't mistake a broken credential for a network blip that will self-heal.
- **Host messages** about plugins are localized (existing `.resx`). **Plugin messages** are the plugin's responsibility (consistent with D4/D10).

---

## 10. Security (D22)

- Optional **allowlist** in `adrplus.json` (names and/or assembly hashes); anything outside → not loaded + warning.
- Duplicate names rejected (D22).
- Documented risk: plugins run with the **user's permissions** (third-party code from `./plugins`).
- **`plugin.json`'s `settings` is plain JSON** — since D10 puts secrets entirely on the plugin, this is a process/documentation safeguard, not something the host enforces: plugin authors must not put real credential values in `settings`, only non-secret config (base URLs, space keys, etc.). Worth stating explicitly in plugin-author docs. (Pre-D36, this manifest could be repo-committed per D29's old clause; post-D36 it lives host-globally under `%UserProfile%/AdrPlus.Plugins/<name>/`, not in any repo, but the same "no secrets in `settings`" guidance still applies — a plugin author's own packaging/source repo for the plugin itself is a different concern than the AdrPlus repo using it.)

---

## 11. Impact on the core (minimal, surgical)

1. New project `AdrPlus.Abstractions` (isolated; core references it).
2. `IPluginManager` / `PluginLoader` (+ retry, dedup, timeouts, disposal) registered in `ServiceCollectionExtensions`.
3. A single `await _pluginManager.DispatchAsync(...)` at the end of each handler in §5 — the **only touch points in existing handlers**. (D36: this call now also passes the repo-scoped `plugins-state/` root, since pending state can no longer be derived from a loaded plugin's now-shared, host-global folder path.)
4. New `sync` and `plugins` commands (added to the command registry; no change to existing commands). **`sync`'s handler is not a one-liner** (fixed in this revision, see §6): it has two modes — default (re-drive `pending.json` only) and `--backfill` (read every existing ADR's current settled status and pick the matching event type to replay). Both live entirely in the new `SyncCommandHandler`, so neither touches existing handlers, but it's more than trivial glue and belongs in this impact list.
5. Public snapshots of `AdrRecord` / `AdrPlusRepoConfig`.
6. Graceful-shutdown hook to dispose/unload plugins.

> Existing command behavior is unchanged when no plugins are installed (empty `./plugins` → dispatch is a no-op).

---

## 12. End-to-end flow (Confluence example)

```
adrplus approve   (adrplus new also dispatches, but §4.3's recommended manifest doesn't subscribe to Created — see below)
  → Core writes/updates the .md  (already durable at this point)
  → Handler calls IPluginManager.DispatchAsync(event)
  → Dedup (adrKey+eventType) → filter (subscribedEvents + ShouldHandle)
  → Invoke subscribed hooks in parallel (per-plugin foregroundTimeoutMs, maxConcurrency, aggregate timeout — D27/D24)
  → [first time this run: InitializeAsync — if it throws (e.g. missing credentials), skip this plugin for the whole run,
     one distinct loud warning, no pending entry — see D30]
  → Per hook: ONE attempt, no retry (D27)
    ├─ Success/Skipped within foregroundTimeoutMs → PluginResult { Success, ExternalKey = pageId } → command returns
    └─ Failed/timed out → host writes pending.json (per-plugin) + warns + file log → command returns immediately (does NOT wait for retries)
  → [not part of this CLI invocation — requires a later, separately-triggered `adrplus sync` run, see §6]
    re-drive of pending.json runs the full retryPolicy (maxAttempts, Fixed/Exponential + jitter)
    until Success or a new failure is recorded
  → On CLI graceful exit: dispose plugins, flush pending, unload ALCs
```

---

## 13. Accepted trade-offs

- Synchronization is **eventual** (retry + idempotency + reconcilable pending state), not transactional.
- Higher memory from isolated ALC per plugin — accepted.
- Secrets delegated to plugins — heterogeneous config UX, simpler host.
- Singleton plugin instance requires reentrant hooks — mitigated by `maxConcurrency` default 1.
- **Interactive latency is bounded, but delivery is not automatic over time** (D27): a slow/unhealthy plugin adds at most `foregroundTimeoutMs` (+ D24's aggregate cap) to any command, not the full retry schedule — but a failed sync then sits in `pending.json` until something explicitly runs `adrplus sync` again (there is no background daemon). Teams relying on a plugin for anything time-sensitive need their own external trigger (cron/CI) for `sync`.
- **Exit code carries no plugin-outcome signal** (D28): scripts must inspect `adrplus plugins list`'s pending counts, not the exit code, to know whether sync succeeded.
- **Coverage depends on who ran the command and what's configured on their machine** (D29): v1 is local/per-developer, not CI-authoritative. An ADR approved by a developer without valid plugin credentials configured locally simply doesn't sync — loudly (D30), but it doesn't sync — until someone with working credentials runs `adrplus sync --backfill`. Accepted permanently — a CI-based model was considered and rejected outright (D29), not deferred.
- **Permanent (credential/config) failures fail fast instead of retrying forever** (D30): distinguishing this from transient failures avoids wasted retry cycles and silent "it'll fix itself" false hope on something that structurally can't self-heal.
- **Host-global plugin folders outlive `dotnet tool uninstall`** (D36): dotnet global tools have no uninstall hook, so `%UserProfile%/AdrPlus.Plugins/` (and the pre-existing `%UserProfile%/AdrPlus.History/`) are never cleaned up automatically when AdrPlus itself is removed — orphaned deliberately, matching the already-accepted behavior of `AdrPlus.History`. No automated cleanup command is planned; user docs must state both paths so a user wanting a complete removal knows what to delete by hand.
- **One installed version per plugin name per host** (D2/D36): a real constraint, not an oversight — accepted after multi-version support was evaluated and found to cost a D22 revision, a SemVer-ordering algorithm that doesn't exist yet, and an unresolvable "is anything still pending against the version I'm about to remove" check (the host has no registry of other repos on the machine). Revisit only if a genuine multi-version need materializes.

---

## 14. Roadmap (v2, out of scope for v1)

- Plugin SDK: `dotnet new adrplus-plugin` template + test harness (mock `AdrEventContext`).
- **D23 (dedup)** and **D24 (aggregate dispatch timeout)** — rejected outright 2026-08-03, not deferred (§3); revisit only if a future plugin's real-world usage motivates either. `maxConcurrency` is no longer tracked here at all — it was removed from `plugin.json` entirely (D18), not left as a future user-tunable knob. `adrplus sync --backfill` itself remains essential for v1, not deferred — see §6.

---

## 15. Glossary

- **ADR** — Architecture Decision Record.
- **ALC** — `AssemblyLoadContext`; isolated assembly loading/unloading.
- **Backoff** — delay-growth strategy between retries (`Fixed`/`Exponential`).
- **Fail-soft** — failure is logged/warned but never aborts the main operation.
- **Fan-out** — same event delivered to multiple independent consumers.
- **Idempotency** — repeating an operation yields the same end state (via `ExternalKey`).
- **Jitter** — randomization of retry delays to avoid synchronized storms.
- **Host** — the AdrPlus core process loading/dispatching to plugins.
- **Hook** — a plugin method invoked in reaction to a lifecycle event.
