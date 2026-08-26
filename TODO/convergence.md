# Convergence tracking — Round 1 (4 independent passes, 2026-08-26)

Passes ran independently: separate agent invocations, none shown another's transcript or conclusions,
each given only its own angle + the code. This file cross-references where they agreed or disagreed,
per the pre-release-audit skill's "merge afterward, report disagreement as disagreement" rule.

Pass legend: **P1** legacy config migration · **P2** ADR006 completeness · **P3** Jaro-Winkler correctness · **P4** Abstractions contract/docs

## Findings confirmed by 2+ independent passes (high confidence)

| Finding | Confirmed by | Agreement detail |
|---|---|---|
| Dead code in `PromptConsole.cs` (`TitleFields`/`ShowDescField` mapping the 4 removed fields) | **P1, P2, P3** (all three) | All three independently traced the same two locations (lines ~576-579, ~1407-1410) and reached the same conclusion: unreachable, safe to remove. No disagreement on severity (all called it cosmetic/low-risk). |
| `doc/api-abstractions/*.md` stale (still documents `RepoInfoSnapshot.Scopes` and old factory signature) | **P2, P4** | Both cited the exact same two files and line ranges independently. P4 additionally traced *why* (generated via `dotnet build -c ReleaseDoc`, never re-run for this change) — P2 didn't have that context, consistent rather than conflicting. |

## Findings from a single pass (not contradicted, just not in another pass's scope)

These weren't checked by a second pass because they fell outside the other three angles' remit — not
independently verified, but no pass produced a conflicting read either.

- P1: `config --repository --file` bypasses `EnsureFieldsRepoStructure` — only P1's angle covered this code path.
- P2: `README.md`/`FAQ.md`/`StepByStepGuide.md` stale content; vacuous `SupersedeCommandHandlerTests.cs` tests; stale test class comment.
- P3: diacritic-folding bug, surrogate-pair bug, `SuggestSimilar` doesn't rank, swallowed exception, case-folding inconsistency.
- P4: `PluginDevelopmentGuide.md:48` stale version pin; the rc1/rc2 ABI-gate blind spot.

## No disagreements found

No two passes reached conflicting conclusions about the same piece of code. The closest to a tension:
P1 and P4 each separately flagged a version-compatibility concern — P1 didn't examine the plugin ABI gate
(out of its angle) and P4's finding there was novel, not a re-confirmation, so this isn't logged as
agreement above; noted here only so it's not mistaken for independent confirmation.

## What this round's passes did NOT check (gaps in the audit itself, not in the code)

- No pass ran the actual test suite as part of its review (all four were static/traced-reasoning reviews,
  P3 partially verified empirically via an isolated scratchpad script). Verification that fixes are
  green happens after this round's fixes land — see `TODO/pre-release-audit.md`'s pending checklist.
- No pass covered the localization/resx files beyond confirming the deliberate "leave orphaned strings"
  decision was consistent (P1 and P2 both surfaced this and both treated it as accepted, not a defect —
  that agreement isn't in the table above since it's a "confirmed non-finding," not a finding).
