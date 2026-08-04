<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions\.Domain](AdrPlus.Abstractions.Domain.md 'AdrPlus\.Abstractions\.Domain')

## AdrStatus Enum

Public mirror of the host's internal ADR status, exposed to plugins via [AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot') and [RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot')\.

```csharp
public enum AdrStatus
```
### Fields

<a name='AdrPlus.Abstractions.Domain.AdrStatus.Unknown'></a>

`Unknown` 0

Indicates an unknown or unspecified value\.

<a name='AdrPlus.Abstractions.Domain.AdrStatus.Proposed'></a>

`Proposed` 1

Draft open for proposed discussion\.

<a name='AdrPlus.Abstractions.Domain.AdrStatus.Accepted'></a>

`Accepted` 2

Approved and ready for implementation\.

<a name='AdrPlus.Abstractions.Domain.AdrStatus.Rejected'></a>

`Rejected` 3

Decision not adopted \(record rationale\)\.

<a name='AdrPlus.Abstractions.Domain.AdrStatus.Superseded'></a>

`Superseded` 4

A new decision has been made that invalidates the previous one; maintain link and history\.