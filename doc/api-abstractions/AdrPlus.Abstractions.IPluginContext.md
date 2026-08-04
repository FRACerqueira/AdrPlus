<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## IPluginContext Interface

Host\-provided services given to a plugin at [InitializeAsync\(IPluginContext, IPluginConfiguration, CancellationToken\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.InitializeAsync(AdrPlus.Abstractions.IPluginContext,AdrPlus.Abstractions.IPluginConfiguration,System.Threading.CancellationToken) 'AdrPlus\.Abstractions\.IAdrPlugin\.InitializeAsync\(AdrPlus\.Abstractions\.IPluginContext, AdrPlus\.Abstractions\.IPluginConfiguration, System\.Threading\.CancellationToken\)')\. Provides no secrets —
credential resolution is entirely the plugin's own responsibility\.

```csharp
public interface IPluginContext
```
### Properties

<a name='AdrPlus.Abstractions.IPluginContext.Logger'></a>

## IPluginContext\.Logger Property

Gets the logger the plugin should use, unified with the host's own file log\.

```csharp
AdrPlus.Abstractions.IPluginLogger Logger { get; }
```

#### Property Value
[IPluginLogger](AdrPlus.Abstractions.IPluginLogger.md 'AdrPlus\.Abstractions\.IPluginLogger')