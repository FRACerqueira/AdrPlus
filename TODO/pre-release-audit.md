# Pre-release audit — ADR006 change (Scope/Domain removal + Jaro-Winkler suggestions)

Ephemeral working notes for this audit. Not a standing project doc — delete this folder when the audit concludes.

## Round 1 — 4 independent passes (2026-08-26)

Angles confirmed with user before running:
1. Legacy config migration/tolerance correctness
2. Implementation completeness vs. ADR006's stated scope
3. Jaro-Winkler correctness & UX safety (new suggestion feature)
4. AdrPlus.Abstractions public contract / doc accuracy

### Findings and status

| # | Finding | Pass | Status |
|---|---|---|---|
| 1 | `config --repository --file` bypasses `EnsureFieldsRepoStructure` (obsolete fields survive) | 1 | **Descoped** — user decided (RC policy): validation-level silent tolerance is the only guarantee; no promised auto-strip. ADR006V02 + doc comments corrected to stop overclaiming. |
| 2 | `README.md`/`FAQ.md`/`StepByStepGuide.md` fully document removed scope/domain governance | 2 | ✅ Fixed |
| 3 | `doc/api-abstractions/*.md` stale (still shows `RepoInfoSnapshot.Scopes`, old factory signature) | 2, 4 (both) | ✅ Fixed — regenerated via `dotnet build -c ReleaseDoc` |
| 4 | `PluginDevelopmentGuide.md:48` PackageReference pinned to `1.0.0-rc1` | 4 | ✅ Fixed → `1.0.0-rc2` |
| 5 | Jaro-Winkler doesn't fold diacritics ("Não" vs "Nao" fails threshold by float rounding) | 3 | ✅ Fixed — NFD decompose + strip combining marks |
| 6 | Jaro-Winkler splits UTF-16 surrogate pairs (emoji score objectively wrong) | 3 | ✅ Fixed — compare by `Rune` (codepoint), not `char` |
| 7 | `SuggestSimilar` filters but doesn't rank (doc comment claimed ranking) | 3 | ✅ Fixed — orders substring-match first, then by similarity desc |
| 8 | Dead code in `PromptConsole.cs` (`TitleFields`/`ShowDescField` mapping 4 removed fields) — confirmed by 3 passes independently | 1, 2, 3 | ✅ Fixed — removed |
| 9 | Vacuous "FolderByScope Tests" in `SupersedeCommandHandlerTests.cs` (assertions don't test what names claim) | 2 | ⏳ Pending |
| 10 | Stale class doc comment `SupersedeCommandHandlerTests.cs:37` | 2 | ⏳ Pending |
| 11 | Test coverage gaps: realistic legacy config values, accents, surrogate pairs, `[0,1]` range invariant, `SuggestSimilar`/threshold direct coverage | 1, 3 | ⏳ Pending |
| 12 | Minor: case-folding inconsistency (Contains vs JaroWinkler), swallowed exception in suggestion fetch | 3 | ✅ Fixed — exception now logged; case-folding difference documented as intentional (substring vs fuzzy match are different checks) |

### Trade-off escalated to user (not a "fix", a decision)

- **Plugin ABI compat gate can't distinguish rc1/rc2** (`PluginLoader.IsAbstractionsVersionCompatible`, major-only check). Options presented: (a) accept as known pre-1.0 limitation, (b) strengthen check to compare pre-release identifier, (c) do nothing now.
  - **Decision: user chose to step back — no compat-gate change.** Since the project is in RC, breaking changes are acceptable; instead of strengthening the gate, added explicit breaking-change notes (`CHANGELOG.md`) telling users to reinstall `adrplus` and plugin authors to rebuild against `1.0.0-rc2`. `PluginLoader.cs` is untouched.

## Pillars formalized (2026-08-26)

The 4 ad-hoc angles this round were written up as standing agent definitions at `~/.claude/agents/`
(user-level, mirroring the existing `auditoria-*` house style), so a future round doesn't redefine them
from scratch: `auditoria-migracao-config`, `auditoria-completude-decisao`, `auditoria-algoritmo`,
`auditoria-contrato-publico`. Each is marked "no empirical precedent yet" per this project's own
convention for a brand-new pillar (see `auditoria-complexidade` for the pattern) — this round's findings
are cited in each as the first data point, not proof the pillar is calibrated.

## Round 1 status: all fixes applied and verified (2026-08-26)

- [x] Delete the two vacuous FolderByScope tests in `SupersedeCommandHandlerTests.cs`
- [x] Fix stale class doc comment in same file
- [x] Add missing test coverage: realistic legacy config (`ValidateJsonConfigTests`), diacritics/surrogate-pairs/range-invariant (`StringSimilarityExtensionsTests`), `SuggestSimilar`/threshold direct coverage (new `PromptConsoleTests.cs`, required bumping `SuggestSimilar`/`SimilaritySuggestionThreshold` from `private` to `internal`)
- [x] Full `dotnet build` (all TFMs, src + tests) — 0 errors, 0 warnings
- [x] Full `dotnet test` — 1316/1316 (AdrPlus.Tests, +14 net from round 1), 13/13 (AdrPlus.Abstractions.Tests)
- [ ] Decide whether to commit this round's fixes — pending user

## Additional fixes applied beyond the original 12-item list (per user's RC-simplification decision)

- Corrected `ADR006V02`'s own amendment text and the `EnsureFieldsRepoStructure`/`ObsoleteRepoConfigFields`
  doc comments to stop promising guaranteed auto-strip (only validation-level tolerance is guaranteed).
- Added an explicit "Breaking change - action required" section to `CHANGELOG.md`: uninstall/reinstall
  `adrplus` below rc5, rebuild plugins against Abstractions below rc2. `PluginLoader.cs` left untouched
  (user declined to strengthen the ABI compat gate — RC policy, no shim, document instead).
- Rewrote stale scope/domain content in `README.md`, `FAQ.md`, `StepByStepGuide.md` (JSON examples, field
  tables, "recipes"/profiles, troubleshooting section, upgrade-policy caveat).
- Regenerated `doc/api-abstractions/*.md` via `dotnet build -c ReleaseDoc` instead of hand-editing.
- Bumped `PluginDevelopmentGuide.md`'s example `PackageReference` to `1.0.0-rc2`.

## Notes for whoever continues this (including future-me)

- ADR006 was revised to V02 mid-audit to correct its own record (it had overstated the auto-heal guarantee). V02 is Accepted.
- Global `adrplus` tool was updated to a locally-packed `1.0.0-rc5` build (`dotnet pack` + `dotnet tool update -g adrplus --add-source <tmp-dir> --version 1.0.0-rc5`) to create/approve ADR006V02. Not published to NuGet.
