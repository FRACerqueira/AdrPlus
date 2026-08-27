// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Extensions;
using AbstractionsDomain = AdrPlus.Abstractions.Domain;

namespace AdrPlus.Tests.Extensions;

public class PluginSnapshotExtensionsTests
{
    [Fact]
    public void ToSnapshot_AdrRecord_MapsEveryFieldToMatchingSnapshotField()
    {
        // Arrange
        var record = new AdrRecord
        {
            Number = 7,
            Version = 2,
            Revision = 1,
            Title = "My Title",
            Domain = "MyDomain",
            Scope = "Enterprise",
            StatusCreate = AdrStatus.Proposed,
            StatusUpdate = AdrStatus.Accepted,
            StatusChange = AdrStatus.Superseded,
            CreateRef = new DateTime(2026, 1, 1),
            UpdateRef = new DateTime(2026, 2, 1),
            ChangeRef = new DateTime(2026, 3, 1),
            Superseded = 3
        };

        // Act
        var snapshot = record.ToSnapshot();

        // Assert
        snapshot.Number.Should().Be(record.Number);
        snapshot.Version.Should().Be(record.Version);
        snapshot.Revision.Should().Be(record.Revision);
        snapshot.Title.Should().Be(record.Title);
        snapshot.Domain.Should().Be(record.Domain);
        snapshot.Scope.Should().Be(record.Scope);
        snapshot.StatusCreate.Should().Be(record.StatusCreate.ToSnapshot());
        snapshot.StatusUpdate.Should().Be(record.StatusUpdate.ToSnapshot());
        snapshot.StatusChange.Should().Be(record.StatusChange.ToSnapshot());
        snapshot.CreateRef.Should().Be(record.CreateRef);
        snapshot.UpdateRef.Should().Be(record.UpdateRef);
        snapshot.ChangeRef.Should().Be(record.ChangeRef);
        snapshot.Superseded.Should().Be(record.Superseded);
    }

    [Fact]
    public void ToSnapshot_AdrRecord_WithNullableFieldsUnset_MapsThemAsNull()
    {
        // Arrange
        var record = new AdrRecord();

        // Act
        var snapshot = record.ToSnapshot();

        // Assert
        snapshot.Revision.Should().BeNull();
        snapshot.CreateRef.Should().BeNull();
        snapshot.UpdateRef.Should().BeNull();
        snapshot.ChangeRef.Should().BeNull();
        snapshot.Superseded.Should().BeNull();
    }

    [Fact]
    public void ToSnapshot_AdrPlusRepoConfig_MapsFolderAdr()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("docs/adr", "");

        // Act
        var snapshot = config.ToSnapshot();

        // Assert
        snapshot.FolderAdr.Should().Be(config.FolderAdr);
    }

    [Fact]
    public void ToSnapshot_AdrPlusRepoConfig_MapsStatusMapping()
    {
        // Arrange
        var config = new AdrPlusRepoConfig("", "")
        {
            StatusNew = "New",
            StatusAcc = "Accepted",
            StatusRej = "Rejected",
            StatusSup = "Superseded"
        };

        // Act
        var snapshot = config.ToSnapshot();

        // Assert
        var expected = config.StatusMapping.ToDictionary(kv => kv.Key.ToSnapshot(), kv => kv.Value);
        snapshot.StatusMapping.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToSnapshot_AdrStatus_MapsEveryValueToMatchingPublicStatus()
    {
        // Assert
        AdrStatus.Unknown.ToSnapshot().Should().Be(AbstractionsDomain.AdrStatus.Unknown);
        AdrStatus.Proposed.ToSnapshot().Should().Be(AbstractionsDomain.AdrStatus.Proposed);
        AdrStatus.Accepted.ToSnapshot().Should().Be(AbstractionsDomain.AdrStatus.Accepted);
        AdrStatus.Rejected.ToSnapshot().Should().Be(AbstractionsDomain.AdrStatus.Rejected);
        AdrStatus.Superseded.ToSnapshot().Should().Be(AbstractionsDomain.AdrStatus.Superseded);
    }
}
