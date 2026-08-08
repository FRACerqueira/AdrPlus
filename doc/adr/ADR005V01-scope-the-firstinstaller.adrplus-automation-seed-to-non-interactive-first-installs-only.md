<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Scope the firstinstaller.adrplus automation seed to non-interactive first installs only|
|Version|01|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-08-08)|
|Changed|Accepted (2026-08-08)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Scope the firstinstaller.adrplus automation seed to non-interactive first installs only

## Deciders

* Deciders: Fernando Cerqueira (project maintainer)

## Context and Problem Statement

AdrPlus's first-run wizard runs automatically on the first command execution and already skips itself when standard input or output is redirected (CI, scripts, an AI agent driving the CLI), falling back to the tool's built-in defaults. Automated/CI installs sometimes need to start from a team's own pre-approved repository settings instead of those built-in defaults, with no interactive prompt at all — this need led to a seed mechanism, `firstinstaller.adrplus`, applied automatically on first install. Once that mechanism exists, a further question follows: should it also take effect for a human running AdrPlus interactively for the very first time on a machine that happens to have the seed file present (e.g. a company-provisioned image), or should it be strictly limited to non-interactive automation?

## Considered Options

* Seed file always takes precedence, even in a fully interactive terminal session — enforces the team's baseline on every install, human or automated
* Seed file only takes effect when the console is non-interactive (redirected stdin/stdout) — automation only; a real terminal session always gets the guided wizard
* Hybrid: in an interactive session, prompt the human to choose between the pre-approved seed and the guided wizard

## Decision Outcome

Chosen option: "Seed file only takes effect when the console is non-interactive", because the mechanism was requested specifically to unblock automation/CI/AI-agent scenarios, not to change what a human sees on first run; scoping it to the same redirection check that already gates the wizard keeps the feature's blast radius limited to its stated purpose and leaves the pre-existing interactive experience completely untouched.

Key characteristics of the chosen design:

* `firstinstaller.adrplus` lives at `<install dir>/template/firstinstaller.adrplus`, using the same JSON schema as `adr-config.adrplus`.
* Checked only inside the same `Console.IsInputRedirected || Console.IsOutputRedirected` branch that already decides whether to skip the wizard — a human at a real terminal always gets the wizard regardless of whether the seed file is present.
* Applied at most once: on success the seed is renamed to `firstinstaller.adrplus.applied`, so it cannot be reapplied and cannot be confused with a fresh seed on a later run.
* Misuse guard: if the seed is found again after the default repository template is already configured with *different* content, AdrPlus refuses with an actionable error instead of silently ignoring it.
* Safe-retry recovery: if a prior run crashed between writing the config and renaming the seed, a retry finds identical content and completes the rename instead of tripping the misuse guard — automation can retry a transient failure without manual cleanup.

### Positive Consequences

* Zero behavior change for every existing interactive install — the wizard's first-run experience is exactly what it was before this feature existed.
* Automation gets a documented, deterministic way to skip the wizard with approved defaults, without any risk of silently overriding a human's own first-run choice.
* The feature's scope stays aligned with why it was built, making it easier to reason about later instead of accumulating an unrequested "governance for all installs" behavior by accident.

### Negative Consequences

* If a team later wants to enforce a baseline on human-provisioned machines too (not just CI), that is a new, separate decision to make — this option does not support it as-is.
* Precedence still relies on the pre-existing `Console.IsInputRedirected`/`IsOutputRedirected` heuristic, which does not perfectly distinguish "a human at a terminal" from every possible automation harness (some allocate a real pty); this is an existing, known limitation the decision inherits rather than one it introduces.
