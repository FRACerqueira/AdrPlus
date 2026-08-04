<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')

## AdrEventContext Class

Immutable event payload delivered to a plugin's [OnAdrEventAsync\(AdrEventContext, CancellationToken\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.OnAdrEventAsync(AdrPlus.Abstractions.AdrEventContext,System.Threading.CancellationToken) 'AdrPlus\.Abstractions\.IAdrPlugin\.OnAdrEventAsync\(AdrPlus\.Abstractions\.AdrEventContext, System\.Threading\.CancellationToken\)')\.

```csharp
public sealed record AdrEventContext : System.IEquatable<AdrPlus.Abstractions.AdrEventContext>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → AdrEventContext

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')
### Properties

<a name='AdrPlus.Abstractions.AdrEventContext.Adr'></a>

## AdrEventContext\.Adr Property

Gets the snapshot of the ADR this event concerns\.

```csharp
public AdrPlus.Abstractions.Domain.AdrRecordSnapshot Adr { get; init; }
```

#### Property Value
[AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot')

<a name='AdrPlus.Abstractions.AdrEventContext.AdrFilePath'></a>

## AdrEventContext\.AdrFilePath Property

Gets the absolute path of the ADR's `.md` file\.

```csharp
public string AdrFilePath { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='AdrPlus.Abstractions.AdrEventContext.CorrelationId'></a>

## AdrEventContext\.CorrelationId Property

Gets the correlation id for this dispatch, for cross\-referencing plugin logs with the host's file log\.

```csharp
public string CorrelationId { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='AdrPlus.Abstractions.AdrEventContext.EventType'></a>

## AdrEventContext\.EventType Property

Gets the lifecycle event that triggered this dispatch\.

```csharp
public AdrPlus.Abstractions.AdrEventType EventType { get; init; }
```

#### Property Value
[AdrEventType](AdrPlus.Abstractions.AdrEventType.md 'AdrPlus\.Abstractions\.AdrEventType')

<a name='AdrPlus.Abstractions.AdrEventContext.GetAdrRenderedContent'></a>

## AdrEventContext\.GetAdrRenderedContent Property

Gets a delegate that renders the ADR's full Markdown content on demand\.

```csharp
public System.Func<string> GetAdrRenderedContent { get; init; }
```

#### Property Value
[System\.Func&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-1 'System\.Func\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-1 'System\.Func\`1')

### Remarks
Lazy by design: rendering only happens if a plugin's `subscribedEvents`/[ShouldHandle\(AdrEventContext\)](AdrPlus.Abstractions.IAdrPlugin.md#AdrPlus.Abstractions.IAdrPlugin.ShouldHandle(AdrPlus.Abstractions.AdrEventContext) 'AdrPlus\.Abstractions\.IAdrPlugin\.ShouldHandle\(AdrPlus\.Abstractions\.AdrEventContext\)')
filter actually decides to handle the event, so un\-subscribed events stay cheap to dispatch\.

<a name='AdrPlus.Abstractions.AdrEventContext.IsReplay'></a>

## AdrEventContext\.IsReplay Property

Gets whether this dispatch is a replay \(e\.g\. from `adrplus sync --backfill`\) rather than a live, first\-time event\.

```csharp
public bool IsReplay { get; init; }
```

#### Property Value
[System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

<a name='AdrPlus.Abstractions.AdrEventContext.Repo'></a>

## AdrEventContext\.Repo Property

Gets the snapshot of the repository configuration relevant to plugins\.

```csharp
public AdrPlus.Abstractions.Domain.RepoInfoSnapshot Repo { get; init; }
```

#### Property Value
[RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot')