// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using System.Runtime.InteropServices;

namespace AdrPlus.Tests.Domain;

public class AdrRecordTests
{
    [Fact]
    public void AdrRecord_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var record = new AdrRecord();

        // Assert
        record.StatusCreate.Should().Be(AdrStatus.Proposed);
        record.StatusUpdate.Should().Be(AdrStatus.Unknown);
        record.StatusChange.Should().Be(AdrStatus.Unknown);
        record.CreateRef.Should().BeNull();
        record.UpdateRef.Should().BeNull();
        record.ChangeRef.Should().BeNull();
        record.Superseded.Should().BeNull();
        record.Number.Should().Be(0);
        record.Version.Should().Be(0);
        record.Revision.Should().BeNull();
        record.Title.Should().Be(string.Empty);
        record.Domain.Should().Be(string.Empty);
        record.Scope.Should().Be(string.Empty);
        record.Template.Should().Be(string.Empty);
    }

    #region GetFileName Tests

    [Fact]
    public void GetFileName_WithBasicConfig_GeneratesCorrectFileName()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR-",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 2,
            LenScope = 0,
            Separator = '-',
            CaseTransform = CaseFormat.KebabCase
        };

        var record = new AdrRecord
        {
            Number = 1,
            Title = "Use New Database",
            Version = 1,
            Revision = 0,
            Scope = string.Empty,
            Domain = string.Empty
        };

        // Act
        var fileName = record.GetFileName(config);

        // Assert
        fileName.Should().Be("ADR-0001-use-new-database-V01R00.md");
    }

    [Fact]
    public void GetFileName_WithScopeAndDomain_IncludesInFileName()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR-",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            LenScope = 3,
            Separator = '-',
            CaseTransform = CaseFormat.PascalCase
        };

        var record = new AdrRecord
        {
            Number = 5,
            Title = "Use GraphQL",
            Version = 2,
            Scope = "API",
            Domain = "Backend"
        };

        // Act
        var fileName = record.GetFileName(config);

        // Assert
        fileName.Should().Be("ADR-0005-UseGraphQl-V02-Api-Backend.md");
    }

    [Fact]
    public void GetFileName_WithSuperseded_IncludesSupersedeTag()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR-",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 2,
            LenScope = 0,
            Separator = '-',
            CaseTransform = CaseFormat.KebabCase
        };

        var record = new AdrRecord
        {
            Number = 10,
            Title = "Use PostgreSQL",
            Version = 1,
            Revision = 0,
            Superseded = 9
        };

        // Act
        var fileName = record.GetFileName(config);

        // Assert
        fileName.Should().Be("ADR-0010-use-postgre-sql-V01R00-SUP0009.md");
    }

    [Fact]
    public void GetFileName_WithNoRevision_ExcludesRevision()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR-",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            LenScope = 0,
            Separator = '-',
            CaseTransform = CaseFormat.PascalCase
        };

        var record = new AdrRecord
        {
            Number = 3,
            Title = "UseRedis",
            Version = 1
        };

        // Act
        var fileName = record.GetFileName(config);

        // Assert
        fileName.Should().Be("ADR-0003-UseRedis-V01.md");
    }

    [Fact]
    public void GetFileName_WithDifferentCaseFormats_AppliesTransformation()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR-",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            LenScope = 0,
            Separator = '-',
            CaseTransform = CaseFormat.SnakeCase
        };

        var record = new AdrRecord
        {
            Number = 1,
            Title = "Use New Database",
            Version = 1
        };

        // Act
        var fileName = record.GetFileName(config);

        // Assert
        fileName.Should().Be("ADR-0001-use_new_database-V01.md");
    }

    [Fact]
    public void GetFileName_WithCamelCase_GeneratesCorrectFileName()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR-",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            LenScope = 0,
            Separator = '-',
            CaseTransform = CaseFormat.CamelCase
        };

        var record = new AdrRecord
        {
            Number = 2,
            Title = "Use New API",
            Version = 1
        };

        // Act
        var fileName = record.GetFileName(config);

        // Assert
        fileName.Should().Be("ADR-0002-useNewApi-V01.md");
    }

    [Fact]
    public void GetFileName_WithScopeOnly_IncludesScopeInFileName()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR-",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            LenScope = 5,
            Separator = '-',
            CaseTransform = CaseFormat.PascalCase
        };

        var record = new AdrRecord
        {
            Number = 7,
            Title = "UseCache",
            Version = 1,
            Scope = "Performance",
            Domain = string.Empty
        };

        // Act
        var fileName = record.GetFileName(config);

        // Assert
        fileName.Should().Be("ADR-0007-UseCache-V01-Perfo.md");
    }

    #endregion

    #region GetHeader Tests

    [Fact]
    public void GetHeader_WithBasicRecord_GeneratesValidHeader()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            HeaderDisclaimer = "Test Header",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var record = new AdrRecord
        {
            Title = "Test ADR",
            Version = 1,
            StatusCreate = AdrStatus.Proposed
        };

        // Act
        var header = record.GetHeader(config);

        // Assert
        header.Should().NotBeNullOrEmpty();
        header.Should().Contain("<!-- Test Header");
        header.Should().Contain("|Adr-Plus");
        header.Should().Contain("|Test ADR|");
        header.Should().Contain("|Version|01|");
        header.Should().Contain("Proposed");
    }

    [Fact]
    public void GetHeader_WithCreatedDate_IncludesDateInHeader()
    {
        // Arrange
        var testDate = new DateTime(2025, 04, 17);
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            HeaderDisclaimer = "Test",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var record = new AdrRecord
        {
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Proposed,
            CreateRef = testDate
        };

        // Act
        var header = record.GetHeader(config);

        // Assert
        header.Should().Contain("2025-04-17");
    }

    [Fact]
    public void GetHeader_WithSuperseded_IncludesSupersedeFileReference()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            HeaderDisclaimer = "Test",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var testDate = new DateTime(2025, 04, 18);
        var record = new AdrRecord
        {
            Title = "Superseded ADR",
            Version = 1,
            StatusChange = AdrStatus.Superseded,
            ChangeRef = testDate
        };

        // Act
        var header = record.GetHeader(config, "ADR-0002.md", false);

        // Assert
        header.Should().Contain("ADR-0002.md");
        header.Should().Contain("Superseded");
    }

    [Fact]
    public void GetHeader_WithMigrated_IncludesMigratedComment()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            HeaderDisclaimer = "Test",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var record = new AdrRecord
        {
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Proposed
        };

        // Act
        var header = record.GetHeader(config, null, true);

        // Assert
        header.Should().Contain("<!-- Migrated -->");
    }

    [Fact]
    public void GetHeader_WithoutMigrated_ExcludesMigratedComment()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            HeaderDisclaimer = "Test",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var record = new AdrRecord
        {
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Proposed
        };

        // Act
        var header = record.GetHeader(config, null, false);

        // Assert
        header.Should().NotContain("<!-- Migrated -->");
    }

    [Fact]
    public void GetHeader_WithRevision_IncludesRevisionInHeader()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 2,
            Separator = '-',
            HeaderDisclaimer = "Test",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var record = new AdrRecord
        {
            Title = "Test",
            Version = 1,
            Revision = 3,
            StatusCreate = AdrStatus.Proposed
        };

        // Act
        var header = record.GetHeader(config);

        // Assert
        header.Should().Contain("|Revision|03|");
    }

    [Fact]
    public void GetHeader_WithScopeAndDomain_IncludesInHeader()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            HeaderDisclaimer = "Test",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var record = new AdrRecord
        {
            Title = "Test",
            Version = 1,
            Scope = "Enterprise",
            Domain = "Backend",
            StatusCreate = AdrStatus.Proposed
        };

        // Act
        var header = record.GetHeader(config);

        // Assert
        header.Should().Contain("|Enterprise|");
        header.Should().Contain("|Backend|");
    }

    [Fact]
    public void GetHeader_WithMultipleStatuses_IncludesAllInHeader()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            HeaderDisclaimer = "Test",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var createDate = new DateTime(2025, 04, 17);
        var updateDate = new DateTime(2025, 04, 18);
        var changeDate = new DateTime(2025, 04, 19);

        var record = new AdrRecord
        {
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Proposed,
            CreateRef = createDate,
            StatusUpdate = AdrStatus.Accepted,
            UpdateRef = updateDate,
            StatusChange = AdrStatus.Rejected,
            ChangeRef = changeDate
        };

        // Act
        var header = record.GetHeader(config);

        // Assert
        header.Should().Contain("Proposed");
        header.Should().Contain("Accepted");
        header.Should().Contain("Rejected");
        header.Should().Contain("2025-04-17");
        header.Should().Contain("2025-04-18");
        header.Should().Contain("2025-04-19");
    }

    [Fact]
    public void GetHeader_WithUnknownStatus_OmitsStatusFromHeader()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            HeaderDisclaimer = "Test",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var record = new AdrRecord
        {
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Unknown,
            StatusUpdate = AdrStatus.Unknown,
            StatusChange = AdrStatus.Unknown
        };

        // Act
        var header = record.GetHeader(config);

        // Assert
        header.Should().Contain("|Created||");
        header.Should().Contain("|Changed||");
        header.Should().Contain("|Superseded||");
    }

    [Fact]
    public void GetHeader_SupersedeFileOnlyIfStatusSuperseded_IgnoresFileOtherwise()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("","")
        {
            Prefix = "ADR",
            LenSeq = 4,
            LenVersion = 2,
            LenRevision = 0,
            Separator = '-',
            HeaderDisclaimer = "Test",
            HeaderTableFields = "Fields",
            HeaderTableValues = "Values",
            HeaderTitleFile = "Title",
            HeaderVersion = "Version",
            HeaderRevision = "Revision",
            HeaderScope = "Scope",
            HeaderDomain = "Domain",
            HeaderTitleStatusCreated = "Created",
            HeaderTitleStatusChanged = "Changed",
            HeaderTitleStatusSuperseded = "Superseded",
            StatusNew = "Proposed",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        var record = new AdrRecord
        {
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Proposed,
            StatusChange = AdrStatus.Accepted // NOT Superseded
        };

        // Act
        var header = record.GetHeader(config, "ADR-0002.md", false);

        // Assert
        header.Should().NotContain("ADR-0002.md");
    }

    #endregion

    #region Record Equality and Record Tests

    [Fact]
    public void AdrRecord_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var date = DateTime.UtcNow;
        var record1 = new AdrRecord
        {
            Number = 1,
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Proposed,
            CreateRef = date
        };

        var record2 = new AdrRecord
        {
            Number = 1,
            Title = "Test",
            Version = 1,
            StatusCreate = AdrStatus.Proposed,
            CreateRef = date
        };

        // Act & Assert
        record1.Should().Be(record2);
    }

    [Fact]
    public void AdrRecord_RecordCopy_WithModifications_CreatesNewInstance()
    {
        // Arrange
        var original = new AdrRecord { Title = "Original", Version = 1 };

        // Act
        var copy = original with { Title = "Modified", Version = 2 };

        // Assert
        original.Title.Should().Be("Original");
        original.Version.Should().Be(1);
        copy.Title.Should().Be("Modified");
        copy.Version.Should().Be(2);
    }

    [Fact]
    public void AdrRecord_GetHashCode_SameForEqualRecords()
    {
        // Arrange
        var record1 = new AdrRecord { Title = "Test", Number = 1 };
        var record2 = new AdrRecord { Title = "Test", Number = 1 };

        // Act & Assert
        record1.GetHashCode().Should().Be(record2.GetHashCode());
    }

    [Fact]
    public void AdrRecord_AllStatuses_CanBeSet()
    {
        // Arrange
        var statusValues = new[] 
        { 
            AdrStatus.Unknown, 
            AdrStatus.Proposed, 
            AdrStatus.Accepted, 
            AdrStatus.Rejected, 
            AdrStatus.Superseded 
        };

        // Act & Assert
        foreach (var status in statusValues)
        {
            var record = new AdrRecord
            {
                StatusCreate = status,
                StatusUpdate = status,
                StatusChange = status
            };

            record.StatusCreate.Should().Be(status);
            record.StatusUpdate.Should().Be(status);
            record.StatusChange.Should().Be(status);
        }
    }

    [Fact]
    public void AdrRecord_WithLargeNumbers_StoresCorrectly()
    {
        // Arrange
        var record = new AdrRecord
        {
            Number = 99999,
            Version = 100,
            Revision = 50,
            Superseded = 88888
        };

        // Act & Assert
        record.Number.Should().Be(99999);
        record.Version.Should().Be(100);
        record.Revision.Should().Be(50);
        record.Superseded.Should().Be(88888);
    }

    [Fact]
    public void AdrRecord_WithNullableDates_CanBeNull()
    {
        // Arrange & Act
        var record1 = new AdrRecord { CreateRef = null, UpdateRef = null, ChangeRef = null };
        var now = DateTime.UtcNow;
        var record2 = new AdrRecord { CreateRef = now, UpdateRef = now, ChangeRef = now };

        // Assert
        record1.CreateRef.Should().BeNull();
        record1.UpdateRef.Should().BeNull();
        record1.ChangeRef.Should().BeNull();
        record2.CreateRef.Should().Be(now);
        record2.UpdateRef.Should().Be(now);
        record2.ChangeRef.Should().Be(now);
    }

    #endregion
}
