![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)

# Translations

AdrPlus's own UI strings (`src/Resources/AdrPlus.*.resx`) and ADR templates (`src/Resources/*-template*.md`, `src/Resources/adr-template*.adrplus`) are localized into 11 cultures, matching the set already shipped by the `PromptPlus` dependency.

## UI strings

| Culture | Resource file | Status |
|---|---|---|
| en-US (neutral) | `AdrPlus.resx` | Reviewed |
| pt-BR | `AdrPlus.pt-BR.resx` | Reviewed |
| de-DE | `AdrPlus.de-DE.resx` | Machine-translated, pending native-speaker review |
| es-ES | `AdrPlus.es-ES.resx` | Machine-translated, pending native-speaker review |
| fr-FR | `AdrPlus.fr-FR.resx` | Machine-translated, pending native-speaker review |
| it-IT | `AdrPlus.it-IT.resx` | Machine-translated, pending native-speaker review |
| ja-JP | `AdrPlus.ja-JP.resx` | Machine-translated, pending native-speaker review |
| ko-KR | `AdrPlus.ko-KR.resx` | Machine-translated, pending native-speaker review |
| nl-BE | `AdrPlus.nl-BE.resx` | Machine-translated, pending native-speaker review |
| ru-RU | `AdrPlus.ru-RU.resx` | Machine-translated, pending native-speaker review |
| zh-CN | `AdrPlus.zh-CN.resx` | Machine-translated, pending native-speaker review |

Each machine-translated `.resx` carries an XML comment at the top noting its pending-review status. No build/csproj changes are needed to add or update a satellite `.resx` — they're picked up automatically by filename.

## ADR templates

8 base templates (`adr-template`/`madr-template`, `alexandrian-template`, `business-case-template`, `merson-template`, `nygard-template`, `planguage-template`, `tyree-ackerman-template`), each in all 11 languages. Filename pattern: `<template-name>-<suffix>.<ext>` (e.g. `nygard-template-de.md`); the neutral English file has no suffix, Portuguese uses `-ptbr` (kept for backward compatibility with the original filename).

| Culture | Suffix | Status |
|---|---|---|
| en-US (neutral) | *(none)* | Reviewed |
| pt-BR | `-ptbr` | Reviewed |
| de-DE | `-de` | Machine-translated, pending native-speaker review |
| es-ES | `-es` | Machine-translated, pending native-speaker review |
| fr-FR | `-fr` | Machine-translated, pending native-speaker review |
| it-IT | `-it` | Machine-translated, pending native-speaker review |
| ja-JP | `-ja` | Machine-translated, pending native-speaker review |
| ko-KR | `-ko` | Machine-translated, pending native-speaker review |
| nl-BE | `-nl` | Machine-translated, pending native-speaker review |
| ru-RU | `-ru` | Machine-translated, pending native-speaker review |
| zh-CN | `-zh` | Machine-translated, pending native-speaker review |

Each machine-translated template file carries an HTML comment at the top noting its pending-review status. Adding a new template family or language requires no `AdrPlus.csproj` changes — `Resources\adr-template*.adrplus` and `Resources\*-template*.md` are wildcard-embedded.

If you're a native speaker of one of these languages and want to review or correct a file (UI string or template), please open a PR against the corresponding file.
