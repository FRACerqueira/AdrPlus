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
8. [Upgrade Settings](#upgrade-settings)
9. [Troubleshooting](#troubleshooting)

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

AdrPlus uses a wizard to guide you through initial setup. This is the **recommended approach** for first-time users.

### Quick Setup with the Wizard

Run the interactive wizard:

```bash
adrplus wizard
```

The wizard will guide you through:
1. **Application settings** (language, editor preferences, folder location)
2. **Repository settings** (ADR naming convention, versioning, status labels)
3. **Repository initialization**
4. **Creating your first ADR**

### Manual Setup (Optional)

If you prefer to configure manually instead of using the wizard:

#### Step 1: Configure Application Settings

```bash
adrplus config --application
```

This creates/edits `adrplus.json` with:
- **Language**: UI language (`en-US`, `pt-BR`, etc.)
- **FolderRepo**: Where to store ADRs (default: `doc/adr`)
- **OpenAdr**: Command to open files after creation (e.g., `code {0}` for VS Code)

Example `adrplus.json`:
```json
{
  "DefaultSettings": {
    "Language": "en-US",
    "YesValue": "",
    "NoValue": "",
    "OpenAdr": "code {0}"
  }
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
  "folderadr": "doc/adr","
  "template": "## Context\r\n\r\nDescribe the context and the problem to be solved.\r\n\r\n## Decision\r\n\r\nExplain the decision made.\r\n\r\n## Consequences\r\n\r\nList the impacts, benefits, and possible risks.\r\n\r\n## Alternatives Considered\r\n\r\n- Alternative 1 (Pros/Cons)\r\n- Alternative 2 (Pros/Cons)",
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
  "headerdisclaimer": "(Do not remove this template. It is required and must remain unchanged to ensure documentation consistency)",
  "headerstatus": "Status",
  "headerversion": "Version",
  "headerrevision": "Revision"
}
```

**Configuration keys (relevant) explained:**

| Key | Meaning | Example |
|-----|---------|---------|
| `folderadr` | Folder where ADR files are stored. | `doc/adr` |
| `template` | Base Markdown template used when creating new ADR files (generated automatically; not editable). | N/A (auto-generated) |
| `prefix` | Prefix for ADR identifiers | `ADR` → `ADR-0001` |
| `lenseq` | Digits for sequential number | `4` → `0001`, `0002`, etc. |
| `lenversion` | Digits for major version | `2` → `01`, `02`, etc. |
| `lenrevision` | Digits for revision (0 = disabled) | `0` (disabled) or `2` → `01` |
| `separator` | Character between name parts | `-` |
| `casetransform` | Case style for names | `PascalCase` |
| `statusnew` | Label for new ADRs | `Proposed` |
| `statusacc` | Label for approved ADRs | `Accepted` |
| `statusrej` | Label for rejected ADRs | `Rejected` |
| `statussup` | Label for superseded ADRs | `Superseded` |

---

## Initialize Your Repository

Now initialize the ADR repository structure in your project:

```bash
adrplus init
```

This command:
- Creates the folder specified in `folderrepo` (e.g., `doc/adr`)
- Creates the `adr-config.adrplus` configuration file in the repository root
- Prepares your repository for ADR management

**What gets created:**
```
your-project/
├── doc/
│   └── adr/                    # ADR storage folder
└── adr-config.adrplus          # Repository configuration
```

---

## Create Your First ADR

Now you're ready to create your first ADR!

### Option 1: Interactive Creation (Recommended)

```bash
adrplus new
```

The wizard will prompt you for:
1. **Title**: A clear, concise decision title
   - Example: "Use PostgreSQL as primary database"
2. **Domain/Scope**: Category or module (if enabled)
   - Example: "backend", "data", etc.

The tool will then:
- Generate a unique number (e.g., `0001`)
- Create the file: `doc/adr/ADR-0001-UsePostgresqlAsPrimaryDatabase.md`
- Open the file in your configured editor (if set)

### Option 2: Direct Creation from Command Line

If you prefer to skip prompts:

```bash
adrplus new --title "Use PostgreSQL as primary database"
```

### What Your First ADR Looks Like

AdrPlus automatically creates a Markdown file with a template:

```markdown
###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation)
##### Version: 01
##### Revision: -
##### -
##### Status
- Proposed (YYYY-MM-DD)
- \-
- \-
# Use PostgreSQL as Primary Database
---
## Context

Describe the context and the problem to be solved.

## Decision

Explain the decision made.

## Consequences

List the impacts, benefits, and possible risks.

## Alternatives Considered

- Alternative 1 (Pros/Cons)
- Alternative 2 (Pros/Cons)
```

### Edit Your ADR

Open the created ADR file and fill in the sections:

1. **Context**: Why was this decision needed? What problem are you solving?
2. **Decision**: What exactly did you decide to do?
3. **Consequences**: What are the impacts, benefits, and risks?
4. **Alternatives Considered**: What other options did you evaluate?

Example completed ADR:

```markdown
###### (Do not remove this template. It is used and must remain unchanged to ensure consistency in documentation)
##### Version: 01
##### Revision: -
##### -
##### Status
- Proposed (YYYY-MM-DD)
- \-
- \-
# Use PostgreSQL as Primary Database
---
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
adrplus approve
```

The wizard will:
1. Show a list of pending ADRs
2. Ask you to select which ADR to approve
3. Update the status from `Proposed` to `Accepted`

The approved ADR file will now show:

```markdown
Status: Accepted
```

### Direct Approval (Without Wizard)

If you know the file path:

```bash
adrplus approve --file "./doc/adr/ADR-0001-UsePostgresqlAsPrimaryDatabase.md"
```

---

## Explore Additional Commands

Now that you've created your first ADR, you can explore other features:

### Reject an ADR

If you decide an ADR is not suitable:

```bash
adrplus reject --file "./doc/adr/ADR-0002-SomeDecision.md"
```

### Undo Status Change

Revert the last status change:

```bash
adrplus undo --file "./doc/adr/ADR-0001-UsePostgresqlAsPrimaryDatabase.md"
```

### Version an ADR (Major Change)

Create a new version when making significant updates:

```bash
adrplus version --file "./doc/adr/ADR-0001-UsePostgresqlAsPrimaryDatabase.md"
```

Creates: `ADR-0001-UsePostgresqlAsPrimaryDatabase-V02.md`

### Review an ADR (Minor Change)

Create a revision for minor updates:

```bash
adrplus review --file "./doc/adr/ADR-0001-UsePostgresqlAsPrimaryDatabase.md"
```

Creates: `ADR-0001-UsePostgresqlAsPrimaryDatabase-R01.md`

### Supersede an ADR (Replace with New One)

When a decision is replaced by a new one, supersede it:

```bash
adrplus supersede --file "./doc/adr/ADR-0001-UsePostgresqlAsPrimaryDatabase.md"
```

This creates a new ADR (e.g., `ADR-0002`) and marks the old one as `Superseded`.

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

## Upgrade Settings

As your project evolves and your needs change, you may need to upgrade your repository configuration to add new features or modify existing settings. The `upgrade` command allows you to evolve your ADR repository structure without losing existing ADRs.

### When to Use Upgrade

Use the `upgrade` command when you need to:
- **Increase version number digits** (e.g., from 1 digit to 2 digits)
- **Enable revision numbering** (if not already enabled)
- **Add scope/domain support** to organize ADRs by area
- **Change the ADR template** for new ADRs
- **Create scope folders** to organize ADRs by team/module

### Upgrade with the Wizard (Recommended)

The easiest way to upgrade your repository is with the interactive wizard:

```bash
adrplus upgrade --wizard
```

The wizard will guide you through:
1. Selecting which settings to upgrade
2. Configuring the new parameters
3. Applying the changes to your repository

### Manual Upgrade Commands

If you prefer to upgrade specific settings directly, use these commands:

#### Upgrade Template

Change the default template used for new ADRs:

```bash
adrplus upgrade --template --path "doc/adr" --file "path/to/new-template.md"
```

Or use the default template configured in the application:

```bash
adrplus upgrade --template --path "doc/adr"
```

**What it does:**
- Updates the ADR template in `adr-config.adrplus`
- Applies to all **new** ADRs created after the upgrade
- Existing ADRs remain unchanged

#### Upgrade Version Numbering

Increase the number of digits used for version numbers:

```bash
adrplus upgrade --version 3 --path "doc/adr"
```

**Parameters:**
- `--version <value>`: Number of digits (2 or 3)
- Must be greater than or equal to current setting
- Example: Upgrade from `01` (2 digit) to `001` (3 digits)

**What it does:**
- Updates the version format in the configuration
- Applies to all **new** versions created after the upgrade
- Existing versioned ADRs are unaffected

#### Enable Revision Numbering

Add revision support if your repository doesn't have it yet:

```bash
adrplus upgrade --revision 2 --path "doc/adr"
```

**Parameters:**
- `--revision <value>`: Number of digits (1, 2, or 3)
- Only works if revisions are not already enabled (LenRevision = 0)
- Example: Enable revisions with 2-digit formatting (`01`, `02`, etc.)

**What it does:**
- Enables revision tracking for ADRs
- Allows you to create minor revisions with `adrplus review`
- Applies to all **new** reviews created after the upgrade

**Example - Before and After:**

Before upgrade:
```
ADR-0001-UsePostgresql.md
```

After enabling revisions:
```
ADR-0001-UsePostgresql-R01.md     (first revision)
ADR-0001-UsePostgresql-R02.md     (second revision)
```

#### Add Scope Support

Organize ADRs by scope/domain (e.g., backend, frontend, data):

```bash
adrplus upgrade --scope 1 --path "doc/adr" --items "backend;frontend;data;infrastructure"
```

**Parameters:**
- `--scope <value>`: Number of characters used in scope abbreviation (1-5)
- `--items "list;of;scopes"`: Semicolon-separated list of scope names
- `--createfolders`: (Optional) Create separate folders for each scope
- Append `*` to mark a scope as "skip domain" (will be omitted from file names)

**What it does:**
- Enables scope/domain support in your repository
- Creates a configuration for organizing ADRs by team/area
- Optionally creates physical folders by scope

**Example - Scopes with Folders:**

```bash
adrplus upgrade --scope 1 --path "doc/adr" --items "backend;frontend;data*;infra" --createfolders
```

This creates:
```
doc/
└── adr/
    ├── backend/
    ├── frontend/
    ├── data/              (marked with * = skip domain)
    ├── infra/
    └── (new ADRs go in respective folders)
```

ADR files created after this upgrade:
```
backend/ADR-0002-UseRedisCache-Backend.md
frontend/ADR-0003-UseVueJs-Frontend.md
data/ADR-0004-UseElasticsearch.md         (no scope suffix - skipped)
infra/ADR-0005-UseDockerContainers-Infra.md
```

### Complete Upgrade Example

Here's a realistic scenario: You start with a simple repository and want to add scopes and revision support:

**Initial configuration:**
```json
{
  "lenversion": 2,
  "lenrevision": 0,
  "lenscope": 0,
  "scopes": ""
}
```

**Step 1: Enable revisions**

```bash
adrplus upgrade --revision 2 --path "doc/adr"
```

**Step 2: Add scopes with folders**

```bash
adrplus upgrade --scope 1 --path "doc/adr" --items "backend;frontend;data*" --createfolders
```

**Result configuration:**
```json
{
  "lenversion": 2,
  "lenrevision": 2,          ← Revision support enabled
  "lenscope": 1,              ← Scope support added
  "scopes": "backend;frontend;data",
  "skipDomain": "data",       ← Data scope won't appear in file names
  "folderbyscope": true       ← Folders created by scope
}
```

**Folder structure after upgrade:**
```
doc/
└── adr/
    ├── backend/
    ├── frontend/
    └── data/
```

### Limitations and Important Notes

⚠️ **Important:**

1. **Cannot downgrade** - You can only increase version/revision digits, not decrease them
2. **One-time settings** - Revision and scope support can only be enabled once
3. **Existing ADRs unaffected** - Upgrades apply to new ADRs created after the change
4. **Backup first** - Always commit your changes to version control before upgrading: `git add . && git commit -m "docs: backup before ADR upgrade"`

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
