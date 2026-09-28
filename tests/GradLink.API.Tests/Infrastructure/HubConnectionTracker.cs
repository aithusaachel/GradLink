using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace GradLink.API.Tests.Infrastructure;

// SignalR completes the client handshake before the hub's OnConnectedAsync has added the connection
// to its user group, so a push sent straight after StartAsync can be lost. Tests await this signal first.
public sealed class HubConnectionTracker : IHubFilter
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource> _connected = new();

    public Task WaitForAsync(string connectionId) =>
        Signal(connectionId).Task.WaitAsync(TimeSpan.FromSeconds(10));

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        await next(context);
        Signal(context.Context.ConnectionId).TrySetResult();
    }

    private TaskCompletionSource Signal(string connectionId) =>
        _connected.GetOrAdd(connectionId, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
}
