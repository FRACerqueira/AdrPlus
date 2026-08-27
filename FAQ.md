![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)

# Frequently Asked Questions

## Index

- [General](#general)
- [Configuration](#configuration)
- [Workflow and Commands](#workflow-and-commands)
- [Plugins](#plugins)
- [Troubleshooting and Best Practices](#troubleshooting-and-best-practices)

## General

### Can I use existing ADRs created with other tools?

Yes. You can use existing ADRs, but they must follow the expected naming and metadata format for the tool to recognize them properly. Consider running `migrate` to align them with the latest configuration.

### Do I need to use .NET in my application repository to use AdrPlus?

No. AdrPlus manages ADR Markdown files and can be used in repositories of any language or framework.

### Is it possible to have multiple ADRs with the same title but different scopes?

No. Title uniqueness is based on the title alone — Scope and Domain are free-text header fields and don't factor into it. Give ADRs that cover different areas a distinct title.

### Is the wizard mandatory to use AdrPlus?

On **first use**, the initial setup wizard runs automatically when you execute any command (except `help`) from a real interactive terminal. This ensures your configuration is set up correctly.

If standard input or output is redirected (CI, scripts, an AI agent driving the CLI), the wizard is skipped automatically instead of hanging or crashing on prompts that need real console input, and the tool falls back to its built-in defaults. A team can also pre-provision a `firstinstaller.adrplus` seed file so that first non-interactive run starts from approved settings instead — see [Non-Interactive Setup](StepByStepGuide.md#non-interactive-setup-ci-scripts-ai-agents) in the Step-by-Step Guide. This only affects non-interactive runs; a human at a real terminal always gets the wizard below.

After the initial setup is complete, the interactive wizard (`--wizard`) is optional for subsequent commands. You can run commands directly with their arguments without using the wizard.

### What file stores application-level settings?

`adrplus.json`.

### What file stores repository-level ADR rules?

`adr-config.adrplus`.

### Does AdrPlus support multiple UI languages?

Yes. Language is configurable (for example, `en-US` and `pt-BR`).

### What happens when I run AdrPlus without arguments?

The behavior is controlled by the `withoutargs` setting in `adrplus.json`:
- **`Help`** (default): Displays the help information with all available commands
- **`Wizard`**: Launches the interactive wizard for guided operations
- **`None`**: Requires you to explicitly provide a command; otherwise, an error is shown

You can change this behavior anytime by running `adrplus config --application`.

### What happens if I run a command but omit one of its required arguments?

The `withoutargs` setting only governs what happens when **no command** is given at all (the case
above) — it does not apply once you've named a specific command. If that command has a required
argument (marked "Required When not in wizard mode" in `adrplus help <command>` — for example
`--file` for `approve`/`reject`/`undo`/`revise`/`version`/`supersede`, or `--title` for `new`) and
you omit it without `--wizard`, AdrPlus fails immediately with a clear
`Required argument '--x' (-x) is missing` error, regardless of `withoutargs`. This also means
`adrplus help <command>` always shows that command's help text, independent of `withoutargs`.

## Configuration

### Can I customize ADR headers?

You can relabel the existing header fields, but you cannot add or remove fields from the header table itself. Relabel via `adr-config.adrplus` keys such as `statusnew`, `statusacc`, `statusrej`, `statussup`, `headertitlestatuscreated`, `headertitlestatuschanged`, and `headertitlestatussuperseded` — these change the displayed text, not the header's structure.

### Can I use custom status labels?

Yes. You can customize status labels in `adr-config.adrplus` with `statusnew`, `statusacc`, `statusrej`, and `statussup`.

### Can I organize ADRs by scope folders?

No. Scope is a free-text header field with no folder or naming behavior tied to it. If you want ADRs physically grouped by area, organize `folderadr` subfolders yourself.

### When is `--domain` required for `new`?

Never — `--scope` and `--domain` are both optional, free-text fields with no validation.

### Can I change the date format in ADR metadata?

No. The date format is fixed in the tool's metadata handling and cannot be customized.

## Workflow and Commands

### What is the difference between `version`, `revise`, and `supersede`?

- `version`: creates a new major version of the same ADR sequence. Accepts `--scope`/`--domain` to reclassify the topic if it changed; omitted, it keeps the source ADR's current values.
- `revise`: creates a revision of the same ADR version (when revision is enabled). Always carries Scope/Domain forward unchanged — no `--scope`/`--domain` flags, since a revision is a wording fix to the same decision, not a reclassification.
- `supersede`: creates a successor ADR with a new sequence number. Also accepts `--scope`/`--domain`, same as `version` — a genuinely different decision is at least as likely to belong elsewhere.

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

## Plugins

### How do I install a plugin?

Plugins are installed **once per machine**, not per repository. Run `adrplus plugins --install "./PluginName-1.0.0.zip"` — the zip must be named `<name>-<version>.zip`, matching its own `plugin.json`. Prefer doing it by hand instead? Drop the compiled plugin (its `.dll` plus a `plugin.json` manifest) into `%UserProfile%/AdrPlus.Plugins/<name>/` directly — both end up the same place. Either way, the plugin is available to every repository on that machine but starts out `Inactive` in each one: run `adrplus plugins --activate <name> --path "path/to/repository"` per repo to let it actually dispatch there. `adrplus plugins --wizard` walks through install and activation interactively too, if you'd rather not type flags. See the [Plugin Development Guide](PluginDevelopmentGuide.md) for the full contract.

### What does the bundled `AdrIndexer` plugin do?

`AdrPlus.Plugins.AdrIndexer` ships with AdrPlus and is discovered automatically on every machine that has AdrPlus installed — no separate install step. It regenerates a linked table of your ADRs (ADR, title, version, status) whenever an ADR event occurs (created, versioned, revised, approved, rejected, superseded, undone, or migrated) — not only on status changes.

### Can I disable plugins?

Yes. Set `disableplugins` to `true` in `adr-config.adrplus` to stop all plugin dispatch for a repository, or run `adrplus plugins --deactivate <name> --path "path/to/repository"` (or remove it from `activeplugins` by hand) to disable one plugin at a time for that repository. `adrplus plugins --uninstall <name>` removes it from the machine entirely — since plugins are host-global, this doesn't touch any repository's `activeplugins`; a repo that still lists the name simply starts reporting it `Missing` the next time a command dispatches.

### Where are plugins stored, and how do I remove everything?

User-installed plugins live in `%UserProfile%/AdrPlus.Plugins/`; AdrPlus also keeps a separate `%UserProfile%/AdrPlus.History/` folder used to carry configuration settings across version upgrades. Neither is touched by uninstalling the `adrplus` tool itself (`dotnet tool uninstall -g adrplus`) — dotnet global tools have no uninstall hook, so both folders are left behind by design, the same way any other CLI's config directory would be. If you want a completely clean removal, delete both folders manually after uninstalling.

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

Depends on which one:
- **The install-level default template** (used by `adrplus init` to seed a brand-new repository when no `--file` is given): if missing, `init` falls back to generating a valid default from the bundled ADR template instead of failing. If malformed, `init` reports a validation error naming the specific problem.
- **An already-initialized repository's own `adr-config.adrplus`**: if missing or malformed, commands that depend on it (`new`, `approve`, `migrate`, etc.) fail with a validation error until the file is restored or corrected — AdrPlus doesn't guess at fixing a real repository's settings.

### Can I use AdrPlus in a monorepo with multiple projects?

Yes. There's no built-in per-project folder organization, but you can point each project at its own `adr-config.adrplus`/`folderadr`, or use the free-text Scope/Domain header fields to note which project an ADR belongs to.

