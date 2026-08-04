<img src="https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png" width="120" alt="AdrPlus" />

#### [AdrPlus\.Abstractions](AdrPlus.Abstractions.md 'AdrPlus\.Abstractions')
### [AdrPlus\.Abstractions\.Testing](AdrPlus.Abstractions.Testing.md 'AdrPlus\.Abstractions\.Testing')

## AdrRecordSnapshotFactory Class

Builds a valid [AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot') for use in a plugin author's own unit tests, without
requiring every `required` field to be filled in by hand\.

```csharp
public static class AdrRecordSnapshotFactory
```

Inheritance [System\.Object](https://learn.microsoft.com/en-us/dotnet/api/system.object 'System\.Object') → AdrRecordSnapshotFactory
### Methods

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_)'></a>

## AdrRecordSnapshotFactory\.Create\(int, int, Nullable\<int\>, string, string, string, AdrStatus, AdrStatus, AdrStatus, Nullable\<DateTime\>, Nullable\<DateTime\>, Nullable\<DateTime\>, Nullable\<int\>\) Method

Creates an [AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot') with sensible defaults, overriding only the parameters
a test cares about\.

```csharp
public static AdrPlus.Abstractions.Domain.AdrRecordSnapshot Create(int number=1, int version=1, System.Nullable<int> revision=null, string title="Sample decision", string domain="General", string scope="core", AdrPlus.Abstractions.Domain.AdrStatus statusCreate=AdrPlus.Abstractions.Domain.AdrStatus.Proposed, AdrPlus.Abstractions.Domain.AdrStatus statusUpdate=AdrPlus.Abstractions.Domain.AdrStatus.Unknown, AdrPlus.Abstractions.Domain.AdrStatus statusChange=AdrPlus.Abstractions.Domain.AdrStatus.Unknown, System.Nullable<System.DateTime> createRef=null, System.Nullable<System.DateTime> updateRef=null, System.Nullable<System.DateTime> changeRef=null, System.Nullable<int> superseded=null);
```
#### Parameters

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).number'></a>

`number` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The ADR's stable sequence number\. Defaults to `1`\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).version'></a>

`version` [System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')

The ADR's version number\. Defaults to `1`\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).revision'></a>

`revision` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The ADR's revision number, if any\. Defaults to [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).title'></a>

`title` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The ADR's title\. Defaults to `"Sample decision"`\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).domain'></a>

`domain` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The ADR's domain\. Defaults to `"General"`\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).scope'></a>

`scope` [System\.String](https://learn.microsoft.com/en-us/dotnet/api/system.string 'System\.String')

The ADR's scope\. Defaults to `"core"`\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).statusCreate'></a>

`statusCreate` [AdrStatus](AdrPlus.Abstractions.Domain.AdrStatus.md 'AdrPlus\.Abstractions\.Domain\.AdrStatus')

The status when the ADR was created\. Defaults to [Proposed](AdrPlus.Abstractions.Domain.AdrStatus.md#AdrPlus.Abstractions.Domain.AdrStatus.Proposed 'AdrPlus\.Abstractions\.Domain\.AdrStatus\.Proposed')\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).statusUpdate'></a>

`statusUpdate` [AdrStatus](AdrPlus.Abstractions.Domain.AdrStatus.md 'AdrPlus\.Abstractions\.Domain\.AdrStatus')

The status after an update operation\. Defaults to [Unknown](AdrPlus.Abstractions.Domain.AdrStatus.md#AdrPlus.Abstractions.Domain.AdrStatus.Unknown 'AdrPlus\.Abstractions\.Domain\.AdrStatus\.Unknown')\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).statusChange'></a>

`statusChange` [AdrStatus](AdrPlus.Abstractions.Domain.AdrStatus.md 'AdrPlus\.Abstractions\.Domain\.AdrStatus')

The status after a change operation\. Defaults to [Unknown](AdrPlus.Abstractions.Domain.AdrStatus.md#AdrPlus.Abstractions.Domain.AdrStatus.Unknown 'AdrPlus\.Abstractions\.Domain\.AdrStatus\.Unknown')\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).createRef'></a>

`createRef` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.DateTime](https://learn.microsoft.com/en-us/dotnet/api/system.datetime 'System\.DateTime')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The date reference when the ADR was created\. Defaults to [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).updateRef'></a>

`updateRef` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.DateTime](https://learn.microsoft.com/en-us/dotnet/api/system.datetime 'System\.DateTime')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The date reference when the ADR was updated\. Defaults to [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).changeRef'></a>

`changeRef` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.DateTime](https://learn.microsoft.com/en-us/dotnet/api/system.datetime 'System\.DateTime')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The date reference when the ADR status was changed\. Defaults to [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

<a name='AdrPlus.Abstractions.Testing.AdrRecordSnapshotFactory.Create(int,int,System.Nullable_int_,string,string,string,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,AdrPlus.Abstractions.Domain.AdrStatus,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_System.DateTime_,System.Nullable_int_).superseded'></a>

`superseded` [System\.Nullable&lt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')[System\.Int32](https://learn.microsoft.com/en-us/dotnet/api/system.int32 'System\.Int32')[&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.nullable-1 'System\.Nullable\`1')

The sequence number of the ADR this one supersedes, if any\. Defaults to [null](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/null 'https://docs\.microsoft\.com/en\-us/dotnet/csharp/language\-reference/keywords/null')\.

#### Returns
[AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot')  
A fully populated, valid [AdrRecordSnapshot](AdrPlus.Abstractions.Domain.AdrRecordSnapshot.md 'AdrPlus\.Abstractions\.Domain\.AdrRecordSnapshot')\.