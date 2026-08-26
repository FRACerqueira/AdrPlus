# Pre-release audit — ADR006 change + plugin system resilience

Ephemeral working notes. Not a standing project doc — delete this folder when the audit concludes.

## Round 1 — ADR006 (Scope/Domain removal) — DONE, committed

See `convergence.md` for round 1's cross-pass detail. Summary: 4 custom angles, all fixes applied,
committed as `f2f4213` + `9b3167f`. 1316/1316 + 13/13 tests green.

## Round 2 — Plugin system resilience (`auditoria-resiliencia`, 3 parallel independent instances)

Angle confirmed with user: resilience of the plugin dispatch/retry/dispose system against ADR002's
documented guarantees (fail-soft, bounded latency, eventual-not-transactional sync). 3 independent
instances of the `auditoria-resiliencia` pillar agent, per its own protocol (2-of-3 corroboration for
Medium-severity claims). All 3 completed. Findings below — **nothing fixed yet, pending user direction**.

### Strongly convergent (2-3 passes independently agree)

| # | Finding | Severity | Passes | Status |
|---|---|---|---|---|
| A | `pending.json` corruption/write failure breaks ADR002's fail-soft guarantee — propagates to command exit code 1 even though the ADR file was already saved successfully; also aborts the *entire* `RetryPendingAsync` loop for every other plugin, not just the affected one | Critical (pass 3) / High (pass 1) | 1, 3 | ✅ Fixed — `PendingStateStore.ReadAllAsync` treats invalid JSON as empty (warns, doesn't throw); `RetryPendingAsync`/`HandleFailureAsync` catch `IOException`/`UnauthorizedAccessException` around read+write and warn instead of propagating |
| B | Same-process plugin reload (reachable via the interactive wizard looping after a config change) reuses init bookkeeping by name across generations — a genuinely new plugin instance skips `InitializeAsync` and dispatches live; previous generation's instances/AssemblyLoadContext never disposed | High (pass 3) / Medium (pass 2, Part A empirical) | 2, 3 | ✅ Fixed — `_initializedPlugins`/`_initFailedPlugins` re-keyed by instance reference (not name) and cleared by a shared `DisposeCurrentGenerationAsync` run at the start of every `LoadPluginsAsync` |
| C | An abandoned hook task (timed out) races with `DisposeAsync` on the *same* plugin instance during shutdown/next dispatch — host-caused use-after-dispose risk for a plugin with real shared resources | Medium (pass 3, empirical) / Low (pass 1, code-read only) | 1, 3 | ✅ Mitigated — abandoned hooks tracked in `_outstandingHooks`; dispose waits one `ForegroundTimeoutMs` grace period for the outstanding hook before disposing (narrows, doesn't eliminate, the window); `IAdrPlugin`'s XML doc now documents the possible overlap for plugin authors |

### Disagreement — resolved by user decision (2026-08-26)

| Finding | Pass 1 + 3's position | Pass 2's position | Resolution |
|---|---|---|---|
| `RetryPendingAsync` loses already-succeeded progress within one plugin's entry loop when cancelled mid-loop (before the single end-of-loop `WriteAllAsync`) | **Bug** — contrast with `BackfillPluginAsync`'s explicit, tested protection against the identical failure mode (same file, same author) is evidence of oversight, not policy | **Not a new finding** — covered by ADR002's own stated "eventual, not transactional — idempotency expected" trade-off; no *cross-plugin* state is lost, only same-plugin same-run entries | **User sided with the 2-of-3 majority (bug) and asked to fix it.** ✅ Fixed — `RetryPendingAsync` now catches `OperationCanceledException` mid-loop, re-adds the unprocessed remainder (`entries.Skip(index)`) to what gets persisted, then rethrows — mirroring `BackfillPluginAsync`'s existing protection. |

### Single-pass, well-evidenced (not corroborated by count, but concrete)

| # | Finding | Severity | Pass | Status |
|---|---|---|---|---|
| D | `DisposeLoadedPluginsAsync` has **no timeout at all** — a slow (non-throwing) `DisposeAsync` hangs *every command*, not just shutdown, because it runs in `MainProgram`'s `finally` every time | High | 2 | ✅ Fixed — each plugin's `DisposeAsync` raced against `Task.Delay(ForegroundTimeoutMs)`; abandoned if it loses, with the fault observed via a continuation so it never surfaces as an unobserved task exception |
| E | Per-call timeout never actually cancels the plugin's task — `OnAdrEventAsync` gets the ambient app-lifetime token, not one derived from `CancelAfter(timeoutMs)` — so "timeout" only stops the *host* waiting, never signals the plugin to stop. This is the root mechanism behind finding C. | High | 2 | ✅ Fixed — `InvokeOnceAsync` now creates a linked `CancellationTokenSource` and calls `CancelAfter(timeoutMs)`, so the plugin's own token actually reflects the timeout |
| F | `adrplus sync --backfill` silently swallows Ctrl+C and reports success with exit code 0 — no warning, no per-item accounting for ADRs never reached | Medium-High | 2 | ✅ Fixed — `SyncCommandHandler.ExecuteBackfillAsync` calls `cancellationToken.ThrowIfCancellationRequested()` right after `BackfillAsync` returns, before building the success message |

### Not a bug (info only)

- Ctrl+C responsiveness during dispatch is bounded by the plugin's own timeout, not instant — pass 3 explicitly calls this documented/bounded behavior, not a resilience violation.
- `attempts = 0` on timeout is documented/intentional (all 3 passes agree).

### Calibration note (all 3 passes flagged this)

Several findings (B, C, D, E) are **currently latent** — the only bundled plugin (`AdrIndexer`) has no
real teardown/network logic, so nothing exercises these paths in practice yet. They become real the
moment a third-party plugin with actual resources (HTTP client, connection) exists — which is the
explicit motivating use case for the whole plugin system (ADR002 cites Confluence/Jira/Teams).

## Round 2 status: all fixes applied and verified (2026-08-26)

User's decision: **"corrigir agora preventivamente e currigir o restante"** — fix everything now,
including findings currently latent (no real third-party plugin exists yet to trigger them B/C/D/E), and
resolve the disagreement by fixing it (siding with the 2-of-3 majority that called it a bug).

- [x] User decision on the Finding-vs-Pass-2 disagreement (RetryPendingAsync mid-loop loss) → fix it
- [x] User decision on fix scope/priority given several findings are "latent until a real plugin exists" → fix preventively now
- [x] All 6 findings (A-F) fixed in `PendingStateStore.cs`, `PluginManager.cs`, `SyncCommandHandler.cs`
- [x] `IPluginManager.cs` doc comments updated (`LoadPluginsAsync` reload behavior, `DisposeLoadedPluginsAsync` timeout bound)
- [x] `IAdrPlugin.cs` (public Abstractions contract) doc updated to mention the DisposeAsync/abandoned-hook overlap; `doc/api-abstractions/*.md` regenerated via `dotnet build -c ReleaseDoc`
- [x] New tests added proving each of the 6 fixes (`PendingStateStoreTests`, `PluginManagerRetryTests`, `PluginManagerDisposalTests`, `PluginManagerDispatchTests`, `SyncCommandHandlerTests`) — one test caught a genuine race in its own first draft (two independent timers of the same duration checked without synchronization) and was corrected to wait on the actual cancellation signal instead
- [x] Full `dotnet build` (all TFMs) — 0 errors, 0 warnings
- [x] Full `dotnet test` — 1326/1326 (AdrPlus.Tests, +10 net from round 2) × net8.0/net9.0/net10.0, 13/13 (AdrPlus.Abstractions.Tests) × net8.0/net9.0/net10.0
- [x] Update `convergence.md` for round 2 (done alongside this file)
- [x] Commit — `5176d10` + `a8c97f9` (doc-gap follow-up from `adr-decision-check`)

## Round 3 — Plugin system concurrency (`auditoria-estabilidade`, 3 parallel independent instances, 2026-08-26)

Angle: correctness under concurrency/shared state in the plugin system — explicitly NOT covered by Round 2
(that round's brief was failure-handling, not concurrency under real fan-out). Scope: `PluginManager.cs`'s
`DispatchAsync`/`BackfillAsync`/`RetryPendingAsync` shared state, `PendingStateStore.cs`.

### Confirmed 3-of-3 (highest confidence of the whole audit so far)

| Finding | Severity | Status |
|---|---|---|
| `DispatchAsync`'s parallel fan-out (`Task.WhenAll`) calls `EnsureInitializedAsync` concurrently for every filtered plugin, which does unsynchronized `Contains`/`Add` on the shared `HashSet<IAdrPlugin>` fields `_initializedPlugins`/`_initFailedPlugins` — the same pattern `BackfillAsync` already avoids by running init *sequentially before* its parallel sweep (comment at `PluginManager.cs:440-442`), but `DispatchAsync` runs init *inside* the fan-out. | **High/Critical** | ⏳ Pending — unambiguous fix, will apply |

All 3 instances independently reproduced this empirically with disposable probes (30-200 fake plugins, never-initialized, subscribed to the same event, dispatched once) — reproduced across net8/net9/net10 in every instance. Two failure modes observed: (a) `HashSet.Add` throws `InvalidOperationException` ("concurrent update... corrupted its state") from *inside* `EnsureInitializedAsync`'s own catch block (`PluginManager.cs:725`), escaping `DispatchToPluginAsync` and `Task.WhenAll` uncaught — this contradicts the class's own documented "Never throws for a rejected plugin — fail-soft by design" (`PluginManager.cs:24`) and turns an already-successful local ADR write into a command-level exit-code-1 failure; (b) a lost `Add` silently drops a subscribed, `ShouldHandle=true` plugin from dispatch with no exception, no log, no warning (R3 reproduced 8-109 dropped invocations out of 200 fake plugins across the three TFMs).

### Same mechanism, severity disagreement (not a factual disagreement — same finding, judged differently)

| Finding | R1 | R2 | R3 |
|---|---|---|---|
| `PendingStateStore.UpsertAsync`/`WriteAllAsync` is an unsynchronized read-modify-write over `pending.json` — no cross-*process* lock. Trigger: a cron/CI `adrplus sync` (which ADR002 itself recommends automating) running concurrently with an interactive command touching the same plugin's pending state in the same repo. | Medium — reasons about last-writer-wins entry loss/resurrection; no ADR declares local-file integrity across processes as a guarantee (only "eventual, not transactional" for *external system* sync). | Medium — same mechanism, **plus new empirical evidence**: `WriteAllAsync`'s temp file name is fixed (`pendingPath + ".tmp"`, not unique per writer), so concurrent `UpsertAsync` calls collide on the same temp path and throw `IOException` (reproduced 6/6 in an isolated same-process stress probe) — already fail-soft (caught, warned) at both call sites, but silently drops the pending entry being persisted. | Low/Info — no code path today produces *intra-process* concurrent writes (verified: `MainProgram`'s command loop is sequential, plugin names are deduped); treats it as a documented-gap/latent risk, not a live bug. |

### Single-instance, well-evidenced

| Finding | Severity | Instance |
|---|---|---|
| `_outstandingHooks.TryRemove(plugin, out _)` (`PluginManager.cs:792`) removes by key, not by value — if the same plugin instance times out twice in the same process (e.g. `MigrateCommandHandler`'s per-file dispatch loop hitting the same slow plugin repeatedly) before the first abandoned hook completes, the second hook's tracking entry gets silently overwritten, then erased by the first hook's completion continuation — letting a still-running abandoned hook escape `DisposeCurrentGenerationAsync`'s grace-period wait entirely. | Medium | R1 only |

### Not a bug (verified and dismissed by at least one instance)

- `OnAdrEventAsync` reentrancy / `DisposeAsync` racing an in-flight hook — already documented as the plugin author's contract in `IAdrPlugin.cs` (this session's own Round 2 doc fix) and mitigated by the grace-period wait — R2 confirms this is a *declared* invariant, not a hidden bug.
- `BackfillAsync`'s sequential-init-then-parallel-sweep pattern itself — correct, confirmed by R2 tracing the exact materialization order.
- `RetryPendingAsync` — strictly sequential across plugins, no `Task.WhenAll`, no risk.
- `_outstandingHooks` (`ConcurrentDictionary`) keyed by `LoadedPlugin` (a record wrapping a `class` manifest) — reference equality is stable, not a hash-instability bug (R2 checked explicitly).

## Round 4 — Three new angles over the whole project (single pass each, first use of each pillar here, 2026-08-26)

### `auditoria-usabilidade` — doc vs. code accuracy, whole CLI

| # | Finding | Severity |
|---|---|---|
| U1 | `README.md:381-390`'s `adrplus.json` example is missing the `DefaultSettings` wrapper the real shipped file (`src/AdrPlus/adrplus.json`) requires — copying it verbatim fails `ValidateDefaultSettingsAsync`. `StepByStepGuide.md:167-175` shows the same file correctly (wrapped) — the two guides contradict each other. | High |
| U2 | `PluginDevelopmentGuide.md:308` says `pluginallowlist` lives in "the repo's `adrplus.json`" — there is no per-repo `adrplus.json`; it's host-global, resolved relative to the install directory (`Program.cs:62-64`). `README.md:397` describes it correctly. | Medium/High |
| U3 | The "Simple repository" default profile (`README.md:535-546`) sets `lenrevision: 0`, but the same doc (and `StepByStepGuide.md`) still exemplifies `adrplus revise` without warning it throws `ErrRevisionNotConfigured` under that exact profile. | Medium |
| U4 | `README.md:352` references a config key `FolderRepo` that doesn't exist — the real key is `FolderAdr`/`folderadr`, correctly documented a few lines below (`README.md:484`). | Low |

### `auditoria-complexidade` — unmeasured hypotheses only, per the pillar's own protocol (never a "finding" until gated)

Not routed to `auditoria-desempenho` (real measurement) or `auditoria-estabilidade` (correctness check on any fix) yet — **no action without those two gates**, per the pillar's own acceptance rule. Confirmed: no ADR or doc anywhere declares an expected/acceptable repo scale.

- H1: `adrplus new` (wizard) independently calls `ReadAllAdr` (full repo scan + parse) up to 4×/command (`GetFileByUniqueTitle`, `GetNextNumber`, `GetScopes`, `GetDomains`) where one shared read would do.
- H2: `ParseAdrHeaderAndContentAsync` materializes and retains every ADR's full body string even when a caller only needs one header field (e.g. all 4 wizard calls above never touch `ContentAdr`).
- H3: `ReadAllAdrByNumber`'s glob (`*{sequence}*.md`) degrades toward a near-full-directory scan as the repo grows, for short sequence numbers.
- Side note (not this pillar's remit, flagged for routing): `AdrService.GetLatestADRSequence` (`AdrService.cs:690-696`) ends in `.Last()` over a possibly-empty sequence — a correctness question for `auditoria-estabilidade`/direct verification, not a complexity finding.

### `auditoria-observabilidade` — telemetry vs. real system state, whole project

| # | Finding | Severity |
|---|---|---|
| O1 | `BackfillPluginAsync`'s `ShouldHandle` throw path (`PluginManager.cs:500-504`) does a bare `continue` — increments nothing in `SyncSummary`. If `ShouldHandle` throws for every eligible ADR during `--backfill`, the whole run reports "0 succeeded, 0 skipped, 0 permanently failed, 0 retries exhausted" via `PromptWriteSuccess` (green/success styling) — a fully-failed sweep looks identical to "nothing to do." | High |
| O2 | `AdrEventContext.CorrelationId`/`PendingEntry.CorrelationId` are documented in the **public Abstractions contract** as "for cross-referencing plugin logs with the host's file log" — but no `LogMessages.*` call site anywhere ever includes it; it's write-only (assigned, persisted, never read back or logged). The documented cross-referencing capability doesn't structurally exist today. | High |
| O3 | A plugin's `ShouldHandle` throwing is logged (`LogPluginError`) but never surfaced via `_prompt` (console) at any of its 3 call sites — unlike every other plugin failure path (`WriteWarning`/`WritePermanentFailure`), which always does both. Contradicts ADR002's "Fail-soft: a plugin failure warns and logs" (`ADR002V01:36`). | Medium-High |
| O4 | `LogCommandCompleted` (`CommandRouter.cs`'s `finally`) fires unconditionally, including when the command threw and was already recorded via `LogCommandException` — indistinguishable from a real success in the log stream by name alone. | Medium |
| O5 | `PromptWarnMissingActivePlugins` — the one signal that a configured-active plugin failed to load — only writes to console (`PromptWriteInfo`), never to the log file, unlike every other plugin-system warning. The one channel a non-interactive/cron `sync` run would have to notice this is silent in the log. | Medium |
| O6 | `SyncSummary.Dropped`'s XML doc only documents one of its two real increment causes (unresolvable `adrKey`) — an unrecognized `eventType` in `pending.json` also increments it, undocumented. | Low-Medium |

## Round 3-4 fixes: status (2026-08-26)

- [x] Round 3: HashSet race in `DispatchAsync`/`EnsureInitializedAsync` — fixed (sequential init phase before parallel fan-out, mirroring `BackfillAsync`)
- [x] Round 3: `_outstandingHooks.TryRemove` by key → by key+value
- [x] Round 3: `PendingStateStore` fixed temp filename → unique per writer (fixes the concrete `IOException` collision; the broader cross-process last-writer-wins race is accepted as a documented gap, per user — full locking not requested)
- [x] Round 4 usabilidade: U1-U4 doc fixes (README `DefaultSettings` wrapper, `pluginallowlist` location, `revise`/`lenrevision` caveat, `FolderRepo`→`folderadr`)
- [x] Round 4 observabilidade: O1 (`SyncSummary` no longer zeroed on `ShouldHandle` throw during backfill), O2 (`CorrelationId` **plumbed for real** into log messages at every relevant call site, per user's explicit choice — not just a doc caveat; retry now reuses the entry's original `CorrelationId` instead of minting a new one), O3 (`ShouldHandle` throw now also warns on console via `_prompt.PromptWriteInfo`, not just logged), O5 (new `PluginActivationGate.WarnMissingActivePlugins` helper — all 9 dispatching command handlers now log AND console this warning, not console-only), O6 (`SyncSummary.Dropped` doc fixed)
- [x] New/updated tests for every fix above; full suite 1334/1334 (net8/9/10) before the H2/H3 work below

## Round 5 — `auditoria-desempenho` (real measurement of Round 4's H1-H3 hypotheses, 2026-08-26)

All 3 hypotheses **confirmed with real numbers** (synthetic repos, N=100/1000/5000, warmup+multiple iterations, Release build). See `convergence.md` for the full write-up. Summary:
- H1: 4× redundant `ReadAllAdr` calls in the `new` wizard — confirmed, ~2s wasted at N=5000, imperceptible at N=100.
- H2: `ReadAllAdr` allocates ~5.4-5.6× more bytes than it retains (full body materialized and mostly discarded).
- H3: worse than hypothesized — `ReadAllAdrByNumber` already costs ~99% of a full scan at N=**100**, not just at scale, because every filename embeds `V01` and the glob is an unanchored substring match.

### Fixes applied (user's choice: "faz a opção 2, com o gate de estabilidade" — root-cause fix, gated by an estabilidade check)

- [x] `ParseAdrHeaderAndContentAsync`/`ParseFileName`/`ReadAllAdr` gained an `includeContent` parameter (default `true`, fully backward compatible) — when `false`, skips the wasteful body re-join entirely (fixes H2's actual root cause).
- [x] `GetFileByUniqueTitle`/`GetNextNumber`/`GetScopes`/`GetDomains` now call `ReadAllAdr(..., includeContent: false)` — none of them ever read `ContentAdr` (verified by reading each body). This makes H1's 4 redundant reads each much cheaper, though it does **not** eliminate the 4× redundant directory scan itself (that would require threading a pre-fetched list across the `PromptConsole` UI boundary, rejected as more risk than the wizard warranted right now — see below).
- [x] `ReadAllAdrByNumber` gained a cheap, I/O-free pre-filter (`ParseFileNameOnly`, extracted from `ParseFileName`'s existing name-parsing branches, zero new logic) — filters by `Number` (always filename-derived, for both adrplus-native and migrated files) before paying for the expensive header+content read. Fixes H3 without the correctness trap a naively-narrowed glob would have hit (migrated files don't follow adrplus's own naming convention, so a stricter glob would have silently missed them).
- [x] Related bug found and fixed during implementation: `GetLatestADRSequence` used `.Last()` (throws `InvalidOperationException` on no match) when its own signature/callers (`RejectCommandHandler`'s `?? throw new InvalidDataException(...)`) expect `null` — changed to `.LastOrDefault()`. Red→green: reproduced the throw first, then fixed.
- [x] 5 new tests, full suite 1338/1338 (net8/9/10).
- [ ] **Estabilidade gate**: an `auditoria-estabilidade` pass is running adversarially against this exact change (not concurrency this time — general correctness, especially the migrated-repo edge case and whether `ParseFileNameOnly`'s extraction is behavior-preserving) — pending its report before this is considered closed.

### H1 full fix — done (2026-08-26, user: "pode continuar com o H1")

Eliminated the redundant directory scan count itself, not just made each scan cheaper:
- `AdrService` gained 4 pure, I/O-free "From" variants (`GetFileByUniqueTitleFrom`, `GetNextNumberFrom`, `GetScopesFrom`, `GetDomainsFrom`) operating on an already-read `AdrFileNameComponents[]`; the existing async methods now delegate to them after their own single read (no behavior change for any other caller).
- `INewAdrPrompts.PromptGetArrayScopesAdr`/`PromptGetArrayDomainsAdr` signatures changed from `(IFileSystemService, path, config, ct)` to `(AdrFileNameComponents[] adrFiles, ct)` — `PromptConsole`'s implementation now calls the pure `GetScopesFrom`/`GetDomainsFrom` instead of re-reading.
- `NewAdrCommandHandler.NewAdrWizard` now reads the repository once per folder selection (same point/cache-per-loop-iteration as the pre-existing scope/domain suggestion caching) and returns that array alongside the wizard's parsed args; `ExecuteAsync` reuses it for the title-uniqueness/next-number checks instead of reading again. Non-wizard mode collapses its own 2 reads (`GetFileByUniqueTitle` + `GetNextNumber`) into 1.
- Net result: the wizard's `new` flow now reads the repository **once** total, down from 4.
- Fixed several now-vestigial test mocks in `NewAdrCommandHandlerTests.cs` that referenced the old methods (would have silently passed for the wrong reason otherwise) + added a call-count regression test (`ExecuteAsync_WithWizardMode_ConfirmedYes_ReadsTheRepositoryOnlyOnce`) and 6 new pure-function tests for the "From" methods.
- Full suite 1345/1345 (net8/9/10) after this change.
- [ ] Estabilidade gate for this specific change (UI/service boundary crossing) — running, same rigor as the H2/H3 gate.

### Explicitly not done (scope decision, not oversight)

- `PendingStateStore`'s cross-process race — full locking not implemented, only the concrete temp-filename bug (see Round 3-4 section above).

## Not yet done

- [x] Estabilidade gate result for the H2/H3/`.LastOrDefault()` fix — **accepted**, no blocking findings (one pre-existing Low-severity finding noted, not a regression: `VersionCommandHandler`/`ReviseCommandHandler` use `!` on `GetLatestADRSequence`'s result instead of handling `null` like `RejectCommandHandler` does)
- [ ] Estabilidade gate result for the H1 fix (running)
- [ ] Update `convergence.md` for rounds 3-5
- [ ] Commit everything once both gates report back
