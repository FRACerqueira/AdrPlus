[![icon](./icon.png)](./icon.png)

# Frequently Asked Questions

## Index

- [General](#general)
- [Configuration](#configuration)
- [Workflow and Commands](#workflow-and-commands)
- [Troubleshooting and Best Practices](#troubleshooting-and-best-practices)

## General

### Can I use existing ADRs created with other tools?

Yes. You can use existing ADRs, but they must follow the expected naming and metadata format for the tool to recognize them properly. Consider running `migrate` to align them with the latest configuration.

### Do I need to use .NET in my application repository to use AdrPlus?

No. AdrPlus manages ADR Markdown files and can be used in repositories of any language or framework.

### Is it possible to have multiple ADRs with the same title but different scopes?

Yes. If scopes are enabled, ADRs with the same title can coexist as long as they are in different scopes.

### Is the wizard mandatory to use AdrPlus?

On **first use**, the initial setup wizard runs automatically when you execute any command (except `help`). This ensures your configuration is set up correctly.

After the initial setup is complete, the interactive wizard (`--wizard`) is optional for subsequent commands. You can run commands directly with their arguments without using the wizard.

### What file stores application-level settings?

`adrplus.json`.

### What file stores repository-level ADR rules?

`adr-config.adrplus`.

### Does AdrPlus support multiple UI languages?

Yes. Language is configurable (for example, `en-US` and `pt-BR`).

### What happens when I run AdrPlus without arguments?

The behavior is controlled by the `WithoutArgs` setting in `adrplus.json`:
- **`Help`** (default): Displays the help information with all available commands
- **`Wizard`**: Launches the interactive wizard for guided operations
- **`None`**: Requires you to explicitly provide a command; otherwise, an error is shown

You can change this behavior anytime by running `adrplus config --application`.

## Configuration

### Can I customize ADR headers?

No. You cannot customize ADR headers directly. You can customize status labels and header names in `adr-config.adrplus` using keys such as `statusnew`, `statusacc`, `statusrej`, `statussup`, `headerstatus`, `headerversion`, and `headerrevision`.

### Can I use custom status labels?

Yes. You can customize status labels in `adr-config.adrplus` with `statusnew`, `statusacc`, `statusrej`, and `statussup`.

### Can I organize ADRs by scope folders?

Yes. Set `folderbyscope` to `true` but ensure your workflow and team conventions align with this structure.

### When is `--domain` required for `new`?

When scope is enabled and the selected scope is not listed in `skipdomain`.

### Can I change the date format in ADR metadata?

No. The date format is fixed in the tool's metadata handling and cannot be customized.

## Workflow and Commands

### What is the difference between `version`, `review`, and `supersede`?

- `version`: creates a new major version of the same ADR sequence.
- `review`: creates a revision of the same ADR version (when revision is enabled).
- `supersede`: creates a successor ADR with a new sequence number.

### How does the tool determine the next ADR number when creating a new ADR?

The tool scans existing ADR files, finds the highest sequence number, and increments it by one.

### Can I run AdrPlus without interactive prompts?

Yes. Pass arguments directly (for example, `--title`, `--file`, `--path`).

### Can AdrPlus create links between superseded and superseding ADRs automatically?

Yes. It follows the integrated status and naming workflow and reports in the header of the replaced ADR that a replacement exists.

### Does AdrPlus support adding metadata fields like owner, tags, or decision date?

No. This version focuses on core ADR fields. You can include additional information in the ADR content as needed.

### Can I configure different templates per scope or domain?

Not as separate integrated template files, however the tool uses the template in the configuration file in each repository. In this scenario, you can use the tool without having to configure the template that has already been agreed upon by the team.

## Troubleshooting and Best Practices

### What happens if an ADR is deleted?

Deleting ADR files can break traceability. Prefer `reject` or `supersede`.

### What happens if I try to approve an ADR that is already rejected?

The tool blocks the action. Use `undo` first, then approve if applicable.

### How does AdrPlus behave when two users create ADRs concurrently?

Concurrent changes may cause numbering conflicts; resolve via normal Git merge/rebase flow.

### Can I lock ADR files to avoid conflicting status changes?

Use repository practices (branch policies, code owners, reviews) to control concurrent edits.

### What happens if `adr-config.adrplus` is missing or malformed?

Commands that depend on repository config can fail until the file is fixed.

### Can I use AdrPlus in a monorepo with multiple projects?

Yes. Use scopes to organize ADRs by project, and ensure your team conventions align with this structure.

