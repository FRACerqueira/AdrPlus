# Changelog

All notable changes to **AdrPlus** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [0.1.0] ~ [0.1.1] — 2026-04-17 ~ 2026-04-19

### Added

- `init` command — initialise ADR repository folder structure.
- `new` command — create a new ADR with an auto-incremented sequential number.
- `version` command — bump the major version of an existing ADR (same number).
- `review` command — create a new revision of an existing ADR (same number and version).
- `supersede` command — mark an ADR as superseded and create a successor.
- `approve` command — mark an ADR as approved.
- `reject` command — mark an ADR as rejected.
- `undo` command — revert the last status change of an ADR.
- `config` command — interactive editor for application and repository settings.
- `wizard` command — interactive guided wizard for all operations.
- `help` command — display contextual help for any command.
- Multi-language support (`en-US` and `pt-BR`) for CLI messages and templates.
- Cross-platform support (Windows, macOS, Linux).
- Multi-target build: `net8.0`, `net9.0`, and `net10.0`.
