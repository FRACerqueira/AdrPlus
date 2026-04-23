[![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)](logo)

# Contributing to AdrPlus

Thank you for your interest in contributing! This document explains how to get involved.

---

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How to Contribute](#how-to-contribute)
- [Development Setup](#development-setup)
- [Running Tests](#running-tests)
- [Coding Guidelines](#coding-guidelines)
- [Commit Messages](#commit-messages)
- [Submitting a Pull Request](#submitting-a-pull-request)
- [Reporting Bugs](#reporting-bugs)
- [Requesting Features](#requesting-features)

---

## Code of Conduct

This project follows the rules described in [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
By participating, you agree to follow these standards in all project spaces.

---

## Getting Started

1. **Fork** the repository.
2. **Clone** your fork:
   ```bash
   git clone https://github.com/<your-username>/AdrPlus.git
   cd AdrPlus
   ```
3. Create a **feature branch** from `main`:
   ```bash
   git checkout -b feat/my-amazing-feature
   ```

---

## How to Contribute

- **Bug fixes** — open or find an issue, comment that you are working on it, submit a PR.
- **New features** — open a feature-request issue first so we can discuss it before you invest time coding.
- **Documentation** — improvements to `README.md`, XML docs, or markdown files are always welcome.
- **Tests** — additional test coverage is always appreciated.

---

## Development Setup

### Prerequisites

| Tool | Minimum version |
|------|----------------|
| .NET SDK | 10.0 |
| Git | 2.x |

### Build

```bash
dotnet restore
dotnet build
```

### Run locally

```bash
dotnet run --project src/AdrPlus.csproj -- help
```

---

## Running Tests

```bash
dotnet test
```

For coverage:

```bash
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test
```

---

## Coding Guidelines

- Target **C# 14 / .NET 10** idioms where possible (file-scoped namespaces, primary constructors, etc.).
- **Least exposure**: prefer `private` over `public` unless the API truly needs to be public.
- **Null safety**: use `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`; avoid `!`.
- All async methods must end with `Async` and accept a `CancellationToken`.
- Add XML documentation (`<summary>`) to all public and internal members.
- Add or update `CHANGELOG.md` under `[Unreleased]` for every user-visible change.

---

## Commit Messages

We follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short summary>

[optional body]

[optional footer(s)]
```

| Type | When to use |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `refactor` | Code change with no feature/fix |
| `test` | Adding or updating tests |
| `chore` | Build, CI, tooling changes |
| `perf` | Performance improvement |

Example:

```
feat(new-adr): add --template flag to override default template
```

---

## Submitting a Pull Request

1. Ensure all tests pass locally: `dotnet test`.
2. Fill in the [PR template](.github/pull_request_template.md).
3. Keep PRs focused — one feature or fix per PR.
4. PRs are merged via **squash merge** to keep the history clean.

---

## Reporting Bugs

Use the [bug report template](.github/ISSUE_TEMPLATE/bug_report.md) and include:
- AdrPlus version (`adrplus version`)
- OS and .NET version
- Minimal reproduction steps

---

## Requesting Features

Use the [feature request template](.github/ISSUE_TEMPLATE/feature_request.md). The more context you provide, the faster we can evaluate and prioritize.
