<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## IPluginConfiguration Interface

Typed, read\-only access to the plugin's own `plugin.json``settings` object\.

```csharp
public interface IPluginConfiguration
```

### Remarks
Settings are plain, non\-secret configuration \(base URLs, space keys, etc\.\) — `plugin.json` may be
committed to the repo, so credential values must never be stored here\.
### Methods

<a name='AdrPlus.Abstractions.IPluginConfiguration.GetValue_T_(string)'></a>

## IPluginConfiguration\.GetValue\<T\>\(string\) Method

Gets the value for [key](AdrPlus.Abstractions.IPluginConfiguration.md#AdrPlus.Abstractions.IPluginConfiguration.GetValue_T_(string).key 'AdrPlus\.Abstractions\.IPluginConfiguration\.GetValue\<T\>\(string\)\.key') converted to [T](AdrPlus.Abstractions.IPluginConfiguration.md#AdrPlus.Abstractions.IPluginConfiguration.GetValue_T_(string).T 'AdrPlus\.Abstractions\.IPluginConfiguration\.GetValue\<T\>\(string\)\.T'), or `default` if the key is absent\.

```csharp
T? GetValue<T>(string key);
```
#### Type parameters

<a name='AdrPlus.Abstractions.IPluginConfiguration.GetValue_T_(string).T'></a>

`T`
#### Parameters

<a name='AdrPlus.Abstractions.IPluginConfiguration.GetValue_T_(string).key'></a>

`key` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

#### Returns
[T](AdrPlus.Abstractions.IPluginConfiguration.md#AdrPlus.Abstractions.IPluginConfiguration.GetValue_T_(string).T 'AdrPlus\.Abstractions\.IPluginConfiguration\.GetValue\<T\>\(string\)\.T')