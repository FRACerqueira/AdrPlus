
![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)

# AdrPlus

Many teams still document architectural decisions **inconsistently** (scattered Markdown files, no version flow, and hard-to-track status changes).

AdrPlus was created to **solve this problem with a practical CLI workflow that keeps ADRs standardized, traceable, and easy to evolve over time**.

**AdrPlus** is a cross-platform .NET command-line tool for managing [Architecture Decision Records (ADRs)](https://adr.github.io/) directly from your terminal. 

It supports versioning, revision cycles, status workflows (approve / reject / undo), and an **interactive wizard** — all driven by a lightweight JSON configuration file.

> 🤖 **New:** manage your ADRs conversationally with the [**AdrPlus Claude Code Plugin**](https://github.com/FRACerqueira/AdrPlus-Claude-Plugin) — let Claude create, approve, audit, and index ADRs for you.

---

## Motivation and Benefits

Using **AdrPlus** in an engineering repository helps you:

- 📚 Keep architectural decisions organized with a predictable structure
- 🔍 Improve traceability with version, revise, and supersede flows
- ⚡ Reduce manual effort when creating and updating ADR files
- 🛠️ Respect the repository's configuration for naming, structure, and ADR status for each team.
- 🤝 Improve collaboration by making decision history visible to the whole team
- 🚀 Accelerate onboarding by exposing context behind technical choices

---

## Features

- 📝 **Create** new ADRs with auto-incremented sequential numbers
- 🔢 **Version** and **revise** existing ADRs (major version or revision bump)
- 🔄 **Supersede** an ADR by creating a successor with a new number
- ✅ **Approve** / ❌ **Reject** / ↩️ **Undo** ADR status changes
- 🧙 **Interactive wizard** for guided, step-by-step operations
- 🤖 **Claude Code integration** — manage ADRs conversationally via the official [Claude Code Plugin](https://github.com/FRACerqueira/AdrPlus-Claude-Plugin)
- 🔍 **Explorer** for viewing or **Generate reports** and managing ADR files in your repository
- ⚙️ **Config editor** for application ,repository settings and migration of existing ADRs to the standardized format
- 📂 **Customizable ADR structure** with user-defined templates and naming conventions
- 🔄 **Migrate** existing ADRs to the standardized format
- 💾 **Preserve settings and configuration** across upgrades and reinitializations
- 🗂️ **Multiple ADR** model options for different project needs and for each team
- 🌍 Multi-language support (`en-US`, `pt-BR`) for messages and UX
  - **ADR content can be written in any language!**
- 🖥️ Cross-platform (Windows, macOS, Linux)

---

## Using AdrPlus with Claude Code

Prefer managing ADRs in plain language instead of typing commands yourself? The official [**AdrPlus Claude Code Plugin**](https://github.com/FRACerqueira/AdrPlus-Claude-Plugin) lets [Claude Code](https://claude.com/claude-code) drive this CLI directly - a skill that teaches Claude the full command surface, plus an audit agent, an index-generator agent, and an agent that flags pending changes needing an ADR before a commit or PR. Requires `adrplus` v1.0.0-beta or later.

---

## License

This project is licensed under the [MIT License](LICENSE).

---