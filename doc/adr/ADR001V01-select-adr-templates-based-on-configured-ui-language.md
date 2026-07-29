<!-- Do not remove this comment, lines and table (1-12) -->
|Adr-Plus Fields|Values Migrated |
|--|--|
|File title md|Select ADR templates based on configured UI language|
|Version|01|
|Revision||
|Scope||
|Domain||
|Created|Proposed (2026-07-29)|
|Changed|Accepted (2026-07-29)|
|Superseded||
<!-- Do not remove this comment, lines and table (1-12) -->
---
# Select ADR templates based on configured UI language

## Context and Problem Statement

AdrPlus ships 7 methodology ADR templates. Before 1.0.0-beta4, only English and Portuguese template variants existed, and the tool always offered these two regardless of the user's configured UI language. Beta4 added UI translations for 9 more languages (`de-DE`, `es-ES`, `fr-FR`, `it-IT`, `ja-JP`, `ko-KR`, `nl-BE`, `ru-RU`, `zh-CN`) to match the set already shipped by the `PromptPlus` dependency. Should the ADR template offered to a user match their configured UI language, or continue to default to English/Portuguese only?

## Considered Options

* Keep offering only English/Portuguese templates regardless of configured UI language
* Select the ADR template based on the configured UI language
* Auto-translate templates on demand at runtime

## Decision Outcome

Chosen option: "Select the ADR template based on the configured UI language", because it keeps ADR content consistent with the rest of the localized experience (UI strings) instead of leaving ADR templates in a mismatched language, and avoids the complexity and unreliability of on-demand runtime translation.

### Positive Consequences

* All 7 methodology templates are now available in all 11 supported languages, matching the UI language picker.
* Users get a template in the same language as the rest of the wizard/UI instead of a fixed English/Portuguese fallback.

### Negative Consequences

* The 9 new language template variants are machine-translated and pending native-speaker review (see `TRANSLATIONS.md`); content quality may vary until reviewed.

## Links

* Implements [CHANGELOG.md - 1.0.0-beta4](../../CHANGELOG.md)
* Translation review status: [TRANSLATIONS.md](../../TRANSLATIONS.md)
