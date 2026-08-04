<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## AdrPlus\.Abstractions\.Domain Namespace

| Classes | |
| :--- | :--- |
| [AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot') | Immutable public snapshot of an ADR record, exposed to plugins via [Adr](AdrPlus.Abstractions.AdrEventContext.md#AdrPlus.Abstractions.AdrEventContext.Adr 'AdrPlus\.Abstractions\.AdrEventContext\.Adr')\. |
| [RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot') | Immutable public snapshot of the parts of the repository configuration relevant to plugins, exposed via [Repo](AdrPlus.Abstractions.AdrEventContext.md#AdrPlus.Abstractions.AdrEventContext.Repo 'AdrPlus\.Abstractions\.AdrEventContext\.Repo')\. |

| Enums | |
| :--- | :--- |
| [AdrStatus](AdrPlus.Abstractions.Domain.AdrStatus.md 'AdrPlus\.Abstractions\.Domain\.AdrStatus') | Public mirror of the host's internal ADR status, exposed to plugins via [AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot') and [RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot')\. |
