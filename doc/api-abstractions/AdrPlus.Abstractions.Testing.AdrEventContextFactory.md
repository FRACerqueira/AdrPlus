<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions\.Testing](AdrPlus.Abstractions.Testing.md 'AdrPlus\.Abstractions\.Testing')

## AdrEventContextFactory Class

Builds a valid [AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext') for use in a plugin author's own unit tests, without
requiring every `required` field — including the nested [AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot') and
[RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot') — to be filled in by hand\.

```csharp
public static class AdrEventContextFactory
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → AdrEventContextFactory

### Remarks
[AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext') is a [record](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/record 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/record'): once built, use a `with` expression
            for any further one\-off overrides [Create\(AdrEventType, bool, AdrRecordSnapshot, string, string, Func&lt;string&gt;, RepoInfoSnapshot, string\)](AdrPlus.Abstractions.Testing.AdrEventContextFactory.md#AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string) 'AdrPlus\.Abstractions\.Testing\.AdrEventContextFactory\.Create\(AdrPlus\.Abstractions\.AdrEventType, bool, AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot, string, string, System\.Func\<string\>, AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot, string\)')'s parameters don't cover\.
### Methods

<a name='AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string)'></a>

## AdrEventContextFactory\.Create\(AdrEventType, bool, AdrRecordSnapshot, string, string, Func\<string\>, RepoInfoSnapshot, string\) Method

Creates an [AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext') with sensible defaults, overriding only the parameters
a test cares about\.

```csharp
public static AdrPlus.Abstractions.AdrEventContext Create(AdrPlus.Abstractions.AdrEventType eventType=AdrPlus.Abstractions.AdrEventType.Approved, bool isReplay=false, AdrPlus.Abstractions.Domain.AdrRecordSnapshot? adr=null, string adrFilePath="docs/adr/ADR0001V01-sample-decision.md", string renderedContent="# Sample decision\n\nSample content.", System.Func<string>? getAdrRenderedContent=null, AdrPlus.Abstractions.Domain.RepoInfoSnapshot? repo=null, string? correlationId=null);
```
#### Parameters

<a name='AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).eventType'></a>

`eventType` [AdrEventType](AdrPlus.Abstractions.AdrEventType.md 'AdrPlus\.Abstractions\.AdrEventType')

The lifecycle event that triggered this dispatch\. Defaults to [Approved](AdrPlus.Abstractions.AdrEventType.md#AdrPlus.Abstractions.AdrEventType.Approved 'AdrPlus\.Abstractions\.AdrEventType\.Approved')\.

<a name='AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).isReplay'></a>

`isReplay` [System\.Boolean](https://learn.microsoft.com/en-us/dotnet/api/system.boolean 'System\.Boolean')

Whether this dispatch is a replay rather than a live event\. Defaults to [false](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/bool 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/builtin\-types/bool')\.

<a name='AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).adr'></a>

`adr` [AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot')

The ADR snapshot this event concerns\. Defaults to [Create\(int, int, Nullable&lt;int&gt;, string, string, string, AdrStatus, AdrStatus, AdrStatus, Nullable&lt;DateTime&gt;, Nullable&lt;DateTime&gt;, Nullable&lt;DateTime&gt;, Nullable&lt;int&gt;\)](AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.md#AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_) 'AdrPlus\.Abstractions\.Testing\.AdrRecordSnapshotFactory\.Create\(int, int, System\.Nullable\<int\>, string, string, string, AdrPlus\.Abstractions\.Domain\.AdrStatus, AdrPlus\.Abstractions\.Domain\.AdrStatus, AdrPlus\.Abstractions\.Domain\.AdrStatus, System\.Nullable\<System\.DateTime\>, System\.Nullable\<System\.DateTime\>, System\.Nullable\<System\.DateTime\>, System\.Nullable\<int\>\)')'s
own defaults\.

<a name='AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).adrFilePath'></a>

`adrFilePath` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The absolute path of the ADR's `.md` file\. Defaults to a sample path\.

<a name='AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).renderedContent'></a>

`renderedContent` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The ADR's rendered Markdown content, wrapped in a delegate to satisfy [GetAdrRenderedContent](AdrPlus.Abstractions.AdrEventContext.md#AdrPlus.Abstractions.AdrEventContext.GetAdrRenderedContent 'AdrPlus\.Abstractions\.AdrEventContext\.GetAdrRenderedContent')\.
Defaults to a short sample document\. Ignored if [getAdrRenderedContent](AdrPlus.Abstractions.Testing.AdrEventContextFactory.md#AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).getAdrRenderedContent 'AdrPlus\.Abstractions\.Testing\.AdrEventContextFactory\.Create\(AdrPlus\.Abstractions\.AdrEventType, bool, AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot, string, string, System\.Func\<string\>, AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot, string\)\.getAdrRenderedContent') is supplied\.

<a name='AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).getAdrRenderedContent'></a>

`getAdrRenderedContent` [System\.Func&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-1 'System\.Func\`1')[System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.func-1 'System\.Func\`1')

Overrides [renderedContent](AdrPlus.Abstractions.Testing.AdrEventContextFactory.md#AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).renderedContent 'AdrPlus\.Abstractions\.Testing\.AdrEventContextFactory\.Create\(AdrPlus\.Abstractions\.AdrEventType, bool, AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot, string, string, System\.Func\<string\>, AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot, string\)\.renderedContent') when a test needs the delegate itself to be lazy,
throw, or be invoked a specific number of times\.

<a name='AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).repo'></a>

`repo` [RepoInfoSnapshot](AdrPlus.Abstractions.Domain.RepoInfoSnapshot.md 'AdrPlus\.Abstractions\.Domain\.RepoInfoSnapshot')

The repository configuration snapshot\. Defaults to [Create\(string, IReadOnlyList&lt;string&gt;, IReadOnlyDictionary&lt;AdrStatus,string&gt;\)](AdrPlus.Abstractions.Testing.RepoInfoSnapshotFactory.md#AdrPlus.Abstractions.Testing.RepoInfoSnapshotFactory.Create(string,System.Collections.Generic.IReadOnlyList_string_,System.Collections.Generic.IReadOnlyDictionary_AdrPlus.Abstractions.Domain.AdrStatus,string_) 'AdrPlus\.Abstractions\.Testing\.RepoInfoSnapshotFactory\.Create\(string, System\.Collections\.Generic\.IReadOnlyList\<string\>, System\.Collections\.Generic\.IReadOnlyDictionary\<AdrPlus\.Abstractions\.Domain\.AdrStatus,string\>\)')'s
own defaults\.

<a name='AdrPlus.Abstractions.Testing.AdrEventContextFactory.Create(AdrPlus.Abstractions.AdrEventType,bool,AdrPlus.Abstractions.Domain.AdrRecordSnapshot,string,string,System.Func_string_,AdrPlus.Abstractions.Domain.RepoInfoSnapshot,string).correlationId'></a>

`correlationId` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The dispatch correlation id\. Defaults to a new GUID\.

#### Returns
[AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext')  
A fully populated, valid [AdrEventContext](AdrPlus.Abstractions.AdrEventContext.md 'AdrPlus\.Abstractions\.AdrEventContext')\.