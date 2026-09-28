using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace GradLink.API.Tests.Infrastructure;

public sealed class NotificationListener : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly Channel<NotificationDto> _received = Channel.CreateUnbounded<NotificationDto>();

    private NotificationListener(HubConnection connection)
    {
        _connection = connection;
        _connection.On<NotificationDto>("ReceiveNotification", notification => _received.Writer.TryWrite(notification));
    }

    // Browsers cannot set headers on a WebSocket handshake, so the web client sends the token as a query parameter.
    public static async Task<NotificationListener> ConnectAsync(GradLinkApiFactory factory, string? accessToken, bool tokenInQueryString = false)
    {
        var path = tokenInQueryString ? $"hubs/notifications?access_token={accessToken}" : "hubs/notifications";
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, path), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                if (!tokenInQueryString)
                    options.AccessTokenProvider = () => Task.FromResult(accessToken);
            })
            .Build();

        var listener = new NotificationListener(connection);
        try
        {
            await connection.StartAsync();
            await factory.HubConnections.WaitForAsync(connection.ConnectionId!);
            return listener;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task<NotificationDto> NextAsync() =>
        await _received.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
