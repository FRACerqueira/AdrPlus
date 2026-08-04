<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## IAdrPlugin Interface

The single mandatory contract every AdrPlus plugin must implement\.

```csharp
public interface IAdrPlugin : System.IAsyncDisposable
```

Derived  
↳ [AdrPluginBase](AdrPlus.Abstractions.AdrPluginBase.md 'AdrPlus\.Abstractions\.AdrPluginBase')

Implements [System\.IAsyncDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable 'System\.IAsyncDisposable')

### Remarks
One singleton instance is held per plugin, reused across events for the lifetime of the process —
[OnAdrEventAsync\(AdrEventContext, CancellationToken\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken) 'AdrPlus\.Abstractions\.IAdrPlugin\.OnAdrEventAsync\(AdrPlus\.Abstractions\.AdrEventContext, System\.Threading\.CancellationToken\)') must be reentrant\. [InitializeAsync\(IPluginContext, IPluginConfiguration, CancellationToken\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken) 'AdrPlus\.Abstractions\.IAdrPlugin\.InitializeAsync\(AdrPlus\.Abstractions\.IPluginContext, AdrPlus\.Abstractions\.IPluginConfiguration, System\.Threading\.CancellationToken\)') is called lazily: only the
first time, in this process, that an event this plugin subscribes to is about to be dispatched\.
### Properties

<a name='AdrPlus.Abstractions.IAdrPlugin.Name'></a>

## IAdrPlugin\.Name Property

Gets the plugin's name\. Must match the `name` declared in its `plugin.json` manifest\.

```csharp
string Name { get; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='AdrPlus.Abstractions.IAdrPlugin.Version'></a>

## IAdrPlugin\.Version Property

Gets the plugin's version\. Must match the `version` declared in its `plugin.json` manifest\.

```csharp
string Version { get; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')
### Methods

<a name='AdrPlus.Abstractions.IAdrPlugin.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken)'></a>

## IAdrPlugin\.InitializeAsync\(IPluginContext, IPluginConfiguration, CancellationToken\) Method

Called once, lazily, before the first subscribed event is dispatched to this plugin in this process\.
If this throws, the host skips this plugin for the rest of the run and logs a permanent\-failure warning —
no event dispatched here is queued for retry\.

```csharp
System.Threading.Tasks.Task InitializeAsync(AdrPlus.Abstractions.IPluginContext context, AdrPlus.Abstractions.IPluginConfiguration config, System.Threading.CancellationToken ct);
```
#### Parameters

<a name='AdrPlus.Abstractions.IAdrPlugin.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken).context'></a>

`context` [IPluginContext](AdrPlus.Abstractions.IPluginContext.md 'AdrPlus\.Abstractions\.IPluginContext')

<a name='AdrPlus.Abstractions.IAdrPlugin.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken).config'></a>

`config` [IPluginConfiguration](AdrPlus.Abstractions.IPluginConfiguration.md 'AdrPlus\.Abstractions\.IPluginConfiguration')

<a name='AdrPlus.Abstractions.IAdrPlugin.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken).ct'></a>

`ct` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')

<a name='AdrPlus.Abstractions.IAdrPlugin.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken)'></a>

## IAdrPlugin\.OnAdrEventAsync\(AdrEventContext, CancellationToken\) Method

Reacts to an ADR lifecycle event\. Must never throw for control flow — return a [PluginResult](AdrPlus.Abstractions.PluginResult.md 'AdrPlus\.Abstractions\.PluginResult')
with [Failed](AdrPlus.Abstractions.PluginResultStatus.md#AdrPlus.Abstractions.PluginResultStatus.Failed 'AdrPlus\.Abstractions\.PluginResultStatus\.Failed') instead\. Must treat unknown/future [AdrEventType](AdrPlus.Abstractions.AdrEventType.md 'AdrPlus\.Abstractions\.AdrEventType')
values as [Skipped](AdrPlus.Abstractions.PluginResultStatus.md#AdrPlus.Abstractions.PluginResultStatus.Skipped 'AdrPlus\.Abstractions\.PluginResultStatus\.Skipped')\.

```csharp
System.Threading.Tasks.Task<AdrPlus.Abstractions.PluginResult> OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext context, System.Threading.CancellationToken ct);
```
#### Parameters

<a name='AdrPlus.Abstractions.IAdrPlugin.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken).context'></a>

`context` [AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext')

<a name='AdrPlus.Abstractions.IAdrPlugin.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken).ct'></a>

`ct` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[PluginResult](AdrPlus.Abstractions.PluginResult.md 'AdrPlus\.Abstractions\.PluginResult')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')

<a name='AdrPlus.Abstractions.IAdrPlugin.ShouldHandle(AdrPlus.Abstractions.AdrEventContext)'></a>

## IAdrPlugin\.ShouldHandle\(AdrEventContext\) Method

Cheap, synchronous, declarative filter allowing the host to skip invoking [OnAdrEventAsync\(AdrEventContext, CancellationToken\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken) 'AdrPlus\.Abstractions\.IAdrPlugin\.OnAdrEventAsync\(AdrPlus\.Abstractions\.AdrEventContext, System\.Threading\.CancellationToken\)') entirely\.

```csharp
bool ShouldHandle(AdrPlus.Abstractions.AdrEventContext context);
```
#### Parameters

<a name='AdrPlus.Abstractions.IAdrPlugin.ShouldHandle(AdrPlus.Abstractions.AdrEventContext).context'></a>

`context` [AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext')

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')