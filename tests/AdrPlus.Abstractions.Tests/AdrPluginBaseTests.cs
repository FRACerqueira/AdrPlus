using AdrPlus.Abstractions;
using AdrPlus.Abstractions.Domain;

namespace AdrPlus.Abstractions.Tests;

public class AdrPluginBaseTests
{
    private static AdrEventContext CreateContext() => new()
    {
        EventType = AdrEventType.Approved,
        IsReplay = false,
        Adr = new AdrRecordSnapshot
        {
            Number = 1,
            Version = 1,
            Title = "Test",
            Domain = string.Empty,
            Scope = string.Empty,
            StatusCreate = AdrStatus.Proposed,
            StatusUpdate = AdrStatus.Unknown,
            StatusChange = AdrStatus.Unknown
        },
        AdrFilePath = "irrelevant.md",
        GetAdrRenderedContent = () => "content",
        Repo = new RepoInfoSnapshot
        {
            FolderAdr = "docs/adr",
            Scopes = [],
            StatusMapping = new Dictionary<AdrStatus, string>()
        },
        CorrelationId = "correlation-id"
    };

    private sealed class TestPlugin(bool shouldHandle, Func<Task<PluginResult>>? handle = null) : AdrPluginBase
    {
        public override string Name => "test-plugin";
        public override string Version => "1.0.0";

        public override bool ShouldHandle(AdrEventContext context) => shouldHandle;

        protected override Task<PluginResult> HandleAsync(AdrEventContext context, CancellationToken ct) =>
            handle is not null ? handle() : Task.FromResult(Success());
    }

    [Fact]
    public async Task OnAdrEventAsync_WhenShouldHandleReturnsFalse_ReturnsSkippedWithoutCallingHandleAsync()
    {
        var called = false;
        var plugin = new TestPlugin(shouldHandle: false, handle: () =>
        {
            called = true;
            return Task.FromResult(new PluginResult { Status = PluginResultStatus.Success });
        });

        var result = await plugin.OnAdrEventAsync(CreateContext(), CancellationToken.None);

        result.Status.Should().Be(PluginResultStatus.Skipped);
        called.Should().BeFalse();
    }

    [Fact]
    public async Task OnAdrEventAsync_WhenHandleAsyncSucceeds_ReturnsItsResult()
    {
        var plugin = new TestPlugin(shouldHandle: true, handle: () =>
            Task.FromResult(new PluginResult { Status = PluginResultStatus.Success, ExternalKey = "page-1" }));

        var result = await plugin.OnAdrEventAsync(CreateContext(), CancellationToken.None);

        result.Status.Should().Be(PluginResultStatus.Success);
        result.ExternalKey.Should().Be("page-1");
    }

    [Fact]
    public async Task OnAdrEventAsync_WhenHandleAsyncThrows_ReturnsRetryableFailed()
    {
        var plugin = new TestPlugin(shouldHandle: true, handle: () => throw new InvalidOperationException("boom"));

        var result = await plugin.OnAdrEventAsync(CreateContext(), CancellationToken.None);

        result.Status.Should().Be(PluginResultStatus.Failed);
        result.IsRetryable.Should().BeTrue();
        result.Message.Should().Be("boom");
    }

    [Fact]
    public async Task OnAdrEventAsync_WhenHandleAsyncThrowsOperationCanceledException_PropagatesInsteadOfShielding()
    {
        var plugin = new TestPlugin(shouldHandle: true, handle: () => throw new OperationCanceledException());

        var act = async () => await plugin.OnAdrEventAsync(CreateContext(), CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DisposeAsync_CompletesWithoutThrowing()
    {
        var plugin = new TestPlugin(shouldHandle: true);

        await plugin.DisposeAsync();
    }
}
