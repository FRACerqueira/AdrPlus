<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## AdrEventType Enum

Identifies the ADR lifecycle event that triggered a plugin dispatch\.
Plugins MUST treat unknown/future values as [Skipped](AdrPlus.Abstractions.PluginResultStatus.md#AdrPlus.Abstractions.PluginResultStatus.Skipped 'AdrPlus\.Abstractions\.PluginResultStatus\.Skipped') rather than throwing\.

```csharp
public enum AdrEventType
```
### Fields

<a name='AdrPlus.Abstractions.AdrEventType.Created'></a>

`Created` 0

An ADR was created\. Content is metadata\-only scaffolding at this point\.

<a name='AdrPlus.Abstractions.AdrEventType.Versioned'></a>

`Versioned` 1

A new version of an ADR was created\. Content is metadata\-only scaffolding at this point\.

<a name='AdrPlus.Abstractions.AdrEventType.Revised'></a>

`Revised` 2

An ADR was revised\. Content is metadata\-only scaffolding at this point \(may start from an empty draft\)\.

<a name='AdrPlus.Abstractions.AdrEventType.Superseded'></a>

`Superseded` 3

An ADR was marked as superseded by another ADR\. Content is settled\.

<a name='AdrPlus.Abstractions.AdrEventType.Approved'></a>

`Approved` 4

An ADR was approved\. Content is settled\.

<a name='AdrPlus.Abstractions.AdrEventType.Rejected'></a>

`Rejected` 5

An ADR was rejected\. Content is settled\.

<a name='AdrPlus.Abstractions.AdrEventType.StatusUndone'></a>

`StatusUndone` 6

A previous status change on an ADR was undone\. Content is settled\.

<a name='AdrPlus.Abstractions.AdrEventType.Migrated'></a>

`Migrated` 7

An ADR was migrated\. Content is settled\.