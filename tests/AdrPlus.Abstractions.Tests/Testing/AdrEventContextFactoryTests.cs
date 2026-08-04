using AdrPlus.Abstractions.Domain;
using AdrPlus.Abstractions.Testing;

namespace AdrPlus.Abstractions.Tests.Testing;

public class AdrEventContextFactoryTests
{
    [Fact]
    public void Create_WithNoArguments_ReturnsValidContextWithDefaults()
    {
        var context = AdrEventContextFactory.Create();

        context.EventType.Should().Be(AdrEventType.Approved);
        context.IsReplay.Should().BeFalse();
        context.Adr.Should().BeEquivalentTo(AdrRecordSnapshotFactory.Create());
        context.Repo.Should().BeEquivalentTo(RepoInfoSnapshotFactory.Create());
        context.AdrFilePath.Should().NotBeNullOrEmpty();
        context.GetAdrRenderedContent().Should().Be("# Sample decision\n\nSample content.");
        context.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_TwoCalls_GenerateDifferentCorrelationIds()
    {
        var first = AdrEventContextFactory.Create();
        var second = AdrEventContextFactory.Create();

        first.CorrelationId.Should().NotBe(second.CorrelationId);
    }

    [Fact]
    public void Create_WithOverriddenAdrAndRepo_UsesThemInsteadOfDefaults()
    {
        var adr = AdrRecordSnapshotFactory.Create(number: 42);
        var repo = RepoInfoSnapshotFactory.Create(folderAdr: "decisions");

        var context = AdrEventContextFactory.Create(eventType: AdrEventType.Rejected, isReplay: true, adr: adr, repo: repo);

        context.EventType.Should().Be(AdrEventType.Rejected);
        context.IsReplay.Should().BeTrue();
        context.Adr.Should().BeSameAs(adr);
        context.Repo.Should().BeSameAs(repo);
    }

    [Fact]
    public void Create_WithGetAdrRenderedContentOverride_IgnoresRenderedContentParameter()
    {
        var callCount = 0;
        string GetContent()
        {
            callCount++;
            return "dynamic content";
        }

        var context = AdrEventContextFactory.Create(renderedContent: "ignored", getAdrRenderedContent: GetContent);

        context.GetAdrRenderedContent().Should().Be("dynamic content");
        callCount.Should().Be(1);
    }
}
