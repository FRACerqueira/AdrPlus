using AdrPlus.Abstractions.Domain;
using AdrPlus.Abstractions.Testing;

namespace AdrPlus.Abstractions.Tests.Testing;

public class RepoInfoSnapshotFactoryTests
{
    [Fact]
    public void Create_WithNoArguments_ReturnsValidSnapshotWithDefaults()
    {
        var snapshot = RepoInfoSnapshotFactory.Create();

        snapshot.FolderAdr.Should().Be("docs/adr");
        snapshot.Scopes.Should().Equal("core");
        snapshot.StatusMapping.Should().ContainKeys(Enum.GetValues<AdrStatus>());
        snapshot.StatusMapping[AdrStatus.Accepted].Should().Be(nameof(AdrStatus.Accepted));
    }

    [Fact]
    public void Create_WithOverrides_UsesGivenValuesInstead()
    {
        var snapshot = RepoInfoSnapshotFactory.Create(
            folderAdr: "decisions",
            scopes: ["backend", "frontend"],
            statusMapping: new Dictionary<AdrStatus, string> { [AdrStatus.Accepted] = "Aprovado" });

        snapshot.FolderAdr.Should().Be("decisions");
        snapshot.Scopes.Should().Equal("backend", "frontend");
        snapshot.StatusMapping.Should().ContainSingle().Which.Should().Be(
            new KeyValuePair<AdrStatus, string>(AdrStatus.Accepted, "Aprovado"));
    }
}
