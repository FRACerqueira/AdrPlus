![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)

# Changelog

All notable changes to **AdrPlus** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [1.0.0-beta8] - 2026-08-04

### Fixed

- Migrating legacy ADRs (`adrplus migrate`) always left the `File title` header field empty — `ParseMigrationFileNameAsync` gated title extraction on a length value that the migration pattern parser intentionally never sets for the title field (position-only, no fixed length), so the guard was always false.
- Corrected numerous inaccuracies across `README.md`, `StepByStepGuide.md`, `MigrationGuide.md`, `PluginDevelopmentGuide.md`, `FAQ.md`, `CONTRIBUTING.md`, `SECURITY.md`, `NugetREADME.md`, and `AbstractionsREADME.txt` — including missing `--path` flags in examples, a stale default case-transform claim, an unparseable `abstractionsVersion` example, an undocumented `pluginallowlist` config key, and other drift between the docs and current behavior.

---

## [1.0.0-beta6] - 2026-08-04

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
