![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)

# Changelog

All notable changes to **AdrPlus** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added

- The `new` wizard's Scope and Domain prompts now suggest similar values already used elsewhere in the
  repository (Jaro-Winkler similarity, plain-C# implementation, no new dependency) - e.g. typing
  `"Backend"` when `"Back-End"` already exists surfaces it as a suggestion. Purely advisory: it never
  blocks, rejects, or silently rewrites what the user types, keeping this consistent with ADR006 (no
  host-enforced rules on these fields). Not applied in non-interactive/CLI-flag mode, where a value the
  caller explicitly passed is always used exactly as given (important for scripted/CI/AI-agent usage).

### Removed

- Scope and Domain are now plain free-text header fields with no host-enforced rules
  (see [ADR006](doc/adr/ADR006V02-remove-scope-and-domain-specific-rules.md)). Removed from
  `adr-config.adrplus`: `scopes` (whitelist), `lenscope` (filename participation/truncation),
  `skipdomain` (per-scope domain skipping), `folderbyscope` (per-scope subfolders). Removed from the
  public `AdrPlus.Abstractions` contract: `RepoInfoSnapshot.Scopes`. A legacy `adr-config.adrplus` that
  still carries these fields is tolerated - it's never rejected as invalid, they're just ignored - but
  the host does not guarantee removing them from the file for you; that's a manual cleanup if you want it.

### Changed

- ADR filenames no longer embed Scope or Domain in any form; both remain visible only in the ADR's
  header table. Title-uniqueness for `adrplus new` is now based on Title alone (previously Title+Domain).
- The `new` wizard's Scope prompt is now free text (previously a picklist sourced from the repository's
  configured scope whitelist), matching Domain's existing free-text-with-autocomplete behavior.
- Bumped to `1.0.0-rc5` (`AdrPlus.Abstractions` to `1.0.0-rc2`) for this breaking config/contract change.

### Fixed

- `adrplus init --path <dir>` (no `--file`) on an already-initialized directory used to always prompt
  for overwrite confirmation, even with no interactive console to answer it (CI, scripts, an AI agent) -
  it crashed with `Cannot run an interactive control: console input is redirected...` instead of
  reinitializing or failing cleanly. It now detects the redirected console and skips the prompt,
  failing with the same clean `Configuration file already exists at: <path>` error a declined
  confirmation already produced. Reinitializing non-interactively still works via `--file`, which
  already bypassed this confirmation entirely.
- `revise` never actually incremented the revision number - a C# operator-precedence bug
  (`Revision??0 + 1`, which binds as `Revision ?? (0 + 1)` instead of `(Revision ?? 0) + 1`) meant the
  new revision was always identical to the source ADR's, so `revise` on any ADR already at revision 1
  or higher failed with `The file already exists`. Only the very first revision of an ADR (revision 0)
  ever succeeded.
- `migrate` accepted any `.md` file long enough to cover the configured `migrationpattern`'s field
  positions, even when the sequence-number, version, or revision segment it extracted from the filename
  wasn't actually numeric - the bad segment was silently treated as `0` instead of the file being
  rejected. A legacy file that didn't genuinely match the pattern (e.g. `DECISION-001.md` under a
  pattern expecting a leading numeric sequence) could migrate anyway and collide with any other
  non-matching file on `Number == 0`. Such files are now rejected as invalid, matching the "Files NOT
  Migrated" behavior documented in `MigrationGuide.md`.
- Omitting a command's own required argument (e.g. `--file` for `approve`/`reject`/`undo`/`revise`/
  `version`/`supersede`, `--title` for `new`) without `--wizard` used to crash with a raw, internal
  `The given key '...' was not present in the dictionary` error instead of a real validation message -
  the "Required When not in wizard mode" check only ever inspected arguments that were already present,
  so a fully-omitted one was never actually caught. It's now reported as a clean
  `Required argument '--x' (-x) is missing` error, for every command, consistently.
- A command run with none of its own flags used to silently re-consult the global `withoutargs` setting
  (meant only for the truly argument-less `adrplus` invocation) a second time, at the individual-command
  level - this broke `adrplus help <command>` under `withoutargs: Wizard` (crashed trying to launch an
  interactive wizard for a non-interactive help request) or `None` (crashed with the same raw dictionary
  error above), and made any command silently exit `0` with just its help text under the default `Help`
  setting instead of failing when a required argument was missing. `adrplus help <command>` now always
  shows that command's help, and a bare command with missing required arguments now always fails with
  the same clean, real error, independent of `withoutargs`.
