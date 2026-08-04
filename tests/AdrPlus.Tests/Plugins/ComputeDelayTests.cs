// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Plugins;

namespace AdrPlus.Tests.Plugins;

/// <summary>
/// Unit tests for <see cref="PluginManager.ComputeDelay"/> — the backoff formula (Fixed/Exponential,
/// jitter, overflow clamp) — exercised in isolation, without any <c>Task.Delay</c> actually elapsing.
/// </summary>
public class ComputeDelayTests
{
    [Fact]
    public void ComputeDelay_WithFixedBackoff_ReturnsFlatDelayRegardlessOfAttempt()
    {
        var policy = new PluginRetryPolicy { Backoff = "Fixed", DelayMs = 2000, Jitter = false };

        PluginManager.ComputeDelay(policy, 1).Should().Be(2000);
        PluginManager.ComputeDelay(policy, 5).Should().Be(2000);
    }

    [Theory]
    [InlineData(1, 1000)]
    [InlineData(2, 2000)]
    [InlineData(3, 4000)]
    [InlineData(4, 8000)]
    public void ComputeDelay_WithExponentialBackoff_DoublesPerAttempt(int attempt, int expected)
    {
        var policy = new PluginRetryPolicy { Backoff = "Exponential", DelayMs = 1000, Jitter = false };

        PluginManager.ComputeDelay(policy, attempt).Should().Be(expected);
    }

    [Fact]
    public void ComputeDelay_WithExponentialBackoff_ClampsToMaxDelayInsteadOfOverflowing()
    {
        var policy = new PluginRetryPolicy { Backoff = "Exponential", DelayMs = 1000, Jitter = false };

        // Cumulative attempt numbers are unbounded (the user's "keep pending across runs" policy) — an
        // unclamped 2^(attempt-1) would overflow around attempt≈32 and go negative, making Task.Delay throw.
        var delay = PluginManager.ComputeDelay(policy, 40);

        delay.Should().BePositive();
        delay.Should().BeLessThanOrEqualTo(300_000);
    }

    [Fact]
    public void ComputeDelay_WithJitter_ReturnsValueWithinZeroToComputedDelay()
    {
        var policy = new PluginRetryPolicy { Backoff = "Fixed", DelayMs = 1000, Jitter = true };

        for (var i = 0; i < 50; i++)
        {
            var delay = PluginManager.ComputeDelay(policy, 1);
            delay.Should().BeInRange(0, 1000);
        }
    }
}
