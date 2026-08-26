<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions\.Testing](AdrPlus.Abstractions.Testing.md 'AdrPlus\.Abstractions\.Testing')

## RepoInfoSnapshotFactory Class

Builds a valid [RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot') for use in a plugin author's own unit tests, without
requiring every `required` field to be filled in by hand\.

```csharp
public static class RepoInfoSnapshotFactory
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → RepoInfoSnapshotFactory
### Methods

<a name='AdrPlus.Abstractions.Testing.RepoInfoSnapshotFactory.Create(string,System.Collections.Generic.IReadOnlyDictionary_AdrPlus.Abstractions.Domain.AdrStatus,string_)'></a>

## RepoInfoSnapshotFactory\.Create\(string, IReadOnlyDictionary\<AdrStatus,string\>\) Method

Creates a [RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot') with sensible defaults, overriding only the parameters
a test cares about\.

```csharp
public static AdrPlus.Abstractions.Domain.RepoInfoSnapshot Create(string folderAdr="docs/adr", System.Collections.Generic.IReadOnlyDictionary<AdrPlus.Abstractions.Domain.AdrStatus,string>? statusMapping=null);
```
#### Parameters

<a name='AdrPlus.Abstractions.Testing.RepoInfoSnapshotFactory.Create(string,System.Collections.Generic.IReadOnlyDictionary_AdrPlus.Abstractions.Domain.AdrStatus,string_).folderAdr'></a>

`folderAdr` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The folder path where ADR files are stored\. Defaults to `"docs/adr"`\.

<a name='AdrPlus.Abstractions.Testing.RepoInfoSnapshotFactory.Create(string,System.Collections.Generic.IReadOnlyDictionary_AdrPlus.Abstractions.Domain.AdrStatus,string_).statusMapping'></a>

`statusMapping` [System\.Collections\.Generic\.IReadOnlyDictionary&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlydictionary-2 'System\.Collections\.Generic\.IReadOnlyDictionary\`2')[AdrStatus](AdrPlus.Abstractions.Domain.AdrStatus.md 'AdrPlus\.Abstractions\.Domain\.AdrStatus')[,](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlydictionary-2 'System\.Collections\.Generic\.IReadOnlyDictionary\`2')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.ireadonlydictionary-2 'System\.Collections\.Generic\.IReadOnlyDictionary\`2')

The mapping between [AdrStatus](AdrPlus.Abstractions.Domain.AdrStatus.md 'AdrPlus\.Abstractions\.Domain\.AdrStatus') values and their configured string representations\.
Defaults to each [AdrStatus](AdrPlus.Abstractions.Domain.AdrStatus.md 'AdrPlus\.Abstractions\.Domain\.AdrStatus') value mapped to its own enum name\.

#### Returns
[RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot')  
A fully populated, valid [RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot')\.