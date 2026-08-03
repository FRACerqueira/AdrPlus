using AdrPlus.Abstractions.Domain;
using AdrPlus.Abstractions.Testing;

namespace AdrPlus.Abstractions.Tests.Testing;

public class AdrRecordSnapshotFactoryTests
{
    [Fact]
    public void Create_WithNoArguments_ReturnsValidSnapshotWithDefaults()
    {
        var snapshot = AdrRecordSnapshotFactory.Create();

        snapshot.Number.Should().Be(1);
        snapshot.Version.Should().Be(1);
        snapshot.Revision.Should().BeNull();
        snapshot.Title.Should().Be("Sample decision");
        snapshot.Domain.Should().Be("General");
        snapshot.Scope.Should().Be("core");
        snapshot.StatusCreate.Should().Be(AdrStatus.Proposed);
        snapshot.StatusUpdate.Should().Be(AdrStatus.Unknown);
        snapshot.StatusChange.Should().Be(AdrStatus.Unknown);
        snapshot.Superseded.Should().BeNull();
    }

    [Fact]
    public void Create_WithOverrides_UsesGivenValuesInstead()
    {
        var snapshot = AdrRecordSnapshotFactory.Create(
            number: 7,
            version: 2,
            revision: 1,
            title: "Use PostgreSQL",
            statusUpdate: AdrStatus.Accepted,
            superseded: 3);

        snapshot.Number.Should().Be(7);
        snapshot.Version.Should().Be(2);
        snapshot.Revision.Should().Be(1);
        snapshot.Title.Should().Be("Use PostgreSQL");
        snapshot.StatusUpdate.Should().Be(AdrStatus.Accepted);
        snapshot.Superseded.Should().Be(3);
    }
}