- `--path` (`--domain`/`--scope` for `new`, `--file` for `explore`'s report path) was mislabeled
  "Required When not in wizard mode" in every command's help text, even though `init`/`explore`/
  `migrate`/`sync`/`plugins` already treated it as genuinely optional (with their own clean
  `DirectoryNotFoundException`-style validation) and `new`'s Domain/Scope have never been validated at
  all (see `FAQ.md`). Only `new`'s `--path` access was actually crash-prone if omitted - it now uses the
  same safe pattern as the other five commands. Help text for all of these now correctly reads
  "(Optional)".

### Breaking change - action required

- This is a pre-1.0 release-candidate breaking change with no compatibility shim, by design (see ADR006).
  **If you have `adrplus` installed globally at a version older than `1.0.0-rc5`, uninstall it and
  reinstall `1.0.0-rc5` rather than upgrading in place** (`dotnet tool uninstall -g adrplus` then
  `dotnet tool install -g adrplus`). **If you maintain a plugin built against `AdrPlus.Abstractions`
  older than `1.0.0-rc2`, rebuild it against `1.0.0-rc2`** - the host's plugin-loader compatibility
  check only compares the major version number, so an un-rebuilt plugin referencing the removed
  `RepoInfoSnapshot.Scopes` will load successfully and then fail at runtime the first time it's
  dispatched an event, rather than being rejected up front.

---

## [1.0.0-rc3] - 2026-08-08

### Added

- `firstinstaller.adrplus`: an opt-in seed file (`<install dir>/template/firstinstaller.adrplus`, same
  JSON schema as `adr-config.adrplus`) that lets a team pre-provision approved repository settings for
  automated/CI/AI-agent first installs, instead of the tool's built-in defaults. Scoped strictly to
  non-interactive sessions - the same redirected stdin/stdout check that already gates the wizard - so
  a human at a real terminal always gets the guided wizard regardless of whether this file is present
  (see [ADR005](doc/adr/ADR005V01-scope-the-firstinstaller.adrplus-automation-seed-to-non-interactive-first-installs-only.md)).
  Applied at most once (renamed to `firstinstaller.adrplus.applied` on success); a retry after an
  interrupted prior run completes safely instead of failing.

### Fixed

- `adrplus init --path <dir>` (no `--file`) no longer throws a raw `FileNotFoundException` on a
  completely fresh, non-interactive install where the installation's default repository template had
  never been generated. This closes a gap the 1.0.0-beta3 fix left open: that release moved the
  "config already exists, confirm overwrite" branch onto `GetConfigDefaultRepoContentAsync`'s in-memory
  fallback, but the structurally identical "no `--file`, no existing config" branch was never updated
  and kept reading the file directly - unnoticed since most installs already had the template file
  from an earlier run.

---

## [1.0.0-rc2] - 2026-08-07

### Fixed

- Corrected `README.md`/`NugetREADME.md`: the AI-plugin section required `adrplus` v1.0.0-beta1 or
  later, but beta1/beta2 weren't actually safe to drive non-interactively. Raised the documented
  floor to v1.0.0-rc1, matching the companion `AdrPlus-IA-Plugin` repo's own requirement.

---

## [1.0.0-rc1] - 2026-08-06

### Changed

- Bumped `AdrPlus.Abstractions` to `1.0.0-rc1` alongside the CLI tool (still released independently under its own `abstr-v*.*.*` tag series — see [AbstractionsREADME.md](AbstractionsREADME.md)); updated the version shown in the `PackageReference` example in [PluginDevelopmentGuide.md](PluginDevelopmentGuide.md).

### Fixed

- Corrected a stale `1.0.0-beta3` version reference in `CLAUDE.md`'s "Current Project Status" section.
- Fixed a README table-of-contents entry ("Rules for adr commands") that didn't match its own section heading ("Rules by ADR commands").
- Updated `README.md`/`NugetREADME.md` references to the Claude Code integration: the companion plugin repository was renamed from `AdrPlus-Claude-Plugin` to `AdrPlus-IA-Plugin` and now also supports GitHub Copilot, not just Claude Code — docs, links, and the "Using AdrPlus with Claude Code" section (renamed to "Using AdrPlus with AI Coding Assistants") updated to match.

---

## [1.0.0-beta9] - 2026-08-05

### Changed

- Updated the `PromptPlus` dependency from `6.0.0-Beta9` to `6.0.0-rc1`. User-visible effect: `HideAfterFinish`/`HideOnAbort` (set globally in `PromptConfigure`) were dead options on `6.0.0-Beta9` — the wizard's completed steps stayed on screen regardless of the setting. On `6.0.0-rc1` these options actually work, so the wizard now clears each step's UI after confirmation/abort, matching the behavior `PromptConfigure` always intended.

---

## [1.0.0-beta8] - 2026-08-04

### Fixed

- Migrating legacy ADRs (`adrplus migrate`) always left the `File title` header field empty — `ParseMigrationFileNameAsync` gated title extraction on a length value that the migration pattern parser intentionally never sets for the title field (position-only, no fixed length), so the guard was always false.
- Corrected numerous inaccuracies across `README.md`, `StepByStepGuide.md`, `MigrationGuide.md`, `PluginDevelopmentGuide.md`, `FAQ.md`, `CONTRIBUTING.md`, `SECURITY.md`, `NugetREADME.md`, and `AbstractionsREADME.txt` — including missing `--path` flags in examples, a stale default case-transform claim, an unparseable `abstractionsVersion` example, an undocumented `pluginallowlist` config key, and other drift between the docs and current behavior.

---

## [1.0.0-beta7] - 2026-08-04

### Added

- Plugin system: AdrPlus now dispatches ADR lifecycle events (create, approve, reject, revise, supersede, undo) to plugins implementing the `IAdrPlugin` contract — see the [Plugin Development Guide](PluginDevelopmentGuide.md).
- Plugins are discovered **host-globally** (`%UserProfile%/AdrPlus.Plugins/<name>/`, merged with whatever ships bundled with the AdrPlus install), not per repository — a repository only holds the `activeplugins`/`disableplugins` on/off switch in `adr-config.adrplus`.
- `sync` command to re-drive pending plugin dispatches, with `--backfill` to sweep every existing ADR and re-emit its current settled event. Pending state is per-repository (`./plugins-state/<name>/pending.json`), kept independent of the host-global plugin code.
- `plugins` command (`--list`/`--validate`) for plugin diagnostics, plus `--wizard` support to manage which plugins are active for a repository. `--list` also works without `--path`, reporting only what this host discovered (no active/inactive/missing status, since there's no repository to cross-reference). `--validate` never needs `--path` at all — it only checks structural load, host-wide.
- Per-repo plugin activation management via the `activeplugins`/`disableplugins` settings in `adr-config.adrplus`, plus non-interactive `adrplus plugins --activate <name>`/`--deactivate <name>` flags.
- `adrplus plugins --install <path-to-zip>`/`--uninstall <name>` to install/remove a plugin on the machine, without manually copying files — both also available interactively via `adrplus plugins --wizard`. Neither requires `--path`: installing/removing a plugin is machine-wide, independent of any repository.
- `AdrPlus.Plugins.AdrIndexer` — a reference plugin bundled with AdrPlus and discovered automatically, generating a linked ADR index table.
- `AdrPlus.Abstractions` (the `IAdrPlugin` plugin contract) is now published as its own NuGet package, decoupled from the CLI tool's own release/versioning — see [AbstractionsREADME.md](AbstractionsREADME.md). Released independently under `abstr-v*.*.*` tags.

---

## [1.0.0-beta5] - 2026-07-30

### Changed

- Updated `PromptPlus`/`PromptPlus.Hosting` dependencies.

---

## [1.0.0-beta4] - 2026-07-28

### Added

- 9 new UI languages (`de-DE`, `es-ES`, `fr-FR`, `it-IT`, `ja-JP`, `ko-KR`, `nl-BE`, `ru-RU`, `zh-CN`), matching the set already shipped by the `PromptPlus` dependency. The first-run wizard's language picker now lists all 11 languages explicitly (plus "Other" for any other valid culture code).
- ADR templates (all 7 methodology templates, in all 11 languages) are now selected based on the configured UI language, instead of only offering English/Portuguese variants.
- See [TRANSLATIONS.md](TRANSLATIONS.md) for per-language review status — the 9 new languages are machine-translated, pending native-speaker review.

---

## [1.0.0-beta3] - 2026-07-28

### Fixed

- Every repo-touching command (`approve`, `explore`, `init`, `migrate`, `new`, `reject`, `revise`, `supersede`, `undo-status`, `version`) crashed with `FileNotFoundException` on a fresh install/upgrade — they gated on a machine-global template file that only the interactive first-run wizard ever writes, even though each command already validates its own local `adr-config.adrplus`. The redundant guard was dropped; `init` now falls back to the in-memory default config when the global template is missing, and `migrate`'s pattern lookup tolerates its absence too.
- `init` always crashed on a genuinely fresh repository — the ADR folder was scanned for existing files before it was created. A missing ADR folder is now treated as zero files instead of throwing.
- `explore --file` crashed on a bare filename with no directory component — `Path.GetDirectoryName` returning empty was treated as a nonexistent directory instead of the current one.
- The NuGet badge in the README didn't show pre-release versions.

---

## [1.0.0-beta2] - 2026-07-28

### Changed

- Standardized color constant names in `PromptConsole.cs` for naming consistency.
- Removed the `wizard` argument from the AdrPlus launch profile (`launchSettings.json`) so it's no longer passed automatically on launch.

---

## [1.0.0-beta1] - 2026-07-27

### Added

- `--version`/`-v` flag to print the installed AdrPlus version.

### Changed

- Renamed the `review` command to `revise` (matches the "revision" terminology used elsewhere).
- Renamed the `explorer` command to `explore` (consistent with the verb-based naming of every other command).
- Renamed the internal `Review`/`Explorer` identifiers (classes, namespaces, resource keys, and the `explore` wizard's saved history keys) to `Revise`/`Explore`, matching the command names above. The saved wizard history for the `explore` command's report filename, show-or-create choice, and field selection resets once on first use after upgrading — everything else is unaffected.
- Fixed the `config --help` usage example, which showed the nonexistent `--migration` flag instead of `--migrate`.

### Fixed

- Every command (not just `--wizard`) crashed with `IOException: The handle is invalid` when run under redirected/piped output (CI, scripts, automation agents) — `PromptShowBanner` called `Console.Clear()` and drew the startup banner unconditionally before routing to any command handler. Now skipped when `Console.IsOutputRedirected`.
- The first-install flow silently launched an interactive wizard on the first command run in any repository without an existing `adr-config.adrplus`, even when the command was given non-interactive flags (e.g. `init --path .`). Now skipped when there's no real console attached (`Console.IsInputRedirected`/`IsOutputRedirected`), letting the command's own flag-driven logic run instead.
- `PromptWriteStartCommand`/`PromptWriteFinishedCommand` crashed with `"The ConsoleColor enum value was not defined on that enum"` under redirected output — `Color.Darkorange` has no legacy `ConsoleColor` equivalent. Now falls back to uncolored text only when there's no real console; interactive runs keep the original color.
- `adrplus --version` (and the welcome/migration banner) reported `1.0.0` instead of `1.0.0-beta1` — the version string was read from `AssemblyName.Version`, a numeric-only `Major.Minor.Build` that MSBuild truncates from the csproj's semver `<Version>`, silently dropping any pre-release suffix. Now reads `AssemblyInformationalVersion`, which preserves it.

### Removed

- Removed 8 unused CLI arguments left over from a previously removed configuration command: `--sequence`, `--createfolders`, and the `--template`/`--version`/`--revision`/`--scope`/`--items` variants that were never wired into any command.

---

## [0.6.3] - 2026-06-01

### Changed

- Refactored configuration validation to use the `IValidateConfig` interface.

---

## [0.6.2] - 2026-05-29

### Changed

- Refactored application startup: removed `HostBuilder` in favor of a manual application lifetime.

---

## [0.6.1] - 2026-05-27

### Fixed

- Improved ADR open logic; updated constructor documentation.

---

## [0.6.0] - 2026-05-25

### Added

- Configuration migration system.

### Changed

- Renamed the `withoutargs` configuration field.

---

## [0.5.0] - 2026-05-21

### Added

- `FlushOutput` on `IPromptConsole`, called at application exit points.

### Changed

- Simplified error handling: exception messages are always written to the prompt.
- ADR file search now uses non-padded sequence numbers.
- Adjusted ADR migration and supersede logic for consistency.

### Removed

- `Humanize` transformation for ADR titles/domains — raw values are used instead.

---

## [0.4.0] - 2026-05-21

### Added

- Configurable behavior for no-argument execution (`withoutargs`: `Help`, `Wizard`, `None`).

---

## [0.3.1] - 2026-05-20

### Added

- `migrate` command, with migration pattern support and a configuration wizard.
- `explorer` command for viewing and reporting on ADR files.
- First-time setup wizard for initial installation and configuration.

### Changed

- Replaced the `upgrade` command with an enhanced `init` command supporting reinitialization and configuration updates.
- Unified ADR repository folder configuration under `folderadr`.
- Refactored ADR header handling, filename convention, and parser logic.
- Normalized all resource strings.

### Fixed

- Bug in the `init` command and in `supersede`.
- Consolidated ADR header validation errors.

---

## [0.2.0] - 2026-04-23

### Added

- `upgrade` command for repository upgrades (superseding an earlier `repo` command).

---

## [0.1.1] - 2026-04-20

### Changed

- Refactored the ADR template system; added multi-template support.

---

## [0.1.0] - 2026-04-15

### Added

- Initial command set: `help`, `wizard`, `config`, `init`, `new`, `version`, `review`, `supersede`, `approve`, `reject`, `undo`.
- Multi-language support (`en-US` and `pt-BR`) for CLI messages and templates.
- Cross-platform support (Windows, macOS, Linux).
- Multi-target build: `net8.0`, `net9.0`, and `net10.0`.
