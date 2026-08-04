<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## IPluginLogger Interface

Host\-provided logger given to a plugin via [IPluginContext](AdrPlus.Abstractions.IPluginContext.md 'AdrPlus\.Abstractions\.IPluginContext')\. Entries are unified with the
host's own file log; plugin authors do not need to pass a correlation id explicitly — use
[CorrelationId](AdrPlus.Abstractions.AdrEventContext.md#AdrPlus.Abstractions.AdrEventContext.CorrelationId 'AdrPlus\.Abstractions\.AdrEventContext\.CorrelationId') in the message if it should be cross\-referenced\.

```csharp
public interface IPluginLogger
```
### Methods

<a name='AdrPlus.Abstractions.IPluginLogger.LogError(string,System.Exception)'></a>

## IPluginLogger\.LogError\(string, Exception\) Method

Logs an error message, with an optional associated exception\.

```csharp
void LogError(string message, System.Exception? exception=null);
```
#### Parameters

<a name='AdrPlus.Abstractions.IPluginLogger.LogError(string,System.Exception).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='AdrPlus.Abstractions.IPluginLogger.LogError(string,System.Exception).exception'></a>

`exception` [System\.Exception](https://learn.microsoft.com/en-us/dotnet/api/system.exception 'System\.Exception')

<a name='AdrPlus.Abstractions.IPluginLogger.LogInformation(string)'></a>

## IPluginLogger\.LogInformation\(string\) Method

Logs an informational message\.

```csharp
void LogInformation(string message);
```
#### Parameters

<a name='AdrPlus.Abstractions.IPluginLogger.LogInformation(string).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='AdrPlus.Abstractions.IPluginLogger.LogWarning(string)'></a>

## IPluginLogger\.LogWarning\(string\) Method

Logs a warning message\.

```csharp
void LogWarning(string message);
```
#### Parameters

<a name='AdrPlus.Abstractions.IPluginLogger.LogWarning(string).message'></a>

`message` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')