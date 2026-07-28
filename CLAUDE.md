## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]


## 5. Current Project Status

- **Version**: `1.0.0-beta3` (published as a GitHub tag; the project is in beta — breaking changes are fine, don't add compatibility aliases/shims for renames).
- **Localization**: both the UI strings (`src/Resources/AdrPlus.*.resx`) and the ADR templates (`src/Resources/*-template*.md`, `adr-template*.adrplus`) are localized into 11 languages — `en-US` (neutral), `pt-BR`, `de-DE`, `es-ES`, `fr-FR`, `it-IT`, `ja-JP`, `ko-KR`, `nl-BE`, `ru-RU`, `zh-CN` — matching the set already shipped by the `PromptPlus` dependency. The first-run wizard offers all 11 explicitly, plus "Other" for any other valid culture code.
- The 9 non-English/Portuguese languages are machine-translated and marked pending native-speaker review — see `TRANSLATIONS.md` for per-file status before treating any of them as final.

## 6. graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).

Guardrail (data sensitivity):
- Code files are parsed locally (tree-sitter, no LLM call). Only docs, papers, and images go through *semantic* extraction — either via `GEMINI_API_KEY`/`GOOGLE_API_KEY` if set, or by the host AI assistant reading the file content directly if not. Treat that pass as leaving the machine.
- `.graphifyignore` (repo root) already excludes known secret-bearing filenames (`.env*`, `*.pem`/`*.key`, `appsettings.*.json`, SSH keys, credential dotfiles, `secrets/`/`credentials/` dirs). It only filters by filename, not content.
- Before running anything beyond `--code-only` (i.e. before letting docs/papers/images enter semantic extraction), list the doc/paper/image files graphify's `detect` step found and get explicit confirmation from the user if any could plausibly contain secrets, tokens, connection strings, or customer data that isn't obvious from the filename alone.
- If `skipped_sensitive` is non-empty, report it to the user before continuing — don't silently proceed.
- Default to `/graphify . --code-only` for routine graph builds/updates on this repo; only drop `--code-only` when the user explicitly wants docs/papers/images included.
