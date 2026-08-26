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
- [ ] Decide whether to commit this round's fixes — pending user
