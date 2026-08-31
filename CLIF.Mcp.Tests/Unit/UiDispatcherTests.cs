using FluentAssertions;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tests.Unit;

public sealed class UiDispatcherTests
{
    [Fact]
    public async Task InvokeAsync_SerializesConcurrentOperations()
    {
        using var dispatcher = new UiDispatcher();
        var active = 0;
        var maximumActive = 0;
        var monitor = new object();

        async Task Run(CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref active);
            lock (monitor) maximumActive = Math.Max(maximumActive, current);
            await Task.Delay(25, cancellationToken);
            Interlocked.Decrement(ref active);
        }

        await Task.WhenAll(
            dispatcher.InvokeAsync(async token => { await Run(token); return 1; }),
            dispatcher.InvokeAsync(async token => { await Run(token); return 2; }));

        maximumActive.Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_CancelsWhileWaitingForUiTurn()
    {
        using var dispatcher = new UiDispatcher();
        using var held = new CancellationTokenSource();
        using var waiting = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = dispatcher.InvokeAsync(async token =>
        {
            started.SetResult();
            await Task.Delay(250, token);
            return 1;
        }, held.Token);
        await started.Task;

        var second = () => dispatcher.InvokeAsync(_ => Task.FromResult(2), waiting.Token);
        await FluentActions.Invoking(second)
            .Should().ThrowAsync<OperationCanceledException>();

        held.Cancel();
        await FluentActions.Invoking(async () => await first)
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
