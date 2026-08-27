<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Allow Version to Change Scope and Domain and Restrict Empty Template to Version|
|Version|01|
|Revision||
|Scope||
|Domain|CLI Design|
|Created|Proposed (2026-08-27)|
|Changed|Accepted (2026-08-27)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Allow Version to Change Scope and Domain and Restrict Empty Template to Version

## Deciders

* Deciders: Fernando Cerqueira (project maintainer)

## Context and Problem Statement

Since [ADR006V03](ADR006V03-remove-scope-and-domain-specific-rules.md), Scope and Domain are free-text header fields populated only at `adrplus new` time — `version` and `revise` both carry them forward unchanged from the source ADR, and neither exposes `-s/--scope`/`-d/--domain`. Both commands also accept `-e/--empty`, starting the new file from a blank template instead of copying the source ADR's body forward.

This pairing no longer fits the documented distinction between the two commands: `version` creates a new major decision on the same topic, while `revise` only fixes or clarifies wording of the *same* decision. A new major version can plausibly belong to a different Scope/Domain than the decision it supersedes-in-place (the topic evolved), but a revision — by definition the same decision, same wording intent — never should. Symmetrically, starting from a blank template makes sense for a new major decision (`version`) but works against `revise`'s own purpose: a revision that begins blank isn't revising anything, it's just writing a new ADR body under an old file name.

Should `version` gain the ability to change Scope/Domain and keep `--empty`, while `revise` loses both?

## Considered Options

* Keep current behavior — both `version` and `revise` carry Scope/Domain forward unchanged and both accept `--empty`.
* Give both `version` and `revise` the ability to change Scope/Domain and keep `--empty` on both.
* Give `version` the ability to change Scope/Domain (flags + wizard) and keep `--empty`; leave `revise` carrying Scope/Domain forward unchanged and remove `--empty` from it.

## Decision Outcome

Chosen option: the third — `version` gains `-s/--scope`/`-d/--domain` (both as CLI flags, matching `new`, and as wizard prompts pre-filled with the source ADR's current values) and keeps `--empty`; `revise` keeps carrying Scope/Domain forward unchanged and loses `--empty` entirely. This is the only option that lines up command capability with each command's already-documented purpose: `version` represents a new major decision, for which changing classification or restarting the body from scratch are both legitimate; `revise` represents a wording-only fix to the same decision, for which changing classification or discarding the existing body both work against what `revise` is for. The first option (do nothing) leaves that mismatch unaddressed. The second (give both commands both capabilities) would let `revise` silently reclassify or blank out a decision it's only supposed to touch cosmetically — no different in effect from calling `version`/`supersede` under the wrong command name.

This is a breaking change to `revise`'s existing CLI surface (`--empty` is removed), but it is being made before the `1.0.0` tag itself has been published (see [ADR007](ADR007V01-adopt-1.0.0-as-the-final-release-version.md) — the version bump was committed, but the maintainer has not yet cut/pushed the tag), so it does not require its own major-version bump under this project's post-1.0.0 breaking-change policy.

Key characteristics of the design:

* `adrplus version` accepts new optional `-s/--scope <text>` and `-d/--domain <text>` flags. When omitted, the new version keeps the source ADR's current Scope/Domain unchanged, same as today; when provided, the new version uses the given value instead.
* `adrplus version --wizard` prompts for Scope and Domain (the same free-text-with-suggestion prompts `new` already uses, via `INewAdrPrompts`), pre-filled with the source ADR's current values as the default, offering suggestions drawn from Scope/Domain values already used elsewhere in the repo.
* `adrplus revise` is unchanged in this respect: it keeps carrying the source ADR's Scope/Domain forward as-is, with no flag or prompt to alter them.
* `-e/--empty` is removed from `adrplus revise`'s accepted arguments entirely (CLI flag and wizard prompt both go away); `adrplus version` keeps `--empty` exactly as it works today.

### Positive Consequences

* `version` and `revise` now match their own documented one-line distinction ("a new major decision on the same topic" vs. "fix/clarify wording, same decision") in what they let the user do, not just in what they're named.
* A topic that outgrows its original Scope/Domain classification can be corrected at the point a new major decision is recorded, without resorting to `supersede` just to relabel it.
* `revise` can no longer be used to silently blank out or reclassify a decision it's meant to only touch cosmetically — removing `--empty` and scope/domain editing from it closes that misuse path.

### Negative Consequences

* Breaking change to `revise`'s CLI surface: any existing script or habit using `adrplus revise --empty` (CLI flag or wizard prompt) stops working and must switch to `version` instead. No compatibility shim is added, consistent with this project's stated policy ([ADR006V01](ADR006V01-remove-scope-and-domain-specific-rules.md)).
* `version`'s help text, wizard flow, and argument surface grow by two more optional flags, adding a small amount of additional complexity relative to today.

## Links

* Relates to [ADR006V03](ADR006V03-remove-scope-and-domain-specific-rules.md) — Scope/Domain remain the same free-text header fields; this ADR only changes which commands can set them.
* Relates to [ADR007V01](ADR007V01-adopt-1.0.0-as-the-final-release-version.md) — this breaking change to `revise`'s CLI surface is made before the `1.0.0` tag is published, so it does not require its own major-version bump.
