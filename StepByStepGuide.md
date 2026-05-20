[![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)](logo)
# Step-by-Step Guide to Set Up and Create Your First ADR

Welcome! This guide will walk you through installing **AdrPlus** and creating your first **Architecture Decision Record (ADR)**.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Initial Configuration](#initial-configuration)
4. [Initialize Your Repository](#initialize-your-repository)
5. [Create Your First ADR](#create-your-first-adr)
6. [Approve Your ADR](#approve-your-adr)
7. [Explore Additional Commands](#explore-additional-commands)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

Before you begin, ensure you have:

- **.NET 8 Runtime** or later installed on your system
  - Download from: [https://dotnet.microsoft.com/download/dotnet](https://dotnet.microsoft.com/download/dotnet)
  - Verify installation: `dotnet --version`

- **Git repository** initialized in your project folder (optional, but recommended)
  - If not yet initialized: `git init`

- A **terminal/command prompt** ready to use (PowerShell, Command Prompt, Bash, etc.)

---

## Installation

### Option 1: Install from NuGet (Recommended)

This is the easiest way to get started:

```bash
dotnet tool install -g adrplus
```

### Option 2: Build and Install from Source

If you prefer to build from the repository:

```bash
# Clone the repository
git clone https://github.com/FRACerqueira/AdrPlus.git
cd AdrPlus

# Build and package
dotnet restore
dotnet build -c Release
dotnet pack -c Release -o ./nupkg

# Install from local package
dotnet tool install -g adrplus --add-source ./nupkg
```

### Verify Installation

Confirm the installation was successful:

```bash
adrplus help
```

You should see the help menu with available commands.

---

## Initial Configuration

**AdrPlus automates the initial setup on first run!**

When you execute any AdrPlus command for the first time (except `help`), an interactive wizard will automatically guide you through the setup process. This ensures your application and repository are configured correctly before you start using the tool.

### Automatic First-Time Setup

The first time you run an AdrPlus command, the setup wizard will automatically:

1. **Select your preferred language** (`en-US`, `pt-BR`, or other)
2. **Configure your editor** (VS Code, Visual Studio, Rider, or custom command)
3. **Set your ADR repository folder** (default: `doc/adr`)
4. **Configure ADR naming conventions** (prefix, numbering, versioning, case style)
5. **Configure migration pattern 
6. **Create the configuration files**:
   - `adrplus.json` (application settings)
   - `adr-config.adrplus` (repository settings)

**Example - Just run any command:**

```bash
# Just run without command and the initial setup wizard starts automatically
adrplus new --wizard

# Or any other command - the first-time wizard will run before it executes
adrplus explorer --path "."
```

### What Happens Next

After the initial setup completes:
- Your configuration files are created
- You're ready to create, manage, and approve ADRs
- You can use the wizard (`--wizard`) with commands for guided operations, or run commands directly with arguments

### Optional: Manual Configuration Later

If you need to adjust your settings after the initial setup, you can reconfigure at any time:

```bash
# Edit application settings (language, editor preferences)
adrplus config --application

# Edit repository settings (ADR naming, structure, status labels)
adrplus config --repository

# Edit migration patterns (for migrating existing ADRs)
adrplus config --migrate

# Edit the default ADR template
adrplus config --template
```

### Manual Setup (Advanced / Optional)

If you prefer to configure manually instead of using the automatic wizard:

#### Step 1: Configure Application Settings

```bash
adrplus config --application
```

This creates/edits `adrplus.json` with:
- **Language**: UI language for prompts and messages (`en-US`, `pt-BR`, etc.)
- **ComandOpenAdr**: Command to open files after creation (e.g., `code {0}` for VS Code)

Example `adrplus.json`:
```json
{
  "Language": "en-US",
  "ComandOpenAdr": "code {0}"
}
```

#### Step 2: Configure Repository Settings

```bash
adrplus config --repository
```

This creates/edits `adr-config.adrplus` with ADR naming conventions.

**For a simple repository (recommended for beginners):**

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

**Configuration keys (relevant) explained:**

| Key | Meaning | Example |
|-----|---------|---------|
| `folderadr` | Folder where ADR files are stored. | `doc/adr` |
| `migrationpattern` | Pattern used for migrating ADR files (generated by the tool). | N/A (auto-generated) |
| `template` | Base Markdown template used when creating new ADR files (generated by the tool). | N/A (auto-generated) |
| `prefix` | Prefix for ADR identifiers | `ADR` → `ADR0001` |
| `lenseq` | Digits for sequential number | `4` → `0001`, `0002`, etc. |
| `lenversion` | Digits for major version (0 disables) | `2` → `V01`, `V02`, etc. |
| `lenrevision` | Digits for revision (0 = disabled) | `0` (disabled) or `2` → `R01`, `R02` |
| `lenscope` | Number of characters for scope abbreviation (0 disables) | `1` → `B`, `F`, etc. |
| `separator` | Character between name parts | `-`, `_`, or `.` |
| `casetransform` | Case style for names | `PascalCase`, `CamelCase`, `SnakeCase`, `KebabCase` |
| `scopes` | Semicolon-separated list of allowed scopes | `Backend;Frontend;Data` |
| `folderbyscope` | Create separate folders per scope | `true` or `false` |
| `skipdomain` | Scopes that skip domain in filenames | `data;platform` |
| `statusnew` | Label for new ADRs | `Proposed` |
| `statusacc` | Label for approved ADRs | `Accepted` |
| `statusrej` | Label for rejected ADRs | `Rejected` |
| `statussup` | Label for superseded ADRs | `Superseded` |
| `headerdisclaimer` | Disclaimer text in ADR header | N/A (template metadata) |
| `headertitlefile` | Header label for ADR file name | `ADR` |
| `headerscope` | Header label for scope field | `Scope` |
| `headerdomain` | Header label for domain field | `Domain` |
| `headertitlestatuscreated` | Header label for "Created" status | `Created` |
| `headertitlestatuschanged` | Header label for "Changed" status | `Changed` |
| `headertitlestatussuperseded` | Header label for "Superseded" status | `Superseded` |
| `headertablefields` | Table header for field names | `Field` |
| `headertablevalues` | Table header for field values | `Value` |
| `headermigrated` | Header label for "Migrated" indicator | `Migrated` |

#### Understanding key configuration concepts

Before initializing your repository, let's understand some important concepts:

- **Scopes**: Define organizational boundaries for your ADRs (e.g., "backend", "frontend", "data"). When enabled (`lenscope > 0`), scopes help organize decisions by domain or team responsibility.

- **Folder by Scope**: When `folderbyscope` is `true`, ADRs are organized into separate folders for each scope. When `false`, all ADRs stay in a flat structure under the `folderadr` folder.

- **Skip Domain**: Some scopes may not need a domain segment in the filename. For example, a "data" scope might skip the domain to keep filenames shorter. List multiple scopes separated by semicolons.

- **Case Transform**: The style applied to the title portion of generated filenames:
  - `PascalCase`: `UsePostgreSQLAsDatabase`
  - `CamelCase`: `usePostgreSQLAsDatabase`
  - `SnakeCase`: `use_postgresql_as_database`
  - `KebabCase`: `use-postgresql-as-database`

- **Separator**: The character separating different parts of the filename:
  - `-` (hyphen): Recommended, most readable
  - `_` (underscore): Alternative style
  - `.` (period): Alternative style

- **Version vs. Revision**: 
  - **Version**: A major change to an ADR (e.g., `V01`, `V02`) that represents a significant decision update.
  - **Revision**: A minor change to an ADR (e.g., `R01`, `R02`) that represents clarifications or documentation improvements.

---

## Initialize Your Repository

### ⚠️ Important: Migrate Existing ADRs First (if applicable)

**If your repository already contains ADR files in a different format than the one you just configured**, you MUST execute the migration process **before creating your first ADR with AdrPlus**.

**Why?** 
- Migration transforms existing ADRs into the AdrPlus format
- This must be done before creating any new ADRs with the tool
- Mixing manually-created ADRs in different formats with tool-created ADRs will cause inconsistencies

**To migrate existing ADRs:**

1. Run the migration configuration:
   ```bash
   adrplus config --migrate
   ```

2. Execute the init or update process using one of these methods:
- **Interactive wizard (recommended)**:
    ```bash
    adrplus init --wizard
    ```
- **Direct path**:
    ```bash
    adrplus init --path "./path/to/existing/adrs" --file "./path/to/config"
    ```
- This process:
    - Creates the folder specified in `folderadr` (e.g., `doc/adr`)
    - Creates the `adr-config.adrplus` configuration file in the repository root
    - Prepares your repository for ADR management

- **What gets created:**
    ```
    your-project/
    ├── doc/
    │   └── adr/                    # ADR storage folder
    └── adr-config.adrplus          # Repository configuration
    ```

3. Execute the migration process using one of these methods:
   - **Interactive wizard (recommended)**:
     ```bash
     adrplus migrate --wizard
     ```
   - **Direct path**:
     ```bash
     adrplus migrate --path "./path/to/existing/adrs"
     ```

4. For detailed migration instructions, see: [Migration Guide](MigrationGuide.md)

**After migration is complete**, proceed with the repository initialization below.

---

## Create Your First ADR

Now you're ready to create your first ADR!

### Option 1: Interactive Creation (Recommended)

```bash
adrplus new --wizard
```

The wizard will prompt you for:
1. **Title**: A clear, concise decision title
   - Example: "Use PostgreSQL as primary database"
2. **Domain/Scope**: Category or module (if enabled)
   - Example: "backend", "data", etc.

The tool will then:
- Generate a unique number (e.g., `0001`)
- Create the file: `doc/adr/ADR0001V01-UsePostgresqlAsPrimaryDatabase.md`
- Open the file in your configured editor (if set)

### Option 2: Direct Creation from Command Line

If you prefer to skip prompts:

```bash
adrplus new --title "Use PostgreSQL as primary database"
```

### What Your First ADR Looks Like

AdrPlus automatically creates a Markdown file with a template:

```markdown
<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values|
|--|--|
|File title|Use PostgreSQL as Primary Database|
|Version|01|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-05-06)|
|Changed||
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# [Brief title of the decision]

## Deciders

* Deciders: [list everyone involved in the decision] <!-- optional -->

Technical Story: [description | ticket/issue URL] <!-- optional -->

## Context and Problem Statement

[Describe the context and problem statement, e.g., in free form using two to three sentences. You may want to articulate the problem in form of a question.]

## Decision Drivers <!-- optional -->

* [driver 1, e.g., a force, facing concern, …]
* [driver 2, e.g., a force, facing concern, …]
* … <!-- numbers of drivers can vary -->

## Considered Options

* [option 1]
* [option 2]
* [option 3]
* … <!-- numbers of options can vary -->

## Decision Outcome

Chosen option: "[option 1]", because [justification. e.g., only option, which meets k.o. criterion decision driver | which resolves force force | … | comes out best (see below)].

### Positive Consequences <!-- optional -->

* [e.g., improvement of quality attribute satisfaction, follow-up decisions required, …]
* …

### Negative Consequences <!-- optional -->

* [e.g., compromising quality attribute, follow-up decisions required, …]
* …

## Pros and Cons of the Options <!-- optional -->

### [option 1]

[example | description | pointer to more information | …] <!-- optional -->

* Good, because [argument a]
* Good, because [argument b]
* Bad, because [argument c]
* … <!-- numbers of pros and cons can vary -->

### [option 2]

[example | description | pointer to more information | …] <!-- optional -->

* Good, because [argument a]
* Good, because [argument b]
* Bad, because [argument c]
* … <!-- numbers of pros and cons can vary -->

### [option 3]

[example | description | pointer to more information | …] <!-- optional -->

* Good, because [argument a]
* Good, because [argument b]
* Bad, because [argument c]
* … <!-- numbers of pros and cons can vary -->

## Links <!-- optional -->

* [Link type] [Link to ADR] <!-- example: Refined by [ADR-0005](0005-example.md) -->
* … <!-- numbers of links can vary -->
```

### Edit Your ADR

Open the created ADR file and fill in the sections:

1. **Context**: Why was this decision needed? What problem are you solving?
2. **Decision**: What exactly did you decide to do?
3. **Consequences**: What are the impacts, benefits, and risks?
4. **Alternatives Considered**: What other options did you evaluate?

Example completed ADR:

```markdown
<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values|
|--|--|
|File title|Use PostgreSQL as Primary Database|
|Version|01|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-05-06)|
|Changed||
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Use PostgreSQL as Primary Database

## Context

Our application needs a reliable, scalable relational database. We're evaluating options between PostgreSQL, MySQL, and AWS RDS Aurora.

## Decision

We have decided to use PostgreSQL as our primary database because:
- Excellent stability and performance
- Rich feature set (JSONB, full-text search, extensions)
- Strong community support
- Cost-effective

## Consequences

**Benefits:**
- Robust SQL compliance
- Advanced features for complex queries
- Good scaling options

**Risks:**
- Team needs to acquire PostgreSQL expertise
- Migration from existing database may take time

## Alternatives Considered

- **MySQL**: Simpler, but fewer advanced features
- **AWS RDS Aurora**: Fully managed but higher cost
```

---

## Approve Your ADR

Once you've finished writing your ADR, approve it:

```bash
adrplus approve --wizard
```

The wizard will:
1. Show a list of pending ADRs
2. Ask you to select which ADR to approve
3. Update the status from `Proposed` to `Accepted`

The approved ADR file will now show:

```markdown
<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values|
|--|--|
|File title|Use PostgreSQL as Primary Database|
|Version|01|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-05-06)|
|Changed|Accepted (2026-05-07)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
```

### Direct Approval (Without Wizard)

If you know the file path:

```bash
adrplus approve --file "./doc/adr/ADR0001V01-UsePostgresqlAsPrimaryDatabase.md"
```

---

## Explore Additional Commands

Now that you've created your first ADR, you can explore other features:

### Explorer - View and Manage ADR Repository

Browse and explore all ADR files in your repository with an interactive file viewer:

```bash
adrplus explorer --wizard
```

#### What the Explorer Does

The Explorer command provides:

- **File Browsing**: Navigate through all ADR files in your repository with an intuitive interface
- **ADR Overview**: View key information about each ADR
  - Choose which fields to include in your report such as status, version, revision, created date, changed date, and more
- **Search and Filter**: Easily find and filter ADRs ([F4 key]) by name, status, or other criteria to locate specific decisions
- **View Details**: Open and inspect ADR content directly from the explorer without switching applications
- **Generate Reports**: Create customizable reports with selected fields from your ADRs
  - Choose which fields to include in your report such as status, version, revision, created date, changed date, and more
  - Export reports in Markdown formats for analysis, stakeholder communication, and documentation
  - Analyze trends and patterns in your architectural decisions

### Reject an ADR

If you decide an ADR is not suitable:

```bash
adrplus reject --wizard
adrplus reject --file "./doc/adr/ADR0002V01-SomeDecision.md"
```

### Undo Status Change

Revert the last status change:

```bash
adrplus undo --wizard
adrplus undo --file "./doc/adr/ADR0001V01-UsePostgresqlAsPrimaryDatabase.md"
```

### Version an ADR (Major Change)

Create a new version when making significant updates:

```bash
adrplus version --wizard
adrplus version --file "./doc/adr/ADR0001V01-UsePostgresqlAsPrimaryDatabase.md"
```

Creates: `ADR0001V02-UsePostgresqlAsPrimaryDatabase.md`

### Review an ADR (Minor Change)

Create a revision for minor updates:

```bash
adrplus review --wizard
adrplus review --file "./doc/adr/ADR0001V01-UsePostgresqlAsPrimaryDatabase.md"
```

Creates: `ADR0001V01R01-UsePostgresqlAsPrimaryDatabase.md`

### Supersede an ADR (Replace with New One)

When a decision is replaced by a new one, supersede it:

```bash
adrplus supersede --wizard
adrplus supersede --file "./doc/adr/ADR0001V01-UsePostgresqlAsPrimaryDatabase.md"
```

This creates a new ADR (e.g., `ADR0002`) and marks the old one as `Superseded`.

```bash
./doc/adr/ADR0002V01-UsePostgresqlAsPrimaryDatabase--0001.md"
```

### View Help

For detailed help on any command:

```bash
adrplus help <command>
```

Examples:
```bash
adrplus help new
adrplus help approve
adrplus help supersede
```

---

### Troubleshooting Upgrade

#### Issue: "Revision is already set"

**Problem:** You're trying to enable revisions when they're already enabled.

**Solution:** 
- This is a protection mechanism. If you need to change revision format, you must manually edit `adr-config.adrplus`
- Or start fresh with a new repository

#### Issue: "Scope is already set"

**Problem:** You're trying to add scopes when they're already configured.

**Solution:**
- Scopes can only be set once. To change scopes, manually edit `adr-config.adrplus`
- Or initialize a new ADR repository with the desired scopes

#### Issue: "Version value must be greater than current"

**Problem:** You specified a version digit count that's not greater than the current setting.

**Solution:**
- Example: If current is `2`, you can only upgrade to `2` or `3`
- To downgrade, manually edit `adr-config.adrplus`

### Next: Commit Your Changes

After upgrading, always commit your updated configuration:

```bash
git add adr-config.adrplus
git commit -m "chore: upgrade ADR repository settings - add scope support"
```

---

## Troubleshooting

### Issue: Command not found: `adrplus`

**Solution:**
- Ensure .NET 8+ is installed: `dotnet --version`
- Verify installation: `dotnet tool list -g`
- Reinstall if needed: `dotnet tool uninstall -g adrplus && dotnet tool install -g adrplus`

### Issue: ADR folder not created

**Solution:**
- Make sure you've run `adrplus init`
- Check that `folderrepo` path in configuration is correct
- Verify folder permissions

### Issue: Configuration file not found

**Solution:**
- Run `adrplus config --repository` to create the configuration
- Ensure you're in the correct repository directory
- Check that `adr-config.adrplus` exists in the project root

### Issue: Cannot create ADR with special characters in title

**Solution:**
- AdrPlus uses `PascalCase` by default for file naming
- Simplify your title to use only letters and numbers
- Example: Instead of "Use PostgreSQL + Redis", use "Use PostgreSQL and Redis"

### Issue: File already exists error

**Solution:**
- Check if an ADR with the same number already exists
- Use a different title or let AdrPlus auto-increment the number
- Run `adrplus new` without `--title` to use the wizard

---

## Next Steps

Congratulations! You've successfully:
- ✅ Installed AdrPlus
- ✅ Configured your repository
- ✅ Created your first ADR
- ✅ Approved your ADR
- ✅ Learned how to upgrade your repository settings

### Recommendations

1. **Create more ADRs** for other important architectural decisions
2. **Commit to version control**: `git add doc/adr && git commit -m "docs: Add initial ADRs"`
3. **Plan for growth**: Review the [Upgrade Settings](#upgrade-settings) section to see if scopes would help organize your ADRs
4. **Share with your team**: Make ADRs part of your project documentation
5. **Review regularly**: Keep ADRs updated as your architecture evolves
6. **Check the FAQ**: See [FAQ.md](FAQ.md) for common questions

### Useful Resources

- **AdrPlus Repository**: [https://github.com/FRACerqueira/AdrPlus](https://github.com/FRACerqueira/AdrPlus)
- **ADR Concept**: [https://adr.github.io/](https://adr.github.io/)
- **NuGet Package**: [https://www.nuget.org/packages/AdrPlus](https://www.nuget.org/packages/AdrPlus)

---

## Support

- Open an issue on GitHub: [https://github.com/FRACerqueira/AdrPlus/issues](https://github.com/FRACerqueira/AdrPlus/issues)
- Check existing documentation in the README
- Review the FAQ for quick answers

Happy documenting! 🚀
