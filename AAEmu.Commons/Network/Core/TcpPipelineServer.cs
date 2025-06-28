#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace AAEmu.Commons.Network.Core;

public interface ITcpPipelineServer : IAsyncDisposable
{
    /// <summary>
    /// Starts accepting client connections.
    /// </summary>
    void Start();

    /// <summary>
    /// Shuts down the server gracefully. No more client connections will be accepted, and existing clients will be
    /// terminated.
    /// </summary>
    Task ShutdownAsync();
}

public class TcpPipelineServer(
    TcpPipelineServerOptions options,
    IBaseProtocolHandler protocolHandler,
    IClientFactory clientFactory,
    ILogger<TcpPipelineServer> logger) : ITcpPipelineServer
{
    private readonly record struct ClientData(IClient2 Client, Task FinishedTask);

    private readonly TcpListener _listener = new(options.ListenAddress, options.Port);
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<EndPoint, ClientData> _clients = new();
    private bool _active;
    private bool _disposed;
    private Task? _acceptLoopTask;

    /// <summary>
    /// Starts accepting client connections.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(TcpPipelineServer));

        if (_active)
        {
            return;
        }

        _listener.Start();
        _acceptLoopTask = AcceptLoopAsync();
        _active = true;
    }

    /// <summary>
    /// Shuts down the server gracefully. No more client connections will be accepted, and existing clients will be
    /// allowed to finish processing.
    /// </summary>
    public async Task ShutdownAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(TcpPipelineServer));

        // Cancel the AcceptTcpClientAsync call
        await _cts.CancelAsync();

        // Wait for the accept loop to finish
        if (_acceptLoopTask != null)
        {
            await _acceptLoopTask;
        }

        // Now stop the listener.
        // This is done last to avoid ObjectDisposedException coming out of the AcceptTcpClientAsync call.
        _listener.Stop();

        // ToArray for a snapshot of the current clients.
        // Stopping the listener above ensures we won't accept new clients at this point, but HandleClientAsync may
        // remove clients from the dictionary when they disconnect.
        var clients = _clients.ToArray();

        // Shutdown all existing clients.
        foreach (var (_, clientData) in clients)
        {
            try
            {
                clientData.Client.Close();
            }
            catch
            {
                // Ignore errors on closing clients
            }
        }

        // Wait for all clients to have finished processing.
        await Task.WhenAll(clients.Select(kvp => kvp.Value.FinishedTask));
    }

    private async Task AcceptLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = HandleClientAsync(client);
            }
            catch (OperationCanceledException)
            {
                // Server stopped
            }
            catch (Exception ex)
            {
                TcpPipelineServerLog.AcceptError(logger, ex);
            }
        }
    }

    private async Task HandleClientAsync(TcpClient tcpClient)
    {
        var endpoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint!;
        await using var client = clientFactory.Create(tcpClient, protocolHandler);
        var finishedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _clients[endpoint] = new ClientData(client, finishedTcs.Task);

        try
        {
            TcpPipelineServerLog.ClientConnected(logger, endpoint);
            client.Start();
            await client.Completion;
        }
        finally
        {
            TcpPipelineServerLog.ClientDisconnected(logger, endpoint);
            _clients.TryRemove(endpoint, out _);
            finishedTcs.SetResult();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        // Gracefully shut down the server and disconnect clients.
        // After this call completes, _clients will be empty.
        await ShutdownAsync();
        Debug.Assert(_clients.IsEmpty);

        _listener.Dispose();
        _cts.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
