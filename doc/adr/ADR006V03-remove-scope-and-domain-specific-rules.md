<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Remove Scope and Domain Specific Rules|
|Version|03|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-08-27)|
|Changed|Accepted (2026-08-27)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Remove Scope and Domain specific rules

## Deciders

* Deciders: Fernando Cerqueira (project maintainer)

## Context and Problem Statement

**This version (V03) narrowly amends V02's Negative Consequences.** V02 added a deliberate exception to this ADR's own "no compatibility shim" policy: `ValidateConfig.ValidateRepoStructure` was made to silently tolerate `adr-config.adrplus` files still carrying the four fields this ADR removes (`scopes`, `lenscope`, `skipdomain`, `folderbyscope`), instead of rejecting them as unexpected. That exception was scoped to the release-candidate period, when configs written by pre-`1.0.0-rc5` installs were still a realistic, current concern. [ADR007](ADR007V01-adopt-1.0.0-as-the-final-release-version.md) now declares `1.0.0` the final release, closing that period — every config written under this project's actual `1.0.0` behavior already omits these fields, so the exception has no live population left to serve. Does the tolerance still earn its keep now that pre-1.0 is over, or should config validation return to this ADR's original, unqualified "no compatibility shim" policy?

The original context below (unchanged from V01/V02) still explains why Scope/Domain lost these fields in the first place; only the Negative Consequences' compatibility-tolerance clause changes in this version.

Scope and Domain today carry seven distinct points of host-enforced behavior beyond being plain descriptive fields: they gate and shape the ADR filename (`AdrPlusRepoConfig.LenScope`), Scope is validated against a configured whitelist (`Scopes`), `SkipDomain` conditions whether Domain is even asked for a given scope, `FolderByScope` drives physical folder layout, the wizard renders Scope as a closed picklist, both are parsed back independently from the filename (position-sensitive regex) and from the header table (fixed line 6/7 offsets) and the two must agree, and Domain also participates in the title-uniqueness key (`AdrFileNameComponents.CreateUniqueTitle`). This spreads Scope/Domain-specific logic across `NewAdrCommandHandler`, `AdrService`, `ValidateConfig`, `AdrFileNameComponents`, and the public `AdrPlus.Abstractions` contract (`RepoInfoSnapshot.Scopes`, `AdrRecordSnapshot.Scope`/`Domain`). Should Scope and Domain remain a governed, whitelist-backed, filename-participating concept, or become plain free-text header fields with no host-enforced rules?

## Considered Options

* Keep the V02 validation-tolerance exception indefinitely — never revisit it, regardless of release status
* Retire the V02 exception now that `1.0.0` is final ([ADR007](ADR007V01-adopt-1.0.0-as-the-final-release-version.md)) — `ValidateConfig.ValidateRepoStructure` rejects `scopes`/`lenscope`/`skipdomain`/`folderbyscope` as unexpected fields again, same as any other unrecognized key
* Retire it but add a one-time migration that rewrites an old config in place instead of just rejecting it

## Decision Outcome

Chosen option: "Retire the V02 exception now that `1.0.0` is final", because the exception's own stated purpose — not hard-failing a config written by a pre-`1.0.0-rc5` install during the release-candidate period — no longer has a real population to protect once `1.0.0` is the actual final release ([ADR007](ADR007V01-adopt-1.0.0-as-the-final-release-version.md)). Keeping a silent-tolerance carve-out alive past the period it was scoped to would mean carrying bespoke compatibility logic indefinitely for no current beneficiary, which this project's own release-candidate policy (V01) already rejected in principle. A migration option was considered and rejected: nothing promises these fields hold a meaningful value worth preserving (V02 already documented that the host never guaranteed removing them from the file), and the fields were dead weight even while tolerated — a config that still carries them today needs a human decision about what replaces the old scope/domain behavior, not a silent rewrite.

Key characteristics of the design (unchanged from V01/V02 except where marked **V03**):

