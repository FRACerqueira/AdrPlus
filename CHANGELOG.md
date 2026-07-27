![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)

# Changelog

All notable changes to **AdrPlus** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added

- `--version`/`-v` flag to print the installed AdrPlus version.

### Changed

- Renamed the `review` command to `revise` (matches the "revision" terminology used elsewhere).
- Renamed the `explorer` command to `explore` (consistent with the verb-based naming of every other command).
- Fixed the `config --help` usage example, which showed the nonexistent `--migration` flag instead of `--migrate`.

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
