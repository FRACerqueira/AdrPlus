[![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)](logo)

# Changelog

All notable changes to **AdrPlus** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [0.1.0] ~ [0.3.1] — 2026-04-17 ~ 2026-05-20 (Preliminary versions)


### Added


| Command | Description |
|---|---|
| `help`      | Display help information for all commands or a specific command |
| `wizard`    | Launch the interactive wizard for guided operations |
| `config`    | Application configuration editor,migrate repository,repository and default ADR template |
| `explorer`  | Launch the file viewer explorer and report for the ADR repository |
| `migrate`   | Migrate existing ADRs to use the tool |
| `init`      | Initialize or reinitialize the ADR repository folder structure (can be run multiple times) |
| `new`       | Create a new ADR with an incremental number |
| `version`   | Create a new version of an  ADR (increment version) |
| `review`    | Create a new revision of an ADR (increment revision) |
| `supersede` | Supersede an ADR by creating a successor with a new incremental number |
| `approve`   | Set an ADR status to *Accepted* |
| `reject`    | Set an ADR status to *Rejected* |
| `undo`      | Revert the last status change of an ADR |

- Multi-language support (`en-US` and `pt-BR`) for CLI messages and templates.
- Cross-platform support (Windows, macOS, Linux).
- Multi-target build: `net8.0`, `net9.0`, and `net10.0`.
