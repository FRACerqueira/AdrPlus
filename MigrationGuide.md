[![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)](logo)
# Migration Guide: Converting Existing ADRs to AdrPlus Format

Welcome! This guide will walk you through migrating your existing Architecture Decision Records (ADRs) to use the **AdrPlus** tool format.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [What Gets Migrated](#what-gets-migrated)
3. [Before You Start](#before-you-start)
4. [Migration Methods](#migration-methods)
   - [Interactive Wizard Mode](#interactive-wizard-mode)
   - [Direct Path Mode](#direct-path-mode)
5. [Migration Process](#migration-process)
6. [Verifying the Migration](#verifying-the-migration)
7. [Troubleshooting](#troubleshooting)
8. [Reverting Changes](#reverting-changes)

---

## Prerequisites

Before migrating your ADRs, ensure you have:

✅ **AdrPlus installed**
   - Install with: `dotnet tool install -g adrplus`
   - Verify: `adrplus help`

✅ **Application configuration set up**
   - Run: `adrplus config --application` to configure language and editor preferences

✅ **Repository configuration already set up**
   - Run: `adrplus config --repository` to configure your ADR naming conventions
   - Your configuration file exists at the repository root

✅ **Migration configuration set up** ⚠️ **REQUIRED**
   - Run: `adrplus config --migrate` to configure migration source settings
   - This must be done BEFORE attempting any migration operation
   - This step configures where the tool will look for ADRs to migrate

✅ **Repository already initialized with AdrPlus**
   - Run: `adrplus init` (if not already done)
   - This creates the `adr-config.adrplus` configuration file

✅ **No ADRs created by AdrPlus tool yet** ⚠️ **CRITICAL**
   - Migration can ONLY be executed if no ADRs have been created using `adrplus new`
   - If you have already created ADRs with the tool, migration cannot proceed
   - Migration is designed for repositories with only existing, manually-created ADR files

✅ **Existing ADR files in your repository**
   - You have `.md` files that need to be migrated
   - Files should be in a consistent location (e.g., `doc/adr/`)
   - These should be manually-created ADRs (not created by AdrPlus tool)

✅ **Git repository initialized** (highly recommended)
   - Initialize with: `git init` (if not already done)
   - Commit current state before migration: `git add . && git commit -m "backup: before ADR migration"`

---

## What Gets Migrated

The migration process adds an **AdrPlus-compliant header** to your existing ADR files.

### Before Migration

```markdown
# Use PostgreSQL as Primary Database

## Context

Our application needs a reliable, scalable relational database...

## Decision

We have decided to use PostgreSQL...

## Consequences

**Benefits:**
- Robust SQL compliance
- Advanced features

**Risks:**
- Team needs PostgreSQL expertise
```

### After Migration

```markdown
<!-- Do not remove this comment, lines and table (1-12) -->
|Fields|Values Migrated <!-- Migrated -->|
|--|--|
|File title|Use PostgreSQL as Primary Database|
|Version||
|Revision||
|Scope||
|Domain||
|Created||
|Changed||
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Use PostgreSQL as Primary Database

## Context

Our application needs a reliable, scalable relational database...

## Decision

We have decided to use PostgreSQL...

## Consequences

**Benefits:**
- Robust SQL compliance
- Advanced features

**Risks:**
- Team needs PostgreSQL expertise
```

> **Note**: The migration adds metadata without removing or modifying your actual ADR content (Context, Decision, Consequences, Alternatives).

### What Gets Added

- **File Title**: Extracted from filename and added to header
- **Version**: Empty by default 
- **Revision**: Empty by default
- **Scope/Domain**: Empty by default
- **Created**: Empty by default
- **Migrated Flag**: Marked in the comment header to indicate this was migrated
- **Header Table**: Displays metadata in a structured format using Fields | Values Migrated

---

## Before You Start

### ⚠️ Critical Prerequisites

Before starting migration, **you MUST**:

1. **Configure migration settings**:
   ```bash
   adrplus config --migrate
   ```
   This command must be executed before any migration operation. It configures how AdrPlus will discover and process your existing ADRs.

2. **Ensure NO ADRs have been created with AdrPlus tool**:
   - Migration can ONLY run when your repository contains zero ADRs created by `adrplus new`
   - If you've already created ADRs using the tool, you cannot migrate
   - Migration is a one-time operation for repositories with only manually-created ADRs
   - Once migration is complete, you can create new ADRs with `adrplus new`

**Why this requirement?**
- Migration transforms your existing ADR structure into AdrPlus format
- Tool-created ADRs already have the correct format and metadata
- Mixing manually-created and tool-created ADRs during migration would cause conflicts

### ⚠️ Important Checklist

- [x] **Verify migration was configured**: Run `adrplus config --migrate` first
  ```bash
  adrplus config --migrate  # Configure migration before proceeding
  ```

- [x] **Backup your repository**: Commit everything to Git
  ```bash
  git add .
  git commit -m "backup: before ADR migration"
  ```

- [x] **Verify configuration is correct**
  ```bash
  adrplus config --repository  # Review settings
  ```

- [x] **Confirm no tool-created ADRs exist**: Check that all existing ADRs are manually created, not via `adrplus new`

- [x] **Ensure all ADR files follow migration patterns convention**: 
  - Invalid files will be skipped during migration

- [x] **Close IDE if files are open**: Migration modifies files; avoid file locking issues

---

## Migration Methods

AdrPlus provides two ways to migrate your ADRs:

### 1. Interactive Wizard Mode (Recommended for First-Time Users)

**Guided experience with preview and confirmation**

#### Step 1: Start the Migration Wizard

```bash
adrplus migrate --wizard
```

#### Step 2: Select the Drive (if multiple drives present)

If your system has multiple drives, you'll be prompted to select one:

```
Select a logical drive to start browsing:
1) C
2) D
3) E

Enter your choice: 1
```

#### Step 3: Select the Repository Folder

Navigate to your repository root where `adr-config.adrplus` exists:

```
Select the target repository folder:
Current path: C:\
  [+] projects/
  [+] documents/
  [+] MyAdrRepo/  ← Select this one

Select folder: MyAdrRepo
```

#### Step 4: Review ADRs to Migrate

The wizard displays all ADRs found for migration with a preview

#### Step 5: Confirm Migration

Review the migration summary and confirm

#### Migration Complete (samples files)

```
Migrated: doc/adr/0001UsePostgreSQL.md
Migrated: doc/adr/0002UseReactFramework.md
```

---

### 2. Direct Path Mode (Quick Migration)

**Migrate all eligible ADRs in a directory without prompts**

#### Example Usage

```bash
adrplus migrate --path "C:\projects\MyAdrRepo"
```

or on Linux/Mac:

```bash
adrplus migrate --path "./doc/adr"
```

#### Behavior

- Automatically scans the repository for ADR files
- Migrates all ADRs that need migration (header invalid or missing)
- Skips already-migrated ADRs
- Displays success message for each migrated file

## Migration Process

### What Happens During Migration

1. **File Discovery**
   - Scans the repository for `.md` files matching your ADR naming convention
   - Validates filename format based on `adr-config.adrplus` settings
   - Identifies which files need migration (header invalid or missing)

2. **Header Generation**
   - Extracts ADR metadata from:
     - Filename (title)
   - Generates a formatted header with all metadata

3. **File Update**
   - Reads the original ADR content 
   - Prepends the new AdrPlus header
   - Writes the combined content back to the file
   - Original content remains unchanged

4. **Logging**
   - Each migrated file is logged
   - Success messages are displayed

### Files NOT Migrated

The migration will skip files that:

- **Already have a valid header** (already migrated)
  - Identified by the presence of the AdrPlus comment header and metadata table

- **Don't match the naming convention**
  - Example: If not found sequence+title e.g: file is named `DECISION-001.md`

- **Are invalid ADR files**
  - Corrupted or malformed Markdown

---

## Verifying the Migration

### Step 1: Check the Migrated Files

Open a migrated ADR file to verify:

### Step 2: Verify Header Format

Confirm the header has been added with the correct metadata:

```markdown
<!-- Do not remove this comment, lines and table (1-12) -->
|Fields|Values Migrated <!-- Migrated -->|
|--|--|
|File title|Use PostgreSQL as Primary Database|
|Version||
|Revision||
|Scope||
|Domain||
|Created||
|Changed||
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
```

### Step 3: Verify Content Integrity

Ensure your original content is intact:

### Step 4: Commit to Git

After verifying, commit the migration:

## Troubleshooting

### Issue: "Migration configuration not found"

**Problem**: Migration command fails because migration settings haven't been configured.

**Solution**: 
- Run migration configuration first:
  ```bash
  adrplus config --migrate
  ```
- This must be done before any migration operation
- Follow the prompts to configure migration source settings
- After configuration is complete, retry your migration command

---

### Issue: "Cannot migrate: existing tool-created ADRs detected"

**Problem**: You already have ADRs created with `adrplus new` command, blocking migration.

**Causes & Why**:
- Migration is designed for repositories with only manually-created ADRs
- Tool-created ADRs already have the correct AdrPlus format and metadata
- Mixing both types during migration would cause conflicts and data loss

**Solutions**:
- [x] **If you haven't created many tool ADRs**: Delete them and retry migration
 
- [x] **If you have valuable tool-created ADRs**: Create a new repository for migration
  - Migrate existing ADRs in a separate branch or repository
  - Merge migrated ADRs back with your tool-created ones after migration completes
  - This requires careful reconciliation

**Best Practice**: 
Run migration BEFORE creating any ADRs with `adrplus new`. Migration is a one-time setup operation.

---

### Issue: "No ADRs found to migrate"

**Problem**: Migration command found no ADR files.

**Causes & Solutions**:
- [x] ADR files don't exist in the specified path
  - **Solution**: Verify files are in `doc/adr/` or the configured folder

- [x] Files don't match the naming convention
  - **Solution**: Check filename format matches your config (e.g., `ADR0001-*.md`)

- [x] All ADRs already migrated
  - **Solution**: This is normal if you've run migration before; all files have valid headers

---

### Issue: "Permission denied" or "File is in use"

**Problem**: Cannot write to ADR files during migration.

**Causes & Solutions**:
- [x] Files are open in your IDE or editor
  - **Solution**: Close the files and retry

- [x] Files are read-only or locked
  - **Solution**: Check file permissions and make them writable:
    ```bash
    # Windows
    attrib -r doc/adr/*.md

    # Linux/Mac
    chmod 644 doc/adr/*.md
    ```

- [x] Antivirus software blocking file access
  - **Solution**: Temporarily disable antivirus or add exclusion

---

### Issue: "Some files were migrated, but others were skipped"

**Problem**: Partial migration, some ADRs remain unchanged.

**Causes & Solutions**:
- [x] Mixed migration status (some already migrated, others not)
  - **Behavior**: This is normal! Already-migrated files are skipped
  - **Solution**: Review logs to see which files were processed

- [x] Some files don't match naming convention
  - **Solution**: Rename files to match your configuration, then re-run migration

---

### Issue: "Migration changes look incorrect"

**Problem**: Header was added but looks wrong or content is corrupted.

**Causes & Solutions**:

- [x] Content was truncated or mixed up
  - **Solution**: Restore from Git backup: `git checkout doc/adr/ADRXXXX.md`

- [x] Encoding issues (special characters displayed incorrectly)
  - **Solution**: Ensure files use UTF-8 encoding

---

## Reverting Changes

If migration didn't go as planned, you can easily revert:

### Option 1: Restore from Git (Recommended)

```bash
# Restore all ADR files to pre-migration state
git checkout doc/adr/

# Verify restoration
git status
```
---
## Best Practices for Migration

### ✅ DO

- ✅ **Backup first**: Always commit to Git before migrating
  ```bash
  git add .
  git commit -m "backup: before ADR migration"
  ```

- ✅ **Test with wizard mode first**: Preview the migration before committing
  ```bash
  adrplus migrate --wizard
  ```

- ✅ **Review configuration**: Ensure naming conventions match your existing files
  ```bash
  adrplus config --repository
  ```

- ✅ **Verify each step**: After migration, open files to confirm format
  ```bash
  code doc/adr/ADR0001-*.md
  ```

- ✅ **Commit migration separately**: Keep migration in its own commit
  ```bash
  git commit -m "docs: migrate ADRs to AdrPlus format"
  ```

- ✅ **Document any changes**: Update team documentation about the new format
  ```bash
  git commit -m "docs: update ADR guidelines for AdrPlus format"
  ```

### ❌ DON'T

- ❌ **Don't migrate without backup**: Always use version control
- ❌ **Don't have files open**: Close IDE to avoid file locking
- ❌ **Don't modify configuration mid-migration**: Complete one migration first
- ❌ **Don't ignore error messages**: Read and address warnings/errors
- ❌ **Don't skip the wizard on first run**: Use interactive mode to preview

---

## Next Steps

After successful migration:

1. **Review all migrated ADRs**: Open each file to verify format
2. **Create new ADRs using AdrPlus**: 
   ```bash
   adrplus new
   ```

3. **Explore ADR management**:
   ```bash
   adrplus approve --wizard
   adrplus supersede --wizard
   adrplus version --wizard
   ```

4. **Share with your team**: Document the new ADR format and workflow
5. **Schedule regular reviews**: Keep ADRs up-to-date as architecture evolves

---

## Getting Help

### Need More Information?

- **Quick help**: `adrplus help migrate`
- **Full documentation**: See [README.md](README.md) for complete feature overview
- **Step-by-step guide**: See [StepByStepGuide.md](StepByStepGuide.md) for initial setup and first ADR creation
- **Configuration reference**: See [Configuration](README.md#configuration) in README.md for all config options
- **FAQ**: See [FAQ.md](FAQ.md) for common questions

### Having Issues?

1. **Ensure migration is configured**: Run `adrplus config --migrate` first
2. Verify no tool-created ADRs exist in your repository
3. Check the [Troubleshooting](#troubleshooting) section above
4. Review your `adr-config.adrplus` file using `adrplus config --repository`
5. Ensure ADR files follow the naming convention in your config
6. Check Git logs to understand what changed: `git log --oneline -5`
7. Use `--wizard` mode for interactive debugging: `adrplus migrate --wizard`

---

## Example: Complete Migration Workflow

Here's a realistic example of migrating a repository:

### Step 1: Configure Migration

```bash
adrplus config --migrate
```

Follow prompts to configure migration source settings. **This step is mandatory.**

### Step 2: Backup

```bash
git add .
git commit -m "backup: before ADR migration"
```

### Step 3: Review Configuration

```bash
adrplus config --repository
```

Output shows:
```json
{
  "prefix": "ADR",
  "lenseq": 4,
  "lenversion": 2,
  "folderadr": "doc/adr"
}
```

### Step 4: Verify No Tool-Created ADRs

Ensure all existing ADRs were manually created (not via `adrplus new`):

```bash
# Check if any ADRs exist with AdrPlus metadata
grep -l "<!-- Do not remove this comment" doc/adr/*.md 2>/dev/null || echo "All ADRs are manually created - migration can proceed"
```

If tool-created ADRs are found, either:
- Delete them first, then migrate, or
- Create a separate repository for migration

### Step 5: Run Migration Wizard

```bash
adrplus migrate --wizard
```

Follow prompts to select drive and repository.

### Step 6: Preview and Confirm

Review the 5 ADRs to migrate, then confirm with `Y`.

```
✓ Migrated: doc/adr/0001UsePostgreSQL.md
✓ Migrated: doc/adr/0002UseReactFramework.md
✓ Migrated: doc/adr/0003UseDocker.md
✓ Migrated: doc/adr/0004UseLowercaseNaming.md
✓ Migrated: doc/adr/0005UseServiceCollection.md
```

### Step 7: Verify

```bash
code doc/adr/0001UsePostgreSQL.md  # Open to verify
```

### Step 8: Commit

```bash
git add doc/adr/
git commit -m "docs: migrate ADRs to AdrPlus format"
git log --oneline -5
```

### Step 9: Create New ADR

Now that migration is complete, you can create new ADRs with the tool:

```bash
adrplus new --title "Use UUID for IDs"
```

Verify new ADR is created with correct format.

---

## Summary

Congratulations! You've successfully migrated your ADRs to AdrPlus format. Your ADRs now benefit from:

✅ Structured metadata (version, domain, status, dates)
✅ Automatic numbering and file naming
✅ Version and revision tracking
✅ Status management (Proposed, Accepted, Rejected, Superseded)
✅ Scope/domain organization
✅ Integration with AdrPlus tools for creation, review, and approval

Happy documenting! 🎉

