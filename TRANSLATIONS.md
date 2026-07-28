![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)

# Translations

AdrPlus's own UI strings (`src/Resources/AdrPlus.*.resx`) are localized into 11 cultures, matching the set already shipped by the `PromptPlus` dependency. ADR templates (the `*-template.md` files) remain English/Portuguese-only for now — that is a separate, larger effort not covered by this table.

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

Each machine-translated file carries an XML comment at the top of the `.resx` noting its pending-review status. If you're a native speaker of one of these languages and want to review or correct a file, please open a PR against the corresponding `.resx` — no other changes (build/csproj) are needed, satellite resources are picked up automatically by filename.
