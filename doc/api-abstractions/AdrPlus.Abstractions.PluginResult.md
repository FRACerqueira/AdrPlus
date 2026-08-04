<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## PluginResult Class

The structured outcome a plugin returns from [OnAdrEventAsync\(AdrEventContext, CancellationToken\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken) 'AdrPlus\.Abstractions\.IAdrPlugin\.OnAdrEventAsync\(AdrPlus\.Abstractions\.AdrEventContext, System\.Threading\.CancellationToken\)')\.

```csharp
public sealed record PluginResult : System.IEquatable<AdrPlus.Abstractions.PluginResult>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → PluginResult

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[PluginResult](AdrPlus.Abstractions.PluginResult.md 'AdrPlus\.Abstractions\.PluginResult')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='AdrPlus.Abstractions.PluginResult.ExternalKey'></a>

## PluginResult\.ExternalKey Property

Gets an optional external identifier \(e\.g\. a Confluence page id\) the plugin can use for idempotent upserts on retry/replay\.

```csharp
public string? ExternalKey { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='AdrPlus.Abstractions.PluginResult.IsRetryable'></a>

## PluginResult\.IsRetryable Property

Gets whether a [Failed](AdrPlus.Abstractions.PluginResultStatus.md#AdrPlus.Abstractions.PluginResultStatus.Failed 'AdrPlus\.Abstractions\.PluginResultStatus\.Failed') outcome is worth retrying\.
Set to `false` for permanent/configuration failures \(e\.g\. invalid credentials\) that would fail identically on every retry —
the host will not queue those for background re\-drive\.

```csharp
public bool IsRetryable { get; init; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='AdrPlus.Abstractions.PluginResult.Message'></a>

## PluginResult\.Message Property

Gets an optional human\-readable message, surfaced in host warnings and file logs on failure\.

```csharp
public string? Message { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='AdrPlus.Abstractions.PluginResult.Status'></a>

## PluginResult\.Status Property

Gets the outcome of the plugin's reaction to the event\.

```csharp
public AdrPlus.Abstractions.PluginResultStatus Status { get; init; }
```

#### Property Value
[PluginResultStatus](AdrPlus.Abstractions.PluginResultStatus.md 'AdrPlus\.Abstractions\.PluginResultStatus')