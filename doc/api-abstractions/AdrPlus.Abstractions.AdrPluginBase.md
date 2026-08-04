<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## AdrPluginBase Class

Optional convenience base class implementing [IAdrPlugin](AdrPlus.Abstractions.IAdrPlugin.md 'AdrPlus\.Abstractions\.IAdrPlugin')\. Shields [AdrPlus\.Abstractions\.AdrPluginBase\.HandleAsync\(AdrPlus\.Abstractions\.AdrEventContext,System\.Threading\.CancellationToken\)](https://learn.microsoft.com/en-us/dotnet/api/adrplus.abstractions.adrpluginbase.handleasync#adrplus-abstractions-adrpluginbase-handleasync(adrplus-abstractions-adreventcontext-system-threading-cancellationtoken) 'AdrPlus\.Abstractions\.AdrPluginBase\.HandleAsync\(AdrPlus\.Abstractions\.AdrEventContext,System\.Threading\.CancellationToken\)')
exceptions into a [Failed](AdrPlus.Abstractions.PluginResultStatus.md#AdrPlus.Abstractions.PluginResultStatus.Failed 'AdrPlus\.Abstractions\.PluginResultStatus\.Failed') result and exposes [AdrPlus\.Abstractions\.AdrPluginBase\.Success\(System\.String\)](https://learn.microsoft.com/en-us/dotnet/api/adrplus.abstractions.adrpluginbase.success#adrplus-abstractions-adrpluginbase-success(system-string) 'AdrPlus\.Abstractions\.AdrPluginBase\.Success\(System\.String\)')/[AdrPlus\.Abstractions\.AdrPluginBase\.Skip\(System\.String\)](https://learn.microsoft.com/en-us/dotnet/api/adrplus.abstractions.adrpluginbase.skip#adrplus-abstractions-adrpluginbase-skip(system-string) 'AdrPlus\.Abstractions\.AdrPluginBase\.Skip\(System\.String\)')/[AdrPlus\.Abstractions\.AdrPluginBase\.Fail\(System\.String,System\.Boolean\)](https://learn.microsoft.com/en-us/dotnet/api/adrplus.abstractions.adrpluginbase.fail#adrplus-abstractions-adrpluginbase-fail(system-string-system-boolean) 'AdrPlus\.Abstractions\.AdrPluginBase\.Fail\(System\.String,System\.Boolean\)')
helpers\. Plugin authors may ignore this and implement [IAdrPlugin](AdrPlus.Abstractions.IAdrPlugin.md 'AdrPlus\.Abstractions\.IAdrPlugin') directly instead\.

```csharp
public abstract class AdrPluginBase : AdrPlus.Abstractions.IAdrPlugin, System.IAsyncDisposable
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → AdrPluginBase

Implements [IAdrPlugin](AdrPlus.Abstractions.IAdrPlugin.md 'AdrPlus\.Abstractions\.IAdrPlugin'), [System\.IAsyncDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.iasyncdisposable 'System\.IAsyncDisposable')
### Properties

<a name='AdrPlus.Abstractions.AdrPluginBase.Name'></a>

## AdrPluginBase\.Name Property

Gets the plugin's name\. Must match the `name` declared in its `plugin.json` manifest\.

```csharp
public abstract string Name { get; }
```

Implements [Name](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.Name 'AdrPlus\.Abstractions\.IAdrPlugin\.Name')

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='AdrPlus.Abstractions.AdrPluginBase.Version'></a>

## AdrPluginBase\.Version Property

Gets the plugin's version\. Must match the `version` declared in its `plugin.json` manifest\.

```csharp
public abstract string Version { get; }
```

Implements [Version](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.Version 'AdrPlus\.Abstractions\.IAdrPlugin\.Version')

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')
### Methods

<a name='AdrPlus.Abstractions.AdrPluginBase.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken)'></a>

## AdrPluginBase\.InitializeAsync\(IPluginContext, IPluginConfiguration, CancellationToken\) Method

Called once, lazily, before the first subscribed event is dispatched to this plugin in this process\.
If this throws, the host skips this plugin for the rest of the run and logs a permanent\-failure warning —
no event dispatched here is queued for retry\.

```csharp
public virtual System.Threading.Tasks.Task InitializeAsync(AdrPlus.Abstractions.IPluginContext context, AdrPlus.Abstractions.IPluginConfiguration config, System.Threading.CancellationToken ct);
```
#### Parameters

<a name='AdrPlus.Abstractions.AdrPluginBase.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken).context'></a>

`context` [IPluginContext](AdrPlus.Abstractions.IPluginContext.md 'AdrPlus\.Abstractions\.IPluginContext')

<a name='AdrPlus.Abstractions.AdrPluginBase.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken).config'></a>

`config` [IPluginConfiguration](AdrPlus.Abstractions.IPluginConfiguration.md 'AdrPlus\.Abstractions\.IPluginConfiguration')

<a name='AdrPlus.Abstractions.AdrPluginBase.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken).ct'></a>

`ct` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Implements [InitializeAsync\(IPluginContext, IPluginConfiguration, CancellationToken\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken) 'AdrPlus\.Abstractions\.IAdrPlugin\.InitializeAsync\(AdrPlus\.Abstractions\.IPluginContext, AdrPlus\.Abstractions\.IPluginConfiguration, System\.Threading\.CancellationToken\)')

#### Returns
[System\.Threading\.Tasks\.Task](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task 'System\.Threading\.Tasks\.Task')

<a name='AdrPlus.Abstractions.AdrPluginBase.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken)'></a>

## AdrPluginBase\.OnAdrEventAsync\(AdrEventContext, CancellationToken\) Method

Reacts to an ADR lifecycle event\. Must never throw for control flow — return a [PluginResult](AdrPlus.Abstractions.PluginResult.md 'AdrPlus\.Abstractions\.PluginResult')
with [Failed](AdrPlus.Abstractions.PluginResultStatus.md#AdrPlus.Abstractions.PluginResultStatus.Failed 'AdrPlus\.Abstractions\.PluginResultStatus\.Failed') instead\. Must treat unknown/future [AdrEventType](AdrPlus.Abstractions.AdrEventType.md 'AdrPlus\.Abstractions\.AdrEventType')
values as [Skipped](AdrPlus.Abstractions.PluginResultStatus.md#AdrPlus.Abstractions.PluginResultStatus.Skipped 'AdrPlus\.Abstractions\.PluginResultStatus\.Skipped')\.

```csharp
public System.Threading.Tasks.Task<AdrPlus.Abstractions.PluginResult> OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext context, System.Threading.CancellationToken ct);
```
#### Parameters

<a name='AdrPlus.Abstractions.AdrPluginBase.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken).context'></a>

`context` [AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext')

<a name='AdrPlus.Abstractions.AdrPluginBase.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken).ct'></a>

`ct` [System\.Threading\.CancellationToken](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken 'System\.Threading\.CancellationToken')

Implements [OnAdrEventAsync\(AdrEventContext, CancellationToken\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken) 'AdrPlus\.Abstractions\.IAdrPlugin\.OnAdrEventAsync\(AdrPlus\.Abstractions\.AdrEventContext, System\.Threading\.CancellationToken\)')

#### Returns
[System\.Threading\.Tasks\.Task&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')[PluginResult](AdrPlus.Abstractions.PluginResult.md 'AdrPlus\.Abstractions\.PluginResult')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task-1 'System\.Threading\.Tasks\.Task\`1')

<a name='AdrPlus.Abstractions.AdrPluginBase.ShouldHandle(AdrPlus.Abstractions.AdrEventContext)'></a>

## AdrPluginBase\.ShouldHandle\(AdrEventContext\) Method

Cheap, synchronous, declarative filter allowing the host to skip invoking [OnAdrEventAsync\(AdrEventContext, CancellationToken\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken) 'AdrPlus\.Abstractions\.IAdrPlugin\.OnAdrEventAsync\(AdrPlus\.Abstractions\.AdrEventContext, System\.Threading\.CancellationToken\)') entirely\.

```csharp
public virtual bool ShouldHandle(AdrPlus.Abstractions.AdrEventContext context);
```
#### Parameters

<a name='AdrPlus.Abstractions.AdrPluginBase.ShouldHandle(AdrPlus.Abstractions.AdrEventContext).context'></a>

`context` [AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext')

Implements [ShouldHandle\(AdrEventContext\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.ShouldHandle(AdrPlus.Abstractions.AdrEventContext) 'AdrPlus\.Abstractions\.IAdrPlugin\.ShouldHandle\(AdrPlus\.Abstractions\.AdrEventContext\)')

#### Returns
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')