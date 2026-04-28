// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using System.Runtime.InteropServices;

namespace AdrPlus.Tests.Domain;

public class AdrRecordTests
{
    private static string PlatformPath(params string[] segments) => Path.Combine(segments);
    private static string PlatformDrive => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:" : "/tmp";

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
}
