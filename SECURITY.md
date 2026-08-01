![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)

# Security Policy

## Supported Versions

| Version | Supported          |
|---------|--------------------|
| 0.x     | ❌ Partially supported |
| 1.x     | ✅ Actively supported |

---

## Reporting a Vulnerability

**Please do NOT open a public GitHub issue for security vulnerabilities.**

If you discover a security vulnerability in AdrPlus, please report it responsibly:

1. Go to the **Security** tab of this repository on GitHub.
2. Click **"Report a vulnerability"** (GitHub Private Vulnerability Reporting).
3. Fill in the details: affected versions, reproduction steps, impact, and any suggested mitigations.

If private vulnerability reporting is unavailable for any reason, contact the repository maintainers through a private GitHub channel.

We aim to:

- Acknowledge your report within **48 hours**.
- Provide an initial assessment within **7 days**.
- Release a patch (if confirmed) within **30 days** for critical or high-severity issues.

We will credit you in the release notes unless you prefer to remain anonymous.

---

## Scope

AdrPlus is a **local CLI tool** that reads and writes Markdown files on the developer's machine.
It does not expose network services, handle credentials, or process untrusted remote input by design.

Typical in-scope concerns include:

- Path traversal or arbitrary file writes via command arguments.
- Malicious configuration file (`adrplus.json`) that causes unintended file-system operations.
- Supply-chain issues in dependencies.

---

## Plugins

AdrPlus supports optional plugins loaded from `./plugins/<name>/` in a repository. Plugins are **third-party
code that runs with the invoking user's own OS permissions** — the same trust level as any other executable
the user chooses to run. AdrPlus does not sandbox plugin code; an optional allowlist in `adrplus.json` (by name
and/or assembly hash) lets a team restrict which plugins load, but does not isolate what an allowed plugin can do.

A plugin's `plugin.json` manifest — including its `settings` block — may be committed to the repository so the
whole team gets the same installed plugins on clone. Because `settings` is plain JSON checked into git:

- **Never put real credential values in `settings`.** Only non-secret configuration belongs there (base URLs,
  space keys, feature flags, etc.). Credential resolution (tokens, API keys) is entirely the plugin's own
  responsibility — e.g. reading an environment variable or a local secret store at runtime — never the host's.
- Review a plugin's `plugin.json` like any other file added to the repository before trusting it.

---

## Security Best Practices for Users

- Keep your .NET SDK and AdrPlus tool updated to the latest version.
- Do not run AdrPlus with elevated (`sudo` / administrator) privileges unless strictly necessary.
- Treat `adrplus.json` as a trusted configuration file — do not copy it from untrusted sources.
- Only install plugins under `./plugins/` from sources you trust, and use the plugin allowlist to prevent
  unreviewed plugins from loading.
