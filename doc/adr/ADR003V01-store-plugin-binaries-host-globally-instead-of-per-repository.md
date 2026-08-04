<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Store plugin binaries host-globally instead of per-repository|
|Version|01|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-08-04)|
|Changed|Accepted (2026-08-04)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Store plugin binaries host-globally instead of per-repository

## Deciders

* Deciders: Fernando Cerqueira (project maintainer)

## Context and Problem Statement

[ADR002](ADR002V01-add-a-plugin-system-for-adr-lifecycle-event-integrations.md) established a plugin system, but plugin binaries need somewhere to live on disk and a way for the host to discover them. The original v1 design stored each plugin under the repository itself (`<repo>/plugins/<name>/`), committed to git so a team shares the same plugins on clone. Where should plugin binaries actually be stored, and how should a repository control which of them are active for it?

## Considered Options

* Per-repository storage: `<repo>/plugins/<name>/`, plugin binaries committed to each repository that uses them
* Host-global storage: `%UserProfile%/AdrPlus.Plugins/<name>/`, merged with a bundled `plugins-builtin/` root, installed once per machine

## Decision Outcome

Chosen option: "Host-global storage", pivoting away from the original per-repository design (D36), because plugin binaries don't need to be duplicated and versioned separately in every repository on a machine — only the per-repo on/off switch does.

Key characteristics of the chosen design:

* Plugins are discovered from folders, each with its own `plugin.json` manifest (name, version, entry assembly/type, `abstractionsVersion`, subscribed events, timeouts, retry policy, settings).
* Plugin binaries live at `%UserProfile%/AdrPlus.Plugins/<name>/`, merged with whatever ships bundled with the AdrPlus install itself (`plugins-builtin/`) — shared across every repository on that machine.
* Each repository controls activation independently via `activeplugins`/`disableplugins` in its own `adr-config.adrplus` — the on/off switch, never the plugin code itself.
* Each plugin loads into its own isolated `AssemblyLoadContext`, with private dependencies resolved per plugin folder.
* On load, the host validates manifest schema, that the entry type implements `IAdrPlugin`, that `Name`/`Version` match the manifest, that `abstractionsVersion` is SemVer-major compatible, and rejects duplicate plugin names.
* `--install`/`--uninstall` are host-global, zip-based operations against the shared plugin store — they take no `--path`, since no single repository is in scope.
* Pending-dispatch state stays per-repository (`<repo>/plugins-state/<name>/`), to avoid cross-repo collisions on the same `adrKey` now that plugin binaries are shared across every repository on the machine.

### Positive Consequences

* Plugin binaries are installed once per machine instead of duplicated per repository, while each repository still independently controls which installed plugins are active for it.
* A team no longer needs to commit plugin binaries into every repository that wants them; only the lightweight `activeplugins` toggle lives with the repo.
* `init` no longer needs to copy a bundled plugin into new repositories, since it's discovered host-globally without a copy step.

### Negative Consequences

* A developer must install a plugin once per machine before any repository on that machine can activate it — there is no longer a "clone the repo and you already have the plugin" experience.
* `--uninstall` no longer edits any repository's `activeplugins` (no single repository is in scope); drift between an active-but-uninstalled plugin surfaces as the existing "Missing" warning instead of being cleaned up automatically.
* Higher memory footprint from one isolated `AssemblyLoadContext` per loaded plugin, accepted as a cost of dependency isolation.

## Links

* Storage-pivot commit: `21d443a` ("Pivot plugin storage from per-repo to host-global (D36)")
* Original design spec, describing the earlier per-repository storage (preserved in git history, no longer in the working tree): `git show 197cd49:doc/Todo/adrplus-plugin-architecture.md` (commit `197cd49`)
* Related [ADR002](ADR002V01-add-a-plugin-system-for-adr-lifecycle-event-integrations.md) — the plugin dispatch model this storage serves
* Related [ADR004](ADR004V01-publish-the-plugin-contract-as-an-independent-adr-plus-abstractions-package.md) — how the plugin contract is packaged and distributed