* Scope and Domain become plain free-text header table cells — populated at `new` time, never validated, never parsed back out of the filename.
* `AdrPlusRepoConfig.Scopes`, `LenScope`, `SkipDomain`, and `FolderByScope` are removed from the config schema.
* **V03**: `ValidateConfig.ValidateRepoStructure` no longer tolerates a config that still carries `scopes`/`lenscope`/`skipdomain`/`folderbyscope` — it rejects the file as having unexpected fields, same as any other unrecognized key. `AppConstants.ObsoleteRepoConfigFields` and the tolerance branch that consulted it are removed from `ValidateConfig.cs`.
* The ADR filename generated by `AdrRecord.GetFileName` no longer includes Scope or Domain in any form.
* Title-uniqueness (`AdrFileNameComponents.CreateUniqueTitle`) is based on Title alone; Domain no longer disambiguates two ADRs sharing a title.
* The wizard's Scope prompt changes from a closed picklist (`.Select<string>().AddItems(GetScopes())`) to a free-text input. Both Scope and Domain now share the same input pattern: free text, with a non-blocking "did you mean X?" suggestion (substring match, or Jaro-Winkler similarity ≥ 0.80, against values already used elsewhere in the repo — plain C# implementation, no new dependency, never applied outside the wizard).
* `AdrPlus.Abstractions`'s `RepoInfoSnapshot.Scopes` field is removed — plugin authors relying on it as a validated vocabulary must stop.

### Positive Consequences

* Config schema loses four inter-dependent fields (`scopes`, `lenscope`, `skipdomain`, `folderbyscope`) and the cross-field validation in `ValidateConfig.cs` that kept them consistent with each other.
* The fragile, position-sensitive round-trip parsing of Scope/Domain out of both the filename (regex, avoiding collision with `V`/`R` markers) and the header (fixed line 6/7 offsets) is eliminated.
* The wizard is simpler: one free-text prompt instead of a picklist sourced from repo config plus a conditional skip rule.
* Teams can start using Scope/Domain immediately, without first agreeing on and configuring a fixed vocabulary.
* **V03**: `ValidateConfig.cs` sheds the tolerance branch and its `ObsoleteRepoConfigFields` set entirely — one less exception to this project's "no compatibility shim" policy to explain, maintain, or accidentally rely on going forward.

### Negative Consequences

* Loses the host-enforced guarantee that all ADRs in a repo use a consistent, typo-free Scope vocabulary — inconsistent variants of the same scope can now coexist undetected. The wizard's similarity suggestion (above) narrows this for interactive use but is advisory only: it never blocks, and has no effect at all on non-interactive/CLI-flag/scripted usage, where a typo is still accepted as given.
* `FolderByScope`'s physical-folder-per-scope organization is removed entirely — repos that wanted ADRs grouped by scope on disk lose that option and must self-organize.
* Title-uniqueness becomes coarser: two proposals sharing a title but differing only by domain, previously allowed as distinct ADRs, now collide and must be retitled.
* Breaking change to `adr-config.adrplus`'s schema and to the public `AdrPlus.Abstractions` contract — the config schema itself carries no compatibility alias, and `RepoInfoSnapshot.Scopes` is gone outright for plugin authors, with no shim.
* ADRs created before this change that carry Scope/Domain in their filename keep that filename (existing files are not renamed retroactively), while ADRs created after will not — filenames within an already-adopted repo become inconsistent unless someone renames the old ones by hand.
* **V03**: a real `adr-config.adrplus` still carrying any of the four removed fields — one hand-edited outside `adrplus`, or never updated since before this ADR's original adoption — now fails validation outright instead of loading with the fields silently ignored. This is a genuine behavior change for that narrow case. **The only fix is removing the four fields from the file by hand** (or supplying `--file` pointing at an already-clean config to `adrplus init`/`config --repository`) — neither command can clean an existing dirty file for you: `EnsureFieldsRepoStructure`'s own field-stripping step was removed together with the tolerance it served, `config --repository` always writes to the machine's shared install-level template regardless of any `--path` (never a specific repository's file), and that template rejects a dirty input exactly like `ValidateRepoStructure` does for any other config, interactively or not.

## Links

* Amends [ADR006V01](ADR006V01-remove-scope-and-domain-specific-rules.md) — see V02 below for what that amendment corrected.
* Amends [ADR006V02](ADR006V02-remove-scope-and-domain-specific-rules.md) — retires the validation-tolerance exception V02 introduced, now that [ADR007](ADR007V01-adopt-1.0.0-as-the-final-release-version.md) closes the release-candidate period the exception was scoped to.
