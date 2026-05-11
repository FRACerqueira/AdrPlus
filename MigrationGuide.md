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

✅ **Repository already initialized with AdrPlus**
   - Run: `adrplus init` (if not already done)
   - This creates the `adr-config.adrplus` configuration file

✅ **Existing ADR files in your repository**
   - You have `.md` files that need to be migrated
   - Files should be in a consistent location (e.g., `doc/adr/`)

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

### ⚠️ Important Checklist

- [x] **Backup your repository**: Commit everything to Git
  ```bash
  git add .
  git commit -m "backup: before ADR migration"
  ```

- [x] **Verify configuration is correct**
  ```bash
  adrplus config --repository  # Review settings
  ```

- [x] **Test with one ADR first**: Use `--wizard` mode to preview before committing

- [x] **Ensure all ADR files follow naming convention**: 
  - Expected format: `prefix-number-title.md` (e.g., `0001-UsePostgreSQL.md`)
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
Migrated: doc/adr/0001-UsePostgreSQL.md
Migrated: doc/adr/0002-UseReactFramework.md
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

### Issue: "No ADRs found to migrate"

**Problem**: Migration command found no ADR files.

**Causes & Solutions**:
- [x] ADR files don't exist in the specified path
  - **Solution**: Verify files are in `doc/adr/` or the configured folder

- [x] Files don't match the naming convention
  - **Solution**: Check filename format matches your config (e.g., `ADR-0001-*.md`)

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
  - **Solution**: Restore from Git backup: `git checkout doc/adr/ADR-XXXX.md`

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
  code doc/adr/ADR-0001-*.md
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
   adrplus review --wizard
   adrplus supersede --wizard
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

1. Check the [Troubleshooting](#troubleshooting) section above
2. Review your `adr-config.adrplus` file using `adrplus config --repository`
3. Ensure ADR files follow the naming convention in your config
4. Check Git logs to understand what changed: `git log --oneline -5`
5. Use `--wizard` mode for interactive debugging: `adrplus migrate --wizard`

---

## Example: Complete Migration Workflow

Here's a realistic example of migrating a repository:

### Step 1: Backup

```bash
git add .
git commit -m "backup: before ADR migration"
```

### Step 2: Review Configuration

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

### Step 3: Run Migration Wizard

```bash
adrplus migrate --wizard
```

Follow prompts to select drive and repository.

### Step 4: Preview and Confirm

Review the 5 ADRs to migrate, then confirm with `Y`.

```
✓ Migrated: doc/adr/0001UsePostgreSQL.md
✓ Migrated: doc/adr/0002-UseReactFramework.md
✓ Migrated: doc/adr/0003-UseDocker.md
✓ Migrated: doc/adr/0004-UseLowercaseNaming.md
✓ Migrated: doc/adr/0005-UseServiceCollection.md
```

### Step 5: Verify

```bash
code doc/adr/ADR-0001-UsePostgreSQL.md  # Open to verify
```

### Step 6: Commit

```bash
git add doc/adr/
git commit -m "docs: migrate ADRs to AdrPlus format"
git log --oneline -5
```

### Step 7: Create New ADR

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