- Performance/complexity angle was not one of the four confirmed angles this round (the project's
  `auditoria-complexidade`/`auditoria-desempenho` personas exist but weren't invoked) — P3 noted the
  Jaro-Winkler algorithm's lack of a length cap as low-severity, but that was incidental to its correctness
  angle, not a dedicated performance pass.

---

# Convergence tracking — Round 2 (3 parallel instances of `auditoria-resiliencia`, same day)

Single angle (plugin system resilience vs. ADR002), run as 3 independent instances of the *same* pillar
agent per its own built-in protocol, rather than 3 different angles like round 1. The agent's own rule:
a Medium-severity claim needs 2-of-3 agreement to be accepted as real; report disagreement as disagreement
rather than resolving it unilaterally.

Instance legend: **R1**, **R2**, **R3** (arrival order — R1 landed first, then R3, then R2)

## Findings confirmed by 2+ independent instances (high confidence)

| Finding | Confirmed by | Agreement detail |
|---|---|---|
| `pending.json` corruption/write failure breaks the fail-soft guarantee (propagates to command exit code; aborts the whole retry loop for every other plugin) | **R1, R3** | Both independently traced the same non-atomic-write root cause and the same unhandled-exception path through `DispatchAsync`/`RetryPendingAsync`. R3 additionally traced it through `ApproveCommandHandler` specifically and elevated severity to Critical — a deepening, not a conflict. |
| Same-process plugin reload skips `InitializeAsync` on the new instance (stale per-name init bookkeeping) | **R2, R3** | Confirmed via two *different* probe methodologies — R3 reproduced the exact wizard-reload call path end-to-end; R2 manipulated the test seam directly (couldn't invoke the real reload without a compiled assembly) but established the same precondition by reading `PluginLoader.LoadAssembly`'s stateless construction. Independent convergence on the same root cause via different evidence paths is stronger, not weaker, confirmation. |
| Abandoned timed-out hook races with `DisposeAsync` on the same instance | **R1, R3** | R1 flagged this as Low/code-read-only, explicitly not corroborated at the time. R3 independently found it too and *empirically* confirmed it with a probe (`hookStillRunningWhenDisposeCalled == true`), which is what elevates it from "R1's uncorroborated read" to a real, confirmed finding. |

## Genuine disagreement — not resolved, reported as disagreement

**Does `RetryPendingAsync` losing already-succeeded entries within one plugin's loop (when cancelled mid-loop, before the single end-of-loop `WriteAllAsync`) count as a bug?**

- **R1 and R3: yes, a bug.** Both point to `BackfillPluginAsync` (same file, same author) having an explicit, tested guard against the *identical* failure shape ("a cancelled item shouldn't erase every prior item's outcome") while `RetryPendingAsync` has none — asymmetric protection for the same risk is evidence of an oversight, not a considered trade-off.
- **R2: no, not a new finding.** R2 examined the same mechanism and concluded it's already covered by ADR002's own stated Negative Consequence ("eventual, not transactional — idempotency expected"), and that no *cross-plugin* durable state is lost (each plugin's own file is written before moving to the next plugin).
- This is a real three-way review with a real split (2 say bug, 1 says by-design) — see `pre-release-audit.md`'s decision table. Not something I'm resolving unilaterally; it's the user's call.

## Findings from a single instance (not contradicted, evidenced but not corroborated by count)

- **R2 only**: `DisposeLoadedPluginsAsync` has no timeout at all — a slow, non-throwing `DisposeAsync` hangs *every* command exit, not just shutdown. Empirically demonstrated by R2 itself (a probe with a hanging `DisposeAsync` and a pre-cancelled token that was ignored) — single-instance origin, but the claim isn't merely asserted, it's shown.
- **R2 only**: the per-call timeout never actually cancels the plugin's task (no `CancelAfter`-derived token passed to `OnAdrEventAsync`) — this is the mechanistic root cause underneath the R1/R3-confirmed "hook races with dispose" finding above. Code-read only, not separately probed, but consistent with and explanatory of a finding two other instances already confirmed independently.
- **R2 only**: `adrplus sync --backfill` silently swallows Ctrl+C, reports success, exit code 0. Code-read + points to an existing test (`PluginManagerBackfillTests`) to establish that the manager-level swallow is deliberate, then shows the gap is one layer up in `SyncCommandHandler` never checking `IsCancellationRequested` afterward.

---

# Convergence tracking — Round 3 (3 parallel instances of `auditoria-estabilidade`, 2026-08-26)

Same protocol as Round 2: 3 independent instances of the same pillar, 2-of-3 for Medium severity, disagreement reported as disagreement. Angle: concurrency/shared-state correctness in the plugin system — deliberately not covered by Round 2 (failure-handling only).

## Confirmed by all 3 instances (strongest signal in either audit so far)

| Finding | Confirmed by | Agreement detail |
|---|---|---|
| `DispatchAsync`'s parallel fan-out corrupts the shared, unsynchronized `HashSet<IAdrPlugin>` (`_initializedPlugins`/`_initFailedPlugins`) when 2+ never-initialized plugins share a subscribed event | **R1, R2, R3** (all three) | Each independently wrote and ran a disposable probe reproducing it empirically across net8/net9/net10 — R1 and R2 both hit an uncaught `InvalidOperationException` escaping `Task.WhenAll` (command-level exit-code-1 failure contradicting the class's own "fail-soft by design" doc); R3's probe instead demonstrated the silent variant (a subscribed plugin never invoked, no exception, no log). Both failure modes stem from the same unsynchronized root cause — a deepening, not a conflict. R1 explicitly noted seeing R2's and R3's uncommitted probe files mid-investigation and treated that as independent corroboration without touching them. |

## Same finding, severity judged differently (not a factual disagreement)

**Is the `PendingStateStore` cross-process read-modify-write race a live bug or a documented/latent gap?**

- **R1: Medium.** Reasons from the code alone (last-writer-wins on `pending.json` between a cron `sync` and an interactive command); no ADR declares cross-process local-file integrity as a guarantee, only "eventual, not transactional" for *external* sync.
- **R2: Medium, with new empirical evidence.** Found and reproduced (6/6, isolated same-process stress probe) a concrete `IOException` from `WriteAllAsync`'s temp filename being fixed (not unique per writer) — already fail-soft at both call sites, but silently drops the entry being persisted.
- **R3: Low/Info.** Verified no code path today produces intra-process concurrent writes (sequential command loop, deduped plugin names) — treats the cross-process case as a real but currently-latent gap, not a live bug.

Not logged as a disagreement in the same sense as Round 2's (all three agree on the mechanism and that it's real); the split is purely how severely to weight a risk that's proven in isolation (R2) but not yet observed via any real production code path (R1, R3 agree on this point).

## Single-instance, well-evidenced

- **R1 only**: `_outstandingHooks.TryRemove(plugin, out _)` removes by key, not by value — a plugin instance timing out twice in the same process (e.g. `MigrateCommandHandler`'s per-file loop) can have its second, still-running abandoned hook's tracking entry silently erased by the first hook's completion continuation, letting it escape `DisposeCurrentGenerationAsync`'s grace-period wait.

## What this round did NOT check

- Real third-party plugins still don't exist — every reproduction used synthetic fake plugins (`Substitute.For<IAdrPlugin>()`-style or hand-written fakes), consistent with Round 2's own calibration note.
- No instance benchmarked lock-based fix candidates for overhead — that's `auditoria-desempenho`'s remit if a fix is chosen that needs it.

---

# Convergence tracking — Round 4 (3 different angles, single pass each, 2026-08-26)

Unlike Rounds 2-3, this round ran 3 *different* pillars once each (like Round 1), not multiple instances of one pillar — each is the first-ever run of that pillar on this project, so none of them carry the "2-of-3" convergence bar internally; each stands as a single, well-evidenced pass.

- **`auditoria-usabilidade`**: 4 findings, all doc-vs-code contradictions with an exact code citation for each (missing `DefaultSettings` wrapper in README's example; wrong file location claimed for `pluginallowlist`; undocumented `revise`-vs-default-profile incompatibility; a config key name that doesn't exist). Also recorded a "not-a-finding" list (things it checked and confirmed correct) for transparency.
- **`auditoria-complexidade`**: first run ever on this project — produced 3 explicitly unmeasured hypotheses plus one correctness side-note routed elsewhere, and confirmed no ADR anywhere declares a scale target. Per the pillar's own protocol, none of these are "findings" until gated through `auditoria-desempenho` (real measurement) and `auditoria-estabilidade` (correctness check on any proposed fix) — that gating hasn't happened yet.
- **`auditoria-observabilidade`**: 6 findings, empirically grounded in confirming the app's file-log sink actually captures Information-level messages by default (so "only in the log" is a real, reachable signal, not moot). Two are High: `SyncSummary` can read as a clean "0/0/0/0" success when `--backfill`'s `ShouldHandle` throws on every item, and the public `CorrelationId` contract promises log cross-referencing that doesn't structurally exist in any current log call site.

## No other disagreements found

All three instances independently agreed `attempts = 0` on timeout is intentional/documented, and that
the config-validation/first-install startup paths are correctly covered by `MainProgram`'s `finally`
before any plugin is ever loaded (R2 explicit; R1/R3 didn't contradict).

## What this round did NOT check

- No instance benchmarked or stress-tested under real concurrent load (`Task.WhenAll` fan-out in
  `DispatchAsync` itself) — this round's brief was resilience/failure-handling, not concurrency/shared-state
  correctness under load, which is `auditoria-estabilidade`'s remit (still unused this project).
- All three instances flagged that several findings (reload-skip-init, dispose-race, no-dispose-timeout,
  timeout-doesn't-cancel) are *currently latent* because the only bundled plugin (`AdrIndexer`) has no
  real teardown/network logic — none of the three could demonstrate real-world impact beyond a synthetic
  probe plugin, since no third-party plugin exists yet to observe failing in practice.
