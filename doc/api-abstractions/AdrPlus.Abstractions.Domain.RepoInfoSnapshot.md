<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions\.Domain](AdrPlus.Abstractions.Domain.md 'AdrPlus\.Abstractions\.Domain')

## RepoInfoSnapshot Class

Immutable public snapshot of the parts of the repository configuration relevant to plugins,
exposed via [Repo](AdrPlus.Abstractions.AdrEventContext.md#AdrPlus.Abstractions.AdrEventContext.Repo 'AdrPlus\.Abstractions\.AdrEventContext\.Repo')\.

```csharp
public sealed record RepoInfoSnapshot : System.IEquatable<AdrPlus.Abstractions.Domain.RepoInfoSnapshot>
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → RepoInfoSnapshot

Implements [System\.IEquatable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')[RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1 'System\.IEquatable\`1')

### Remarks
Deliberately excludes filename/header\-formatting settings \(prefix, separators, digit lengths, case
transform, header label strings\) — those exist to build the `.md` filename and header the host
already writes, not information a plugin needs to react to an event\.
### Properties

<a name='AdrPlus.Abstractions.Domain.RepoInfoSnapshot.FolderAdr'></a>

## RepoInfoSnapshot\.FolderAdr Property

Gets the folder path where ADR files are stored, e\.g\., "docs/adr"\.

```csharp
public string FolderAdr { get; init; }
```

#### Property Value
[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

<a name='AdrPlus.Abstractions.Domain.RepoInfoSnapshot.Scopes'></a>

## RepoInfoSnapshot\.Scopes Property

Gets the configured scopes for organizing ADRs\.

```csharp
public System.Collections.Generic.IReadOnlyList<string> Scopes { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyList&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlylist-1 'System\.Collections\.Generic\.IReadOnlyList\`1')

<a name='AdrPlus.Abstractions.Domain.RepoInfoSnapshot.StatusMapping'></a>

## RepoInfoSnapshot\.StatusMapping Property

Gets the mapping between [AdrStatus](AdrPlus.Abstractions.Domain.AdrStatus.md 'AdrPlus\.Abstractions\.Domain\.AdrStatus') values and their configured, localized string representations\.

```csharp
public System.Collections.Generic.IReadOnlyDictionary<AdrPlus.Abstractions.Domain.AdrStatus,string> StatusMapping { get; init; }
```

#### Property Value
[System\.Collections\.Generic\.IReadOnlyDictionary&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlydictionary-2 'System\.Collections\.Generic\.IReadOnlyDictionary\`2')[AdrStatus](AdrPlus.Abstractions.Domain.AdrStatus.md 'AdrPlus\.Abstractions\.Domain\.AdrStatus')[,](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlydictionary-2 'System\.Collections\.Generic\.IReadOnlyDictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlydictionary-2 'System\.Collections\.Generic\.IReadOnlyDictionary\`2')