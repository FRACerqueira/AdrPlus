# Security Policy

## Supported Versions

| Version | Supported          |
|---------|--------------------|
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
- Malicious configuration file (`AdrPlus.json`) that causes unintended file-system operations.
- Supply-chain issues in dependencies.

---

## Security Best Practices for Users

- Keep your .NET SDK and AdrPlus tool updated to the latest version.
- Do not run AdrPlus with elevated (`sudo` / administrator) privileges unless strictly necessary.
- Treat `AdrPlus.json` as a trusted configuration file — do not copy it from untrusted sources.
