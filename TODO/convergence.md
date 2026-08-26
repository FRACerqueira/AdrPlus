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
