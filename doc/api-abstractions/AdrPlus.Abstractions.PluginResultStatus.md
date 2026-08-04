<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## PluginResultStatus Enum

Outcome of a plugin's reaction to an ADR lifecycle event\.

```csharp
public enum PluginResultStatus
```
### Fields

<a name='AdrPlus.Abstractions.PluginResultStatus.Success'></a>

`Success` 0

The plugin handled the event successfully\.

<a name='AdrPlus.Abstractions.PluginResultStatus.Skipped'></a>

`Skipped` 1

The plugin deliberately chose not to act on this event\.

<a name='AdrPlus.Abstractions.PluginResultStatus.Failed'></a>

`Failed` 2

The plugin attempted to handle the event and failed\. See [IsRetryable](AdrPlus.Abstractions.PluginResult.md#AdrPlus.Abstractions.PluginResult.IsRetryable 'AdrPlus\.Abstractions\.PluginResult\.IsRetryable')\.