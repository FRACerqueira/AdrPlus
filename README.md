![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)

# AdrPlus

[![CI](https://github.com/FRACerqueira/AdrPlus/actions/workflows/ci.yml/badge.svg)](https://github.com/FRACerqueira/AdrPlus/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AdrPlus.svg?include_prereleases)](https://www.nuget.org/packages/AdrPlus)
[![NuGet Abstractions](https://img.shields.io/nuget/v/AdrPlus.Abstractions.svg?label=Abstractions&include_prereleases)](https://www.nuget.org/packages/AdrPlus.Abstractions)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AdrPlus.svg?label=AdrPlus)](https://www.nuget.org/packages/AdrPlus)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AdrPlus.Abstractions.svg?label=Abstractions)](https://www.nuget.org/packages/AdrPlus.Abstractions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com/)

> 🤖 **New:** manage your ADRs conversationally with the [**AdrPlus AI Assistant Plugin**](https://github.com/FRACerqueira/AdrPlus-IA-Plugin) — let Claude Code or GitHub Copilot create, approve, audit, and index ADRs for you. [Learn more ↓](#using-adrplus-with-ai-coding-assistants)

Many teams still document architectural decisions **inconsistently** (scattered Markdown files, no version flow, and hard-to-track status changes).

AdrPlus was created to **solve this problem with a practical CLI workflow that keeps ADRs standardized, traceable, and easy to evolve over time**.

**AdrPlus** is a cross-platform .NET command-line tool for managing [Architecture Decision Records (ADRs)](https://adr.github.io/) directly from your terminal.

It supports versioning, revision cycles, status workflows (approve / reject / undo), and an **interactive wizard** — all driven by a lightweight JSON configuration file.

![AdrPlus wizard demo](demoadr.gif)

---

## Table of Contents

- [Motivation and Benefits](#motivation-and-benefits)
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Migration Guide](MigrationGuide.md)
- [Step-by-Step Guide](StepByStepGuide.md)
- [Plugin Development Guide](PluginDevelopmentGuide.md)
- [Using AdrPlus with AI Coding Assistants](#using-adrplus-with-ai-coding-assistants)
- [Advanced Configuration (Optional)](#advanced-configuration-optional)
- [Individual Commands (without the wizard)](#individual-commands-without-the-wizard)
- [Commands](#commands)
- [Rules by ADR commands](#rules-by-adr-commands)
- [Suggested profiles](#suggested-settings-per-team-profile)
- [Configuration](#configuration)
- [Plugins](#plugins)
- [Settings and configuration across upgrades](#settings-and-configuration-across-upgrades)
- [Architecture Decisions of this Project](#architecture-decisions-of-this-project)
- [Contributing](#contributing)
- [Code of Conduct](#code-of-conduct)
- [Security](#security)
- [License](#license)
- [Frequently Asked Questions](FAQ.md)
---

## Motivation and Benefits

Using **AdrPlus** in an engineering repository helps you:

- 📚 Keep architectural decisions organized with a predictable structure
- 🔍 Improve traceability with version, revise, and supersede flows
- ⚡ Reduce manual effort when creating and updating ADR files
- 🛠️ Respect the repository's configuration for naming, structure, and ADR status for each team
- 🤝 Improve collaboration by making decision history visible to the whole team
- 🚀 Accelerate onboarding by exposing context behind technical choices

---

## Features

- 📝 **Create** new ADRs with auto-incremented sequential numbers
- 🔢 **Version** and **revise** existing ADRs (major version or revision bump)
- 🔄 **Supersede** an ADR by creating a successor with a new number
- ✅ **Approve** / ❌ **Reject** / ↩️ **Undo** ADR status changes
- 🧙 **Interactive wizard** for guided, step-by-step operations
- 🤖 **AI assistant integration** — manage ADRs conversationally via the official [AdrPlus AI Assistant Plugin](https://github.com/FRACerqueira/AdrPlus-IA-Plugin) for Claude Code and GitHub Copilot
- 🧩 **Plugin support** for integrations that react to ADR lifecycle events — see the [Plugin Development Guide](PluginDevelopmentGuide.md)
- 🔍 **Explorer** for viewing or **Generate reports** and managing ADR files in your repository
- ⚙️ **Config editor** for application, repository settings and migration of existing ADRs to the standardized format
- 📂 **Customizable ADR structure** with user-defined templates and naming conventions
- 🔄 **Migrate** existing ADRs to the standardized format
- 💾 **Preserve settings and configuration** across upgrades and reinitializations
- 🗂️ **Multiple ADR** model options for different project needs and for each team
- 🌍 Multi-language support (`en-US`, `pt-BR`, `de-DE`, `es-ES`, `fr-FR`, `it-IT`, `ja-JP`, `ko-KR`, `nl-BE`, `ru-RU`, `zh-CN`) for messages, UX, and ADR templates — see [TRANSLATIONS.md](TRANSLATIONS.md) for review status.
  - **ADR content can be written in any language!**
- 🖥️ Cross-platform (Windows, macOS, Linux)
---

## Requirements

### For running

- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) or later

`AdrPlus` can be used in repositories of **any language or framework** (C#, Java, Node.js, Python, Go, etc.), because it manages ADR files in Markdown and does not depend on your application stack.

### For building and packaging from source

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

---

## Installation

### Install from NuGet (Recommended for .NET developers)

The easiest way to install `AdrPlus` is directly from [NuGet.org](https://www.nuget.org/packages/AdrPlus):

```bash
dotnet tool install -g adrplus
```

To update to the latest version:

```bash
dotnet tool update -g adrplus
```

To uninstall:

```bash
dotnet tool uninstall -g adrplus
```

After installation, you can use `adrplus` from any terminal in any repository.

Check the installed version anytime with:

```bash
adrplus --version
```

### Build and install from source

If you prefer to build from the repository source code:

#### 1. Build and generate a local package

```bash
# From repository root
dotnet restore
dotnet build -c Release
dotnet pack -c Release -o ./nupkg
```

#### 2. Install from local package

```bash
# Install as global tool from local package folder
dotnet tool install -g adrplus --add-source ./nupkg

# If already installed, update from the same local source
dotnet tool update -g adrplus --add-source ./nupkg
```

---

## Quick Start

**AdrPlus automates the initial setup on first run!**

When you run any command for the first time (except `help`), an interactive wizard will automatically guide you through:
- Selecting your preferred language
- Configuring your editor to open ADR files
- Setting up your ADR repository structure and naming conventions
- Creating your ADR folder and configuration files

To get started, simply run without a command, and the setup wizard will appear:

```bash
# Just run without command and the first-time setup will run
adrplus

# Or any other command - the first-time wizard will run before it executes
adrplus init --wizard
```

This wizard only runs in a real interactive terminal. In CI, scripts, or an AI agent driving the CLI (standard input/output redirected), it's skipped automatically and AdrPlus falls back to its built-in defaults — a team can also pre-provision a `firstinstaller.adrplus` seed file so that first automated run starts from approved settings instead. See [Non-Interactive Setup](StepByStepGuide.md#non-interactive-setup-ci-scripts-ai-agents) in the Step-by-Step Guide.

After initial setup completes, you can use any command directly:

```bash
# Create a new ADR with the wizard
adrplus new --wizard

# Or explore and manage your ADRs
adrplus explore --path "path/to/repository" --file "path/to/report.md"
adrplus approve --file "path/to/adr/ADR0001.md"
```

> **Note**: If you have existing ADR files in a different format, see [Migration Guide](MigrationGuide.md) for detailed prerequisites and migration instructions before creating new ADRs with the tool.

For a detailed walkthrough, see the [Step-by-Step Guide](StepByStepGuide.md).

---

## Using AdrPlus with AI Coding Assistants

Prefer managing ADRs in plain language instead of typing commands yourself? The official [**AdrPlus AI Assistant Plugin**](https://github.com/FRACerqueira/AdrPlus-IA-Plugin) brings the same skill and agents to both [Claude Code](https://claude.com/claude-code) and GitHub Copilot:

- A skill (`manage-adrs`) that teaches Claude the full `adrplus` command surface, so it can create, approve, reject, version, revise, supersede, and configure ADRs without guessing at flags.
- An `adr-auditor` agent that audits an existing ADR repository (structural compliance, content completeness, supersede-chain and date-consistency checks, risk-calibrated status hygiene, and failure-visibility documentation) and produces a read-only report.
- An `adr-indexer` agent that turns `adrplus explore`'s report into a readable, grouped index page.
- An `adr-decision-check` agent that checks pending changes before a commit or PR (or on request) and recommends whether they need a new ADR or a version/revise/supersede of an existing one.

Requires `adrplus` v1.0.0 or later — earlier pre-releases (including beta1/beta2, which weren't safe to drive non-interactively) are no longer supported.

---

## Advanced Configuration (Optional)

**Note:** The initial setup wizard runs automatically on first use in an interactive terminal (see [Quick Start](#quick-start) above for the non-interactive/CI behavior). The commands below are optional if you want to reconfigure or adjust settings after the initial setup.

### Reconfigure Application Settings

You can modify application settings at any time:

```bash
# Edit language, editor preferences, and other application settings
adrplus config --application

# Edit the default template used for new ADRs
adrplus config --template

# Edit patterns for migrating existing ADRs (used with the migrate command)
adrplus config --migrate

# Edit repository-specific settings (ADR naming, statuses, structure)
adrplus config --repository
```

### Reinitialize or Update Repository Structure

The `init` command can be run multiple times to reinitialize or update your ADR repository structure:

```bash
# Initialize repository for the first time (or with wizard guidance)
adrplus init --wizard

# Reinitialize with specific configuration file (scriptable, no confirmation needed)
adrplus init --path "path/to/repository" --file "path/to/config"

# Reinitialize current directory
adrplus init --path "."
```

This is useful when:
- You want to recreate default configuration files
- You're adjusting naming conventions or patterns

> **Note**: Re-running `init --path <dir>` **without** `--file` on a directory that already has a
> config asks for confirmation before overwriting it. In a real interactive terminal you'll be
> prompted as usual; in a non-interactive context (CI, scripts, an AI agent) there's no one to
> confirm, so it fails cleanly with `Configuration file already exists at: <path>` instead of
> overwriting silently. To reinitialize non-interactively, pass `--file` pointing at the
> configuration you want — that always skips the confirmation, as shown above.

---

## Individual Commands (without the wizard)

You can also execute commands directly, one by one, without the wizard and without interactive prompts.

```bash
# Configure the tool (optional, you can edit the config file directly or use the config command later)
# --file reads its content as a SEED for this machine's shared default template (used by future
# `init` runs on this host) - it does not edit an existing repository's own config file in place.

    adrplus config --application --file "path/to/file-tool-config"
    adrplus config --template --file "path/to/file-template.md"
    adrplus config --repository --file "path/to/file-adr-config"
    adrplus config --migrate --file "path/to/file-ard-config"

# Launch the ADR file viewer explorer (--file is the path to write the generated report to)

    adrplus explore --path "path/to/repository" --file "path/to/report.md"

# Initialize ADR structure (if the first time you set up the repository)

    adrplus init --path "path/to/repository"

# Create a new ADR directly

    # the parameter --open is optional and depends on the configuration for opening files after creation/update
    adrplus new --title "Use PostgreSQL as primary database" --path "path/to/repository" --open

# Approve or reject a specific ADR file

    adrplus approve --file "./doc/adr/ADR0001V01-use-postgresql.md"
    adrplus reject --file "./doc/adr/ADR0002V01-legacy-cache.md"

# Undo last status change

    adrplus undo --file "./doc/adr/ADR0001V01-use-postgresql.md"

# Create supersede flows

    adrplus approve --file "./doc/adr/ADR0001V01-use-postgresql.md"
    # the parameter --open is optional and depends on the configuration for opening files after creation/update
    adrplus supersede --file "./doc/adr/ADR0001V01-use-postgresql.md" --open

# Create revise/version flows
# `revise` requires revision support enabled (`lenrevision` > 0 in `adr-config.adrplus`) —
# the default "Simple repository" profile below sets `lenrevision: 0`, which disables it.

    # the parameter --open is optional and depends on the configuration for opening files after creation/update
    adrplus revise --file "./doc/adr/ADR0001V01-use-postgresql.md" --open
    adrplus version --file "./doc/adr/ADR0001V01-use-postgresql.md" --open

# Re-drive pending plugin dispatches (safe to schedule via cron/CI); --backfill sweeps every
# existing ADR and is manual-only, never scheduled

    adrplus sync --path "path/to/repository"
    adrplus sync --path "path/to/repository" --backfill

# Check installed plugins, or manage which ones are active for this repository; --list with no
# --path reports only what this host discovered, without any repository's active/inactive status.
# --validate never needs --path either: it only checks structural load, host-wide

    adrplus plugins --list
    adrplus plugins --list --path "path/to/repository"
    adrplus plugins --validate
    adrplus plugins --activate "PluginName" --path "path/to/repository"
    adrplus plugins --deactivate "PluginName" --path "path/to/repository"

# Install or remove a plugin — host-wide, no --path: the zip must be named <name>-<version>.zip,
# matching its plugin.json; --force overwrites an existing install entirely (including plugin.json);
# neither touches any repository's activeplugins

    adrplus plugins --install "./PluginName-1.0.0.zip"
    adrplus plugins --uninstall "PluginName"

```

All six commands above that record a status change (`new`, `approve`, `reject`, `supersede`, `version`, `revise`)
also accept `--refdate <date>`, to record the change as having happened on a specific date instead of today —
useful when backfilling history or batch-registering decisions made earlier. `--refdate` can never be later than
today. On `approve`/`reject`/`supersede`/`version`/`revise` it also can't be earlier than the date already
recorded for that ADR (its `Created` date, or its most recent recorded update, whichever applies); `new` has no
lower bound, since a brand-new sequence number has no prior history to respect. `--wizard` mode enforces the same
range directly in the date picker, so an invalid date can't even be selected there.

Use `adrplus help <command>` to check the available parameters for each command.

---

## Commands

| Command | Description |
|---|---|
| `help`      | Display help information for all commands or a specific command |
| `wizard`    | Launch the interactive wizard for guided operations |
| `config`    | Application configuration editor,migrate repository,repository and default ADR template |
| `explore`   | Launch the file viewer explorer and report for the ADR repository |
| `migrate`   | Migrate existing ADRs to use the tool |
| `init`      | Initialize or reinitialize the ADR repository folder structure (can be run multiple times) |
| `new`       | Create a new ADR with an incremental number |
| `version`   | Create a new version of an  ADR (increment version) |
| `revise`    | Create a new revision of an ADR (increment revision) |
| `supersede` | Supersede an ADR by creating a successor with a new incremental number |
| `approve`   | Set an ADR status to *Accepted* |
| `reject`    | Set an ADR status to *Rejected* |
| `undo`      | Revert the last status change of an ADR |
| `sync`      | Re-drive pending plugin dispatches (`--backfill` sweeps every existing ADR and re-emits its current settled event) |
| `plugins`   | Diagnostics for installed plugins (`--list`/`--validate`); activate/deactivate a plugin (`--activate`/`--deactivate <name>`); install/remove one from a zip (`--install <path>`/`--uninstall <name>`) — every mode also available interactively via `--wizard` |

Run `adrplus help <command>` for detailed usage of any command.

### Rules by ADR commands

The rules below describe what must be true for a command to select its target successfully (especially in wizard mode).

> For file-based commands (`approve`, `reject`, `undo`, `version`, `revise`, `supersede`), the file must exist, be a valid ADR `.md`, be under the configured `folderadr`, and the repository config file must be valid.

| Command | Successful selection rules |
|---|---|
| `new` | `title` must be unique. `--scope`/`--domain` are optional free-text fields with no validation. |
| `approve` | ADR must be eligible: not already approved/rejected and for the same sequence number not superseded.|
| `reject` | ADR must be eligible: not already approved/rejected.|
| `undo` | ADR must be eligible: already approved/rejected and for the same sequence not a superseded and not proposed.|
| `version` | ADR must be eligible: latest(or last approved and last rejected) ADR for the same sequence number approved/rejected and not superseded. `--scope`/`--domain` are optional — omitted, the new version keeps the source ADR's current values; provided, it uses the given value instead.|
| `revise` | ADR must be eligible: latest(or last approved and last rejected) ADR for the same sequence number approved/rejected , not superseded and revision enabled. Scope/Domain always carry forward unchanged — `revise` has no `--scope`/`--domain`.|
| `supersede` | ADR must be eligible: already approved and not superseded. `--scope`/`--domain` are optional — omitted, the new ADR keeps the superseded ADR's current values; provided, it uses the given value instead.|

---

## Configuration

AdrPlus uses two configuration files:

- `adrplus.json`: application-level settings (language and command to open ADR).
- `adr-config.adrplus`: repository-level settings (ADR naming, template, statuses, and structure).

### `adrplus.json` example

You can edit the application configuration with:

```bash
adrplus config --application
```

```json
{
  "DefaultSettings": {
    "language": "en-US",
    "comandopenadr": "code {0}",
    "withoutargs": "Help",
    "pluginallowlist": [
      { "name": "AdrIndexer" }
    ]
  }
}
```

| Key | Description |
|-----|-------------|
|`language`| UI language/culture used by the tool (`en-US`, `pt-BR`, `de-DE`, `es-ES`, `fr-FR`, `it-IT`, `ja-JP`, `ko-KR`, `nl-BE`, `ru-RU`, `zh-CN`, or any other valid culture code). Defines the language for all prompts and messages displayed in the wizard and command outputs, and the language of the built-in ADR templates. |
|`comandopenadr`| Command to open an ADR file after creation/update when supported. See examples below. |
|`withoutargs`| Behavior when no arguments are provided (`Help`, `Wizard`, or `None`). Default is `Help`. |
|`pluginallowlist`| Optional, host-wide allowlist restricting which installed plugins may load, matched by `name` (case-insensitive). Omit or set to `null` to allow every installed plugin (default); an empty array blocks all plugins. Each entry also accepts a `hash` field, reserved for future enforcement and not yet checked. See [Plugins](#plugins). |

##### Examples for `comandopenadr`

- **VS Code**: `code {0}` — Opens the file in VS Code.
- **Visual Studio**: `devenv.exe {0}` — Opens the file in the associated application (Windows only).
- **JetBrains Rider**: `rider {0}` — Opens the file in Rider.
- **Sublime Text**: `subl {0}` — Opens the file in Sublime Text.
- **Vim**: `vim {0}` — Opens the file in Vim.
- **Nano**: `nano {0}` — Opens the file in Nano.
- **Disabled**: `""` (empty string) — Disables automatic opening of ADR files.

> **Note**: The command must be available as a global PATH variable in your system to work properly. Test it manually in your terminal before configuring it here.

##### Behavior when no arguments are provided (`withoutargs`)

The `withoutargs` setting determines how AdrPlus behaves when executed without any arguments or commands:

- **`Help`** (default): Displays the help information with available commands and options.
- **`Wizard`**: Launches the interactive wizard for guided operations (useful for agile experienced users).
- **`None`**: Requires the user to explicitly provide a command; if no command is given, an error message is shown.

Example configurations:

```json
{
  "withoutargs": "Help"    // Display help when no arguments provided
}
```

```json
{
  "withoutargs": "Wizard"  // Launch wizard when no arguments provided
}
```

```json
{
  "withoutargs": "None"    // Require explicit command
}
```


### `adr-config.adrplus` example

AdrPlus uses the `adr-config.adrplus` file to control repository behavior, ADR naming, template content, and status labels.

You can edit it with:

```bash
adrplus config --repository
```

```json
{
  "folderadr": "doc/adr",
  "migrationpattern": "...",
  "template": "...",
  "prefix": "ADR",
  "lenseq": 4,
  "lenversion": 2,
  "lenrevision": 0,
  "separator": "-",
  "casetransform": "PascalCase",
  "statusnew": "Proposed",
  "statusacc": "Accepted",
  "statusrej": "Rejected",
  "statussup": "Superseded",
  "headerdisclaimer": "Do not remove this comment, lines and table",
  "headertitlefile": "ADR",
  "headerversion": "Version",
  "headerrevision": "Revision",
  "headerscope": "Scope",
  "headerdomain": "Domain",
  "headertitlestatuscreated": "Created",
  "headertitlestatuschanged": "Changed",
  "headertitlestatussuperseded": "Superseded",
  "headertablefields": "Fields",
  "headertablevalues": "Values",
  "headermigrated": "Migrated",
  "activeplugins": [],
  "disableplugins": false
}
```

| Key | Description |
|-----|-------------|
| `folderadr` | Folder where ADR files are stored. |
| `migrationpattern` | Pattern used for migrating ADR files (generated by the tool). |
| `template` | Base Markdown template used when creating new ADR files (generated by the tool). |
| `prefix` | Prefix used in ADR titles/identifiers (for example: `ADR`). |
| `lenseq` | Number of digits for the sequential ADR number (for example: `4` => `0001`). |
| `lenversion` | Number of digits for major version formatting (for example: `2` => `01`). |
| `lenrevision` | Number of digits for revision formatting (for example: `2` => `01`; `0` disables revision numbering). |
| `separator` | Separator character used in generated file names (valid values: `-`, `_`, or `.`). |
| `casetransform` | Case style applied to generated name segments (for example: `PascalCase`, `CamelCase`, `SnakeCase`, or `KebabCase`). |
| `statusnew` | Label used for newly created ADRs. |
| `statusacc` | Label used for accepted ADRs. |
| `statusrej` | Label used for rejected ADRs. |
| `statussup` | Label used for superseded ADRs. |
| `headerdisclaimer` | Disclaimer header added to ADR template output. |
| `headertitlefile` | Header label for the ADR file name field in the header. |
| `headerversion` | Header label for ADR version field. |
| `headerrevision` | Header label for ADR revision field. |
| `headerscope` | Header label for ADR scope field. |
| `headerdomain` | Header label for ADR domain field. |
| `headertitlestatuscreated` | Header label for the "Created" status indicator. |
| `headertitlestatuschanged` | Header label for the "Changed" status indicator. |
| `headertitlestatussuperseded` | Header label for the "Superseded" status indicator. |
| `headertablefields` | Table header label for displaying field names in the ADR. |
| `headertablevalues` | Table header label for displaying field values in the ADR. |
| `headermigrated` | Header label for the "Migrated" indicator (used for ADRs migrated via the `migrate` command). |
| `activeplugins` | Names of the host-installed plugins (see [Plugins](#plugins)) expected to be active for this repository. Written automatically by `init` from whatever's installed on the machine at the time; edit it via `adrplus plugins --wizard`'s manage mode rather than by hand. A plugin installed but left off this list is treated as deliberately inactive (silently skipped); a name listed here with no matching installed plugin is reported as missing the next time a command dispatches. |
| `disableplugins` | Repository-wide kill switch. When `true`, no plugin ever dispatches for this repo, regardless of `activeplugins` — the ADR operation itself still completes normally. |

### Suggested settings per team profile

#### Understanding configuration concepts

Before selecting a team profile, understand these key concepts:

- **Scope and Domain**: Free-text fields on every ADR, shown only in the header table (not the filename), with no validation and no folder organization tied to them. Use them however your team finds useful — e.g. noting which area or team a decision belongs to — AdrPlus does not enforce or interpret their values.

- **Case Transform**: The style applied to the title portion of generated filenames:
  - `PascalCase`: `UsePostgreSqlAsDatabase`
  - `CamelCase`: `usePostgreSqlAsDatabase`
  - `SnakeCase`: `use_postgre_sql_as_database`
  - `KebabCase`: `use-postgre-sql-as-database` (default)

- **Separator**: The character separating different parts of the filename:
  - `-` (hyphen): `ADR0001V01-UsePostgreSql.md`
  - `_` (underscore): `ADR0001_UsePostgreSql.md`
  - `.` (period): `ADR0001V01.UsePostgreSql.md`

- **Version vs. Revision**:
  - **Version**: A major change to an ADR (e.g., `V01`, `V02`) that typically represents a significant decision update.
  - **Revision**: A minor change to an ADR (e.g., `R01`, `R02`) that represents clarifications or documentation improvements.

#### 1) Simple repository

The default shape: a flat `folderadr` directory, no scope/domain-based organization (there isn't one anymore — see [Scope and Domain](#understanding-configuration-concepts) above). Fits most projects, single-domain or not.

> `lenrevision: 0` disables the `revise` command entirely — attempting it under this profile fails with a
> configuration error. Set `lenrevision` to `1` or higher (see [Product team with frequent revisions](#2-product-team-with-frequent-revisions) below) if you plan to use `revise`.

```json
{
  "separator": "-",
  "casetransform": "PascalCase",
  "lenversion": 2,
  "lenrevision": 0
}
```

**Example filenames generated**:
- `doc/adr/ADR0001V01-UsePostgreSql.md`
- `doc/adr/ADR0002V01-AdoptReactFramework.md`

#### 2) Product team with frequent revisions

Keep revision metadata visible and standardized. Useful for teams that frequently update ADR documentation or maintain multiple versions.

```json
{
  "lenseq": 4,
  "lenversion": 2,
  "lenrevision": 2
}
```

**Example filenames generated**:
- `doc/adr/ADR0001V01R01-DecisionTitle.md` (created)
- `doc/adr/ADR0001V01R02-DecisionTitle.md` (after revision - revision increments, version stays)
- `doc/adr/ADR0001V02R01-DecisionTitle.md` (after version bump - version increments, revision resets to 01)
- `doc/adr/ADR0002V01R01-DecisionTitle--0001.md` (after superseded bump)

> Tip: start with one profile, run `adrplus init`, create a test ADR with `adrplus new`, and adjust the config iteratively.

---

## Plugins

Not every ADR-related need belongs in the core CLI. Teams often want to react to ADR lifecycle events — regenerate an index, notify a channel, sync to an external system, enforce a custom policy — without AdrPlus growing built-in support for every possible target. The plugin system exists for exactly that: any command that changes an ADR's state (create, approve, reject, revise, supersede, undo) dispatches lifecycle events that plugins can subscribe to and react to independently of the core tool.

Writing a plugin means implementing the `IAdrPlugin` interface from the `AdrPlus.Abstractions` package — see the [Plugin Development Guide](PluginDevelopmentGuide.md) for the full contract and event lifecycle.

You don't need to write one to get value from the plugin system: **`AdrPlus.Plugins.AdrIndexer` already ships bundled with AdrPlus** and is discovered automatically — no install step needed. It rebuilds a linked table of your ADRs (ADR, title, version, status) every time an ADR changes, and doubles as a working reference implementation if you want to build your own.

Plugins are installed **once per machine, host-wide** — not per repository. Installing a third-party or hand-built plugin doesn't require manually copying files: `adrplus plugins --install "./PluginName-1.0.0.zip"` unpacks a zip named `<name>-<version>.zip` into `%UserProfile%/AdrPlus.Plugins/<name>/`, making it available to every repository on that machine; `--uninstall <name>` removes it from the machine entirely. A newly installed plugin never dispatches on its own for any given repository — activate it explicitly per repo with `adrplus plugins --activate <name> --path "path/to/repository"` (or deactivate one with `--deactivate <name> --path "path/to/repository"`), the same trust checkpoint the interactive wizard's manage mode already enforces.

Before activation even comes into play, an optional `pluginallowlist` in `adrplus.json` can restrict which installed plugin names are permitted to load at all on that host — see the [Configuration](#configuration) section above. `adrplus plugins --list` reports each plugin's allowlist status alongside its active/inactive state.

> Removing AdrPlus completely? `dotnet tool uninstall -g adrplus` doesn't clean up `%UserProfile%/AdrPlus.Plugins/` (installed plugins) or `%UserProfile%/AdrPlus.History/` (settings carried across upgrades) — dotnet global tools have no uninstall hook, so both are left behind by design. Delete them by hand if you want a fully clean removal.

---

## Settings and configuration across upgrades

When you upgrade AdrPlus to a new version, all your settings and configurations are automatically preserved:

```bash
# Update to the latest version
dotnet tool update -g adrplus
```

Your configuration persists automatically:
- ✅ **Application settings** (`adrplus.json`): Language, editor preferences, and interface behavior remain unchanged
- ✅ **Repository configuration** (`adr-config.adrplus`): ADR naming patterns and folder structure are maintained

No manual reconfiguration is needed after upgrading — simply update the tool and continue using it as before.

> A breaking change occasionally requires more than an in-place update — check `CHANGELOG.md`'s latest
> section for a "Breaking change - action required" note before running `dotnet tool update`. For
> example, upgrading from any version older than `1.0.0` requires uninstalling and reinstalling rather
> than updating in place (see the changelog for why).

---

## Architecture Decisions of this Project

AdrPlus is used to manage its own architecture decisions — see the [ADR Index](doc/adr/indexadrs.md) (auto-generated by the `AdrIndexer` plugin) for the list of decisions recorded for this repository.

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting pull requests or issues.

---

## Code of Conduct

Please read and follow [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

---

## Security

To report a vulnerability, please read [SECURITY.md](SECURITY.md).

---

## License

This project is licensed under the [MIT License](LICENSE).

---