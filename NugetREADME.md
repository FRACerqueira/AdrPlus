
[![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)](logo)

# AdrPlus

Many teams still document architectural decisions **inconsistently** (scattered Markdown files, no revision flow, and hard-to-track status changes).

AdrPlus was created to **solve this problem with a practical CLI workflow that keeps ADRs standardized, traceable, and easy to evolve over time**.

**AdrPlus** is a cross-platform .NET command-line tool for managing [Architecture Decision Records (ADRs)](https://adr.github.io/) directly from your terminal. 

It supports versioning, revision cycles, status workflows (approve / reject / undo), and an **interactive wizard** — all driven by a lightweight JSON configuration file.



## Motivation and Benefits

Using **AdrPlus** in an engineering repository helps you:

- 📚 Keep architectural decisions organized with a predictable structure
- 🔍 Improve traceability with version, review, and supersede flows
- ⚡ Reduce manual effort when creating and updating ADR files
- 🛠️ Respect the repository's configuration for naming, structure, and ADR status for each team.
- 🤝 Improve collaboration by making decision history visible to the whole team
- 🚀 Accelerate onboarding by exposing context behind technical choices

---

## Features

- 📝 **Create** new ADRs with auto-incremented sequential numbers
- 🔢 **Version** and **review** existing ADRs (major version or revision bump)
- 🔄 **Supersede** an ADR by creating a successor with a new number
- ✅ **Approve** / ❌ **Reject** / ↩️ **Undo** ADR status changes
- 🧙 **Interactive wizard** for guided, step-by-step operations
- ⚙️ **Config editor** for application and repository settings
- 🗂️ **Multiple ADR** model options for different project needs and for each team
- 🌍 Multi-language support (`en-US`, `pt-BR`) for messages and UX
  - **ADR content can be written in any language!**
- 🖥️ Cross-platform (Windows, macOS, Linux)

---


## License

This project is licensed under the [MIT License](LICENSE).

---