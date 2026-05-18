[![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)](logo)

# AdrPlus

[![CI](https://github.com/FRACerqueira/AdrPlus/actions/workflows/ci.yml/badge.svg)](https://github.com/FRACerqueira/AdrPlus/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AdrPlus.svg)](https://www.nuget.org/packages/AdrPlus)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AdrPlus.svg)](https://www.nuget.org/packages/AdrPlus)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4)](https://dotnet.microsoft.com)

Many teams still document architectural decisions **inconsistently** (scattered Markdown files, no revision flow, and hard-to-track status changes).

AdrPlus was created to **solve this problem with a practical CLI workflow that keeps ADRs standardized, traceable, and easy to evolve over time**.

**AdrPlus** is a cross-platform .NET command-line tool for managing [Architecture Decision Records (ADRs)](https://adr.github.io/) directly from your terminal. 

It supports versioning, revision cycles, status workflows (approve / reject / undo), and an **interactive wizard** — all driven by a lightweight JSON configuration file.


---

## Table of Contents

- [Motivation and Benefits](#motivation-and-benefits)
- [Features](#features)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Migration Guide](MigrationGuide.md)  
- [Step-by-Step Guide](StepByStepGuide.md)  
- [Commands](#commands)
- [Rules for adr commands](#rules-by-adr-commands)
- [Suggested profiles](#suggested-settings-per-team-profile)
- [Configuration](#configuration)
- [Contributing](#contributing)
- [Code of Conduct](#code-of-conduct)
- [Security](#security)
- [License](#license)
- [Frequently Asked Questions](FAQ.md)
---

## Motivation and Benefits

Using **AdrPlus** in an engineering repository helps you:

- 📚 Keep architectural decisions organized with a predictable structure
- 🔍 Improve traceability with version, review, and supersede flows
- ⚡ Reduce manual effort when creating and updating ADR files
- 🛠️ Respect the repository's configuration for naming, structure, and ADR status for each team
- 🤝 Improve collaboration by making decision history visible to the whole team
- 🚀 Accelerate onboarding by exposing context behind technical choices

---

## Features

- 📝 **Create** new ADRs with auto-incremented sequential numbers
- 🔢 **Version** and **review** existing ADRs (major version or revision bump)
- 🔄 **Supersede** an ADR by creating a successor with a new number
- ✅ **Approve** / ❌ **Reject** / ↩️ **Undo** ADR status changes
- 🧙 **Interactive wizard** for guided, step-by-step operations
- 🔍 **Explorer** for viewing or **Generate reports** and managing ADR files in your repository
- ⚙️ **Config editor** for application ,repository settings and migration of existing ADRs to the standardized format
- 📂 **Customizable ADR structure** with user-defined templates and naming conventions
- 🔄 **Migrate** existing ADRs to the standardized format
- 🗂️ **Multiple ADR** model options for different project needs and for each team
- 🌍 Multi-language support (`en-US`, `pt-BR`) for messages and UX
  - **ADR content can be written in any language!**
- 🖥️ Cross-platform (Windows, macOS, Linux)
---

## Requirements

### For running

- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) or later

`AdrPlus` can be used in repositories of **any language or framework** (C#, Java, Node.js, Python, Go, etc.), because it manages ADR files in Markdown and does not depend on your application stack.

### For building and packaging from source

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
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

```bash
# 1. Run the command wizard to configure and use the tool
adrplus wizard
```
Using this for the first time? Follow the step-by-step guide to set up and create your first ADR:

[Step-by-Step Guide](StepByStepGuide.md)

> **Note**: If you have existing ADR files in a different format, see [Migration Guide](MigrationGuide.md) for detailed prerequisites and migration instructions before creating new ADRs with the tool.

---

## Manual Setup with the Wizard

```bash
# 1. Configure the tool (optional, you can edit the config file directly or use the config command later)

    # Configure application settings (optional: language, prompts, defaults)
    adrplus config --application

    # Configure the base template used for new ADRs (optional: default template is madr)
    adrplus config --template

    # Configure pattern migrated ADRs (optional: used when migrating existing ADRs) 
    adrplus config --migrate

    # Configure repository settings (ADR naming, template, statuses, and structure)
    adrplus config --repository

# 2. Initialize a new ADR repository in the current directory
    
    adrplus init --wizard

# 3. Create your first ADR

    adrplus new --wizard

# 4. Approve it

    adrplus approve --wizard

# 5. List available commands

    adrplus help
```

---

## Individual Commands (without the wizard)

You can also execute commands directly, one by one, without the wizard and without interactive prompts.

```bash
# Configure the tool (optional, you can edit the config file directly or use the config command later)

    adrplus config --application --file "path/to/file-tool-config"
    adrplus config --template --file "path/to/file-template.md"
    # any mode can be used for repository config, the important part is to point to the correct file
    adrplus config --repository --file "path/to/file-adr-config"
    adrplus config --migrate --file "path/to/file-ard-config"

# Launch the ADR file viewer explorer

    adrplus explorer --path "path/to/repository"

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

# Create review/version flows

    # the parameter --open is optional and depends on the configuration for opening files after creation/update
    adrplus review --file "./doc/adr/ADR0001V01-use-postgresql.md" --open
    adrplus version --file "./doc/adr/ADR0001V01-use-postgresql.md" --open

```

Use `adrplus help <command>` to check the available parameters for each command.

---

## Commands

| Command | Description |
|---|---|
| `help`      | Display help information for all commands or a specific command |
| `wizard`    | Launch the interactive wizard for guided operations |
| `config`    | Application configuration editor,migrate repository,repository and default ADR template |
| `explorer`  | Launch the file viewer explorer and report for the ADR repository |
| `migrate`   | Migrate existing ADRs to use the tool |
| `init`      | Initialize or update the ADR repository folder structure |
| `new`       | Create a new ADR with an incremental number |
| `version`   | Create a new version of an  ADR (increment version) |
| `review`    | Create a new revision of an ADR (increment revision) |
| `supersede` | Supersede an ADR by creating a successor with a new incremental number |
| `approve`   | Set an ADR status to *Accepted* |
| `reject`    | Set an ADR status to *Rejected* |
| `undo`      | Revert the last status change of an ADR |

Run `adrplus help <command>` for detailed usage of any command.

### Rules by ADR commands

The rules below describe what must be true for a command to select its target successfully (especially in wizard mode).

> For file-based commands (`approve`, `reject`, `undo`, `version`, `review`, `supersede`), the file must exist, be a valid ADR `.md`, be under the configured `FolderRepo`, and the repository config file must be valid.

| Command | Successful selection rules |
|---|---|
| `new` | `title + domain` must be unique. When scope is enabled , `--scope` must be valid; `--domain` is required unless scope is listed in `skipdomain`. |
| `approve` | ADR must be eligible: not already approved/rejected and for the same sequence number not superseded.|
| `reject` | ADR must be eligible: not already approved/rejected.|
| `undo` | ADR must be eligible: already approved/rejected and for the same sequence not a superseded and not proposed.|
| `version` | ADR must be eligible: latest(or last approved and last rejected) ADR for the same sequence number approved/rejected and not superseded.|
| `review` | ADR must be eligible: latest(or last approved and last rejected) ADR for the same sequence number approved/rejected , not superseded and revision enabled.|
| `supersede` | ADR must be eligible: already approved and not superseded.|

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
  "Language": "en-US",
  "ComandOpenAdr": "code {0}"
}
```

| Key | Description |
|-----|-------------|
|`Language`| UI language/culture used by the tool (`en-US`, `pt-BR`). Defines the language for all prompts and messages displayed in the wizard and command outputs. |
|`ComandOpenAdr`| Command to open an ADR file after creation/update when supported. See examples below. |

##### Examples for `ComandOpenAdr`

- **VS Code**: `code {0}` — Opens the file in VS Code.
- **Visual Studio**: `devenv.exe {0}` — Opens the file in the associated application (Windows only).
- **JetBrains Rider**: `rider {0}` — Opens the file in Rider.
- **Sublime Text**: `subl {0}` — Opens the file in Sublime Text.
- **Vim**: `vim {0}` — Opens the file in Vim.
- **Nano**: `nano {0}` — Opens the file in Nano.
- **Disabled**: `""` (empty string) — Disables automatic opening of ADR files.

> **Note**: The command must be available as a global PATH variable in your system to work properly. Test it manually in your terminal before configuring it here.


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
  "lenscope": 0,
  "separator": "-",
  "casetransform": "PascalCase",
  "statusnew": "Proposed",
  "statusacc": "Accepted",
  "statusrej": "Rejected",
  "statussup": "Superseded",
  "scopes": "",
  "folderbyscope": false,
  "skipdomain": "",
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
  "headermigrated": "Migrated"
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
| `lenscope` | Maximum scope segment length used in generated names (when scope is enabled, value > 0). |
| `separator` | Separator character used in generated file names (valid values: `-`, `_`, or `.`). |
| `casetransform` | Case style applied to generated name segments (for example: `PascalCase`, `CamelCase`, `SnakeCase`, or `KebabCase`). |
| `statusnew` | Label used for newly created ADRs. |
| `statusacc` | Label used for accepted ADRs. |
| `statusrej` | Label used for rejected ADRs. |
| `statussup` | Label used for superseded ADRs. |
| `scopes` | Semicolon-separated list of allowed scopes for organizing ADRs (for example: `Enterprise;Domain;Project`; can be empty when `lenscope = 0`). |
| `folderbyscope` | If `true`, ADR files are grouped by scope folders; if `false`, all ADRs remain in the flat `folderadr` directory. |
| `skipdomain` | Semicolon-separated list of scope names for which the domain segment should be omitted from the generated filename (must be a subset of `scopes`). |
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

### Suggested settings per team profile

#### Understanding configuration concepts

Before selecting a team profile, understand these key concepts:

- **Scopes**: Define organizational boundaries for your ADRs (e.g., "Enterprise", "Backend", "Frontend"). Scopes help organize decisions by domain or team responsibility. When enabled (`lenscope > 0`), the scope appears in the ADR filename.

- **Folder by Scope**: When enabled, ADRs are organized into separate folders for each scope (e.g., `doc/adr/enterprise/`, `doc/adr/backend/`). When disabled, all ADRs stay in a flat structure under the configured `folderadr` folder.

- **Skip Domain**: Some scopes may not need a domain segment in the filename. For example, a "Corporate" scope might skip the domain to keep filenames shorter. You can list multiple scopes separated by semicolons.

- **Case Transform**: The style applied to the title portion of generated filenames:
  - `PascalCase`: `UsePostgreSQLAsDatabase`
  - `CamelCase`: `usePostgreSQLAsDatabase`
  - `SnakeCase`: `use_postgresql_as_database`
  - `KebabCase`: `use-postgresql-as-database` (default)

- **Separator**: The character separating different parts of the filename:
  - `-` (hyphen): `ADR0001V01-UsePostgreSQL.md`
  - `_` (underscore): `ADR0001_UsePostgreSQL.md`
  - `.` (period): `ADR0001V01.UsePostgreSQL.md`

- **Version vs. Revision**: 
  - **Version**: A major change to an ADR (e.g., `V01`, `V02`) that typically represents a significant decision update.
  - **Revision**: A minor change to an ADR (e.g., `R01`, `R02`) that represents clarifications or documentation improvements.

#### 1) Monorepo (multiple apps/domains or enterprise architecture)

Use scopes and folder grouping to keep ADRs organized by area. Each team or domain maintains its own ADR sequence.

```json
{
  "scopes": "Enterprise;Project;Backend;Frontend;Mobile;Data",
  "skipdomain": "Enterprise",
  "folderbyscope": true,
  "lenscope": 1,
  "separator": "-",
  "casetransform": "PascalCase",
  "lenversion": 2,
  "lenrevision": 0
}
```

**Example filenames generated**:
- `doc/adr/Enterprise/ADR0001V01E-UnifyAuthenticationStrategy.md`
- `doc/adr/Backend/ADR0001V01B-UsePostgreSQ@MyBackEndScope.md`
- `doc/adr/Frontend/ADR0001V01F-AdoptReactFramework@MyFrontEndScope.md`

#### 2) Simple repository

Use a simple flat structure with no scope folder split. Ideal for smaller projects or single-domain repositories.

```json
{
  "scopes": "",
  "folderbyscope": false,
  "lenscope": 0,
  "separator": "-",
  "casetransform": "PascalCase",
  "lenversion": 2,
  "lenrevision": 0
}
```

**Example filenames generated**:
- `doc/adr/ADR0001V01-UsePostgreSQL.md`
- `doc/adr/ADR0002V01-AdoptReactFramework.md`

#### 3) Product team with frequent revisions

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
- `doc/adr/ADR0001V02R01-DecisionTitle.md` (after revision)
- `doc/adr/ADR0001V03R02-DecisionTitle.md` (after version bump)
- `doc/adr/ADR0002V01R01-DecisionTitle--0001.md` (after superseded bump)

#### 4) Enterprise with department scopes

Organize ADRs by department with custom headers and folder structure.

```json
{
  "scopes": "Infrastructure;Database;Platform;Security",
  "skipdomain": "Platform",
  "folderbyscope": true,
  "lenscope": 3,
  "separator": "-",
  "casetransform": "PascalCase",
  "lenversion": 2,
  "lenrevision": 0
}
```

**Example filenames generated**:
- `doc/adr/Infrastructure/ADR0001V01Inf-UseDockerContainers.md` (Infrastructure)
- `doc/adr/Database/ADR0001V01Dat-AdoptPostgresql.md` (Database)


> Tip: start with one profile, run `adrplus init`, create a test ADR with `adrplus new`, and adjust the config iteratively.

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