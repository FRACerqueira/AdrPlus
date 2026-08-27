<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Adopt 1.0.0 as the final release version|
|Version|01|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-08-27)|
|Changed|Accepted (2026-08-27)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Adopt 1.0.0 as the final release version

## Deciders

* Deciders: Fernando Cerqueira (project maintainer)

## Context and Problem Statement

The project's `<Version>` in `src/AdrPlus/AdrPlus.csproj` and `src/AdrPlus.Abstractions/AdrPlus.Abstractions.csproj` has moved through `1.0.0-rc1` … `1.0.0-rc5` across recent commits, with each rc's rationale recorded in `CHANGELOG.md`. None of that has actually shipped: the release workflows (`.github/workflows/release.yml`, `release-abstractions.yml`) derive the published version from the git tag at publish time, not from the csproj, and the newest tag in this repository is still `v1.0.0-beta5` — no `v1.0.0-rc*` tag and no `abstr-v*` tag exists at all, so `AdrPlus.Abstractions` has never actually been published to NuGet despite being described elsewhere as independently released. A recent hardening pass (revalidating every command and argument against its own `--help` text and documentation) found and fixed the last known class of bug blocking a stable release: a redundant, incorrectly-scoped `withoutargs` fallback inside argument parsing, a required-argument check that could never detect a fully-missing flag, and the `revise`/`migrate` correctness bugs it also surfaced. With those closed, is the project's CLI surface and plugin contract stable enough to stop iterating through release-candidate numbers and adopt `1.0.0` as the actual final version — and if so, does that decision also mean retiring release-candidate-era accommodations that no longer have a real population to serve?

## Considered Options

* Keep incrementing `rc` numbers until some later, unspecified point felt "more final" than now
* Adopt `1.0.0` as the final version now, in the csproj files and every piece of documentation that names a specific pre-1.0 version — but leave creating and pushing the actual `v1.0.0` git tag (which triggers the publish workflow) as a separate, later, deliberately human-triggered action
* Adopt `1.0.0` and immediately tag and publish it as part of this same decision

## Decision Outcome

Chosen option: "Adopt `1.0.0` as the final version now... but leave creating and pushing the actual `v1.0.0` git tag... as a separate, later, deliberately human-triggered action", because there is no remaining technical blocker to calling this version final — the rc cycle's own purpose (shaking out exactly the kind of parsing/correctness bugs the recent hardening pass found) has been served — while actually cutting a public release (pushing a tag, triggering `dotnet pack`/`nuget push` for two packages) is a separate, irreversible-in-practice, publicly-visible action that deserves its own deliberate trigger rather than happening as a side effect of a documentation-and-versioning decision made in the course of an editing session.

Key characteristics of the chosen design:

* `AdrPlus.csproj` and `AdrPlus.Abstractions.csproj`'s `<Version>` become `1.0.0` (no suffix). Both move together — they have been bumped in lockstep at every prior rc step, and neither has actually shipped yet, so there is no independent compatibility reason to stagger them now.
* `CHANGELOG.md`'s `[Unreleased]` section (all of this session's fixes, plus everything already sitting there) becomes `## [1.0.0]`, dated with this decision. Every prior version section (`0.1.0` through `1.0.0-rc4`) stays exactly as written — that is this project's real release history and stays intact regardless of the fact that most of it, too, was never tagged.
* Every place in the documentation that states a specific pre-1.0 version as a "minimum required version" or a "current version" fact (README.md, NugetREADME.md, PluginDevelopmentGuide.md's plugin-project example, CLAUDE.md's status line, the pre-1.0 upgrade-caveat block in README.md, the rc-specific breaking-change instructions carried over from `[Unreleased]`) is corrected to reflect `1.0.0` as the actual, final floor — dropping language like "release candidate", "pre-1.0", and "no compatibility shim... during the release-candidate period" that no longer describes the project's status.
* [ADR006V03](ADR006V03-remove-scope-and-domain-specific-rules.md) retires the one real, deliberate release-candidate-era accommodation this project ever carried — `ValidateConfig`'s silent tolerance of `adr-config.adrplus` files still holding the four fields ADR006 removed — since that tolerance's own documented purpose (not hard-failing a config written by a pre-`1.0.0-rc5` install during the rc period) has no live population left to protect once `1.0.0` is the final, adopted version.
* Historical decision context inside existing ADRs (e.g. ADR001's "Before 1.0.0-beta4, only English and Portuguese...", ADR005's "documented under 1.0.0-rc3") is left untouched — it describes what was true at the time that decision was made, not the project's current status, and rewriting it would falsify the record.
* The actual `v1.0.0` git tag is **not** created or pushed as part of this decision. That remains a distinct, later action for the maintainer to trigger deliberately, at which point the already-tagless `AdrPlus.Abstractions` line also gets its first real published release.

### Positive Consequences

* Every doc claim about "requires v1.0.0-rc1 or later" / "pre-1.0 release candidate" collapses to a single, stable floor (`1.0.0`) instead of a moving rc target that was already stale by two versions in `CLAUDE.md` before this decision.
* Closes the gap this ADR's own investigation surfaced: `AdrPlus.Abstractions` has been described in multiple docs as "released independently under its own `abstr-v*` tag series" while never actually having one — adopting `1.0.0` for both packages together, ahead of their first real tag, prevents that description from staying wrong indefinitely.
* Removes one whole category of "is this actually a divergence, or just pre-1.0 noise" ambiguity from future documentation-vs-behavior audits of this project.

### Negative Consequences

* A version number in a csproj or CHANGELOG heading that says `1.0.0` before the corresponding `v1.0.0` tag exists is, strictly, aspirational until that tag is actually pushed — a reader diffing the repository against what's live on NuGet during that window sees a project whose docs already claim `1.0.0` while the newest real release remains `v1.0.0-beta5`. This is accepted as a deliberate, temporary, and honestly-disclosed state (see this ADR's own Context section), not hidden.
* Retiring ADR006V02's compatibility tolerance ([ADR006V03](ADR006V03-remove-scope-and-domain-specific-rules.md)) is a real breaking change for the narrow case of a config file that still carries the four removed fields — see that ADR's own Negative Consequences for the specific impact and remedy.
* Once the `v1.0.0` tag is eventually pushed, this project's own "avoid breaking changes... unless critical" release-candidate policy (recorded in `CLAUDE.md`'s project-status section) no longer applies at all — from that point on, any breaking change to the CLI or to `AdrPlus.Abstractions` is a real semver-major event, not a pre-1.0 adjustment. That's the intended effect of calling this version final, not a side effect, but it does raise the bar for every change after this one.

## Links

* Prerequisite for [ADR006V03](ADR006V03-remove-scope-and-domain-specific-rules.md), which retires ADR006V02's release-candidate-scoped config-validation tolerance now that this ADR ends the release-candidate period.
